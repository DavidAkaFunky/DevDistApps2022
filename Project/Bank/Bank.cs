using System.Collections.Concurrent;
using System.Collections.Generic;
using Grpc.Core;
using Timer = System.Timers.Timer;

namespace DADProject;

internal enum Command
{
    BoneyServer,
    BankServer,
    Client,
    SlotCount,
    WallTime,
    SlotDuration,
    SlotState,
    Invalid
}

internal struct ServerState
{
    public bool IsFrozen;
    public bool IsSuspected;
}

internal class Bank
{
    private static int _currentSlot = 1;
    private static int _slotCount;
    private static int _slotDuration;
    private static int _initialSleepTime;

    private static string _address = "";
    private static readonly List<string> _boneyAddresses = new();
    private static readonly List<string> _bankAddresses = new();
    private static readonly List<int> _bankIDs = new();

    private static readonly List<Dictionary<int, ServerState>> _serverStates = new();

    private static readonly List<BankToBankFrontend> bankToBankFrontends = new();
    private static readonly List<BankToBoneyFrontend> bankToBoneyFrontends = new();

    private static Command GetType(string line, out string[] tokens)
    {
        if (line.Length == 0)
        {
            tokens = Array.Empty<string>();
            return Command.Invalid;
        }

        tokens = line.Split(' ');
        return tokens[0] switch
        {
            "P" => tokens[2] switch
            {
                "boney" => Command.BoneyServer,
                "bank" => Command.BankServer,
                _ => Command.Client
            },
            "S" => Command.SlotCount,
            "D" => Command.SlotDuration,
            "F" => Command.SlotState,
            _ => Command.Invalid,
        };
    }

    public static void Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Wrong number of arguments: [id] [configPath] [startTime]");
            return;
        }

        if (!int.TryParse(args[0], out var id))
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        try
        {
            File.ReadLines(args[1]).ToList().ForEach(line =>
            {
                switch (GetType(line, out var tokens))
                {
                    case Command.BankServer:
                        _bankIDs.Add(int.Parse(tokens[1]));
                        _bankAddresses.Add(tokens[3]);
                        break;
                    case Command.BoneyServer:
                        _boneyAddresses.Add(tokens[3]);
                        break;
                    case Command.SlotCount:
                        _slotCount = int.Parse(tokens[1]);
                        break;
                    case Command.SlotDuration:
                        _slotDuration = int.Parse(tokens[1]);
                        break;
                    case Command.SlotState:
                        // Giga Cursed
                        foreach (var c in new string[] { ",", "(", ")" })
                            line = line.Replace(c, string.Empty);
                        var fields = line.Split();
                        Dictionary<int, ServerState> states = new();
                        for (var i = 2; i < fields.Length; i += 3)
                        {
                            var _id = int.Parse(fields[i]);
                            if (!_bankIDs.Contains(_id)) continue;
                            bool isFrozen = fields[i + 1] != "N";
                            bool isSuspected;
                            if (_id == id)
                                isSuspected = isFrozen;
                            else
                                isSuspected = fields[i + 2] == "S";
                            var state = new ServerState
                            { IsFrozen = isFrozen, IsSuspected = isSuspected };
                            states.Add(_id, state);
                        }

                        _serverStates.Add(states);
                        break;
                }
            });

            var currentTime = DateTime.Now;
            var startTime = Convert.ToDateTime(args[2]);
            if (startTime < currentTime)
                startTime = startTime.AddDays(1);
            _initialSleepTime = (int)(startTime - currentTime).TotalMilliseconds;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Cannot open file");
            return;
        }

        //==================================Service Info======================================
        var account = new BankAccount();
        var primaries = new ConcurrentDictionary<int, int>();
        _address = _bankAddresses[id - _boneyAddresses.Count - 1];

        _bankAddresses.ForEach(serverAddr =>
            bankToBankFrontends.Add(new BankToBankFrontend(id, serverAddr)));

        _boneyAddresses.ForEach(serverAddr =>
            bankToBoneyFrontends.Add(new BankToBoneyFrontend(id, serverAddr, primaries)));

        var TwoPC = new TwoPhaseCommit(id, _address, bankToBankFrontends, account);

        //===================================Server Initialization============================

        Uri ownUri = new(_address);

        var isPerceivedLeader = new Dictionary<int, bool>();
        var isFrozen = new Dictionary<int, bool>();

        for (var slot = 0; slot < _serverStates.Count; slot++)
        {
            // ver se está frozen em cada slot
            isFrozen[slot + 1] = _serverStates[slot][id].IsFrozen;

            // pls don't blame me for spaghetti code, couldn't find a way to convert it
            // directly to list without converting to dict and then getting the keys
            var notSuspected = _serverStates[slot].Where(server => !server.Value.IsSuspected)
                .ToDictionary(server => server.Key, server => server.Value)
                .Keys.ToList();

            // pensa que e lider se a lista de servidores vivos para um slot n estiver vazia (duh)
            // e se o minimo dos valores da lista for ele proprio
            isPerceivedLeader[slot + 1] = notSuspected.Count > 0 && notSuspected.Min() == id;
        }

        var bankServerService = new BankServerService(id, primaries, TwoPC, isFrozen, account);
        var bank2PCService = new BankTwoPCService(primaries, TwoPC, isFrozen);

        Server server = new()
        {
            Services = 
            { 
                ProjectBankServerService.BindService(bankServerService),
                ProjectBankTwoPCService.BindService(bank2PCService)
            },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        PrintHeader();

        Console.WriteLine("Listening on port " + ownUri.Port);

        //============================Set Timer==========================

        void HandleTimer()
        {
            _currentSlot++;
            primaries[_currentSlot] = -1;

            bankServerService.CurrentSlot = _currentSlot;
            bank2PCService.CurrentSlot = _currentSlot;

            if (_currentSlot > _slotCount)
            {
                // TODO: Maybe wait until everything was finished, but how?
                //server.ShutdownAsync().Wait();
                //Environment.Exit(0);
            }

            Console.WriteLine("--NEW SLOT: {0}--", _currentSlot);

            if (isPerceivedLeader[_currentSlot])
                bankToBoneyFrontends.ForEach(frontend => frontend.RequestCompareAndSwap(_currentSlot));

            if (!isFrozen[_currentSlot])
            {
                while (primaries[_currentSlot] == -1);

                if (primaries[_currentSlot] == id && primaries[_currentSlot] != primaries[_currentSlot - 1])
                {
                    TwoPC.CleanUp2PC(_currentSlot);
                }
            }
        }

        //ACTIVATE BEFORE THE DELIVERY!!!!
        Console.WriteLine("Waiting for " + _initialSleepTime + " milliseconds");
        Thread.Sleep(_initialSleepTime);

        Timer timer = new(_slotDuration);
        timer.Elapsed += (sender, e) => HandleTimer();
        timer.Start();

        //=============================Start Processing Commands===============================

        primaries[_currentSlot] = -1;

        if (isPerceivedLeader[_currentSlot])
            bankToBoneyFrontends.ForEach(frontend => frontend.RequestCompareAndSwap(_currentSlot));

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }


    public static void PrintHeader()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("$$$$$$$\\   $$$$$$\\  $$\\   $$\\ $$\\   $$\\ ");
        Console.WriteLine("$$  __$$\\ $$  __$$\\ $$$\\  $$ |$$ | $$  |");
        Console.WriteLine("$$ |  $$ |$$ /  $$ |$$$$\\ $$ |$$ |$$  / ");
        Console.WriteLine("$$$$$$$\\ |$$$$$$$$ |$$ $$\\$$ |$$$$$  /  ");
        Console.WriteLine("$$  __$$\\ $$  __$$ |$$ \\$$$$ |$$  $$<   ");
        Console.WriteLine("$$ |  $$ |$$ |  $$ |$$ |\\$$$ |$$ |\\$$\\  ");
        Console.WriteLine("$$$$$$$  |$$ |  $$ |$$ | \\$$ |$$ | \\$$\\ ");
        Console.WriteLine("\\_______/ \\__|  \\__|\\__|  \\__|\\__|  \\__|");
        Console.WriteLine("========================================");
    }

}