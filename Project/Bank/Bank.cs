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
    private static string _address = "";
    private static readonly List<string> _boneyAddresses = new();
    private static readonly List<string> _bankAddresses = new();
    private static readonly List<int> _bankIDs = new();
    private static int _slotCount;
    private static readonly List<int> _wallTimes = new();
    private static int _slotDuration;
    private static readonly List<Dictionary<int, ServerState>> _serverStates = new();
    private static readonly List<BankToBankFrontend> bankToBankFrontends = new();
    private static readonly List<BankToBoneyFrontend> bankToBoneyFrontends = new();
    private static readonly ConcurrentDictionary<int, ClientCommand> tentativeCommands = new();
    private static readonly ConcurrentDictionary<int, ClientCommand> committedCommands = new();

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
            "T" => Command.WallTime,
            "D" => Command.SlotDuration,
            "F" => Command.SlotState,
            _ => Command.Invalid,
        };
    }

    public void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Wrong number of arguments: [id] [configPath]");
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
                    case Command.WallTime:
                        tokens[1].Split(':').ToList().ForEach(time => _wallTimes.Add(int.Parse(time)));
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
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Cannot open file");
            return;
        }

        //==================================Service Info======================================

        var isPrimary = new ConcurrentDictionary<int, int>();

        //===================================Server Initialization============================

        _address = _bankAddresses[id - _boneyAddresses.Count - 1];
        Uri ownUri = new(_address);

        _bankAddresses.ForEach(serverAddr => 
            bankToBankFrontends.Add(new BankToBankFrontend(id, serverAddr)));

        _boneyAddresses.ForEach(serverAddr =>
            bankToBoneyFrontends.Add(new BankToBoneyFrontend(id, serverAddr, isPrimary)));

        var bankServerService = new BankServerService(id, isPrimary);
        var bank2PCService = new BankTwoPCService(id, isPrimary, tentativeCommands, committedCommands);


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
        Thread.Sleep(5000);

        PrintHeader();

        Console.WriteLine("Listening on port " + ownUri.Port);

        //============================Set Timer==========================
        void HandleTimer()
        {
            _currentSlot++;
            isPrimary[_currentSlot] = -1;

            bankServerService.CurrentSlot = _currentSlot;
            bank2PCService.CurrentSlot = _currentSlot;

            if (_currentSlot > _slotCount)
            {
                // TODO: Maybe wait until everything was finished, but how?
                //server.ShutdownAsync().Wait();
                //Environment.Exit(0);
            }

            Console.WriteLine("--NEW SLOT: {0}--", _currentSlot);
            bankToBoneyFrontends.ForEach(frontend => frontend.RequestCompareAndSwap(_currentSlot));
        }

        Timer timer = new(_slotDuration);
        timer.Elapsed += (sender, e) => HandleTimer();
        timer.Start();

        bankToBoneyFrontends.ForEach(frontend => frontend.RequestCompareAndSwap(_currentSlot));

        //=============================Start Processing Commands===============================

        CommandProcessing(
            id,
            isPrimary);

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }

    public void CleanUp2PC(int slot)
    {
        Dictionary<int, ClientCommand> commandsToCommit = new();

        //slot is not needed, remove??
        //listPendingRequests(lastKnownSequenceNumber) to all
        bankToBankFrontends.ForEach(frontend =>
        {
            var reply = frontend.ListPendingTwoPCRequests(slot, committedCommands.Keys.Max());

            foreach(var cmd in reply.Commands)
            {
                //Remove duplicates and prefer the most recent tentative version 
                if (!commandsToCommit.ContainsKey(cmd.GlobalSeqNumber))
                {
                    commandsToCommit.Add(
                        cmd.GlobalSeqNumber, 
                        new(cmd.Slot, cmd.ClientId, cmd.ClientSeqNumber, cmd.Message));
                }
                else 
                {
                    if (commandsToCommit[cmd.GlobalSeqNumber].Slot < cmd.Slot)
                    {
                        commandsToCommit[cmd.GlobalSeqNumber] = new(cmd.Slot, cmd.ClientId, cmd.ClientSeqNumber, cmd.Message);
                    }

                }
            }

            foreach(int seq in commandsToCommit.Keys)
            {
                TwoPC(seq, commandsToCommit[seq]);
            }
            
        });

        //TODO: wait for majority
    }

    public void TwoPC(int seq, ClientCommand cmd)
    {
        //send tentative with seq for command(cmd)
        bankToBankFrontends.ForEach(server =>
        {
            server.SendTwoPCTentative(_currentSlot, cmd, seq);

        });

        //TODO: wait for acknowledgement of majority

        //send commit to all replicas
        bankToBankFrontends.ForEach(server =>
        {
            server.SendTwoPCCommit(_currentSlot, cmd, seq);

        });

        //TODO: wait for acknowledgement of majority ???????????
    }

    public void CommandProcessing(
        int id,
        ConcurrentDictionary<int, int> isPrimary)
    {

        while (true)
        {

            //check if is primary 

            //check if leader changed
            if (leaderChanged)
            {
                leaderChanged = false;
                CleanUp2PC(_currentSlot);
            }

            //check if there is commands -> select first

            //if both:

            TwoPC(seq, cmd);

        }
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