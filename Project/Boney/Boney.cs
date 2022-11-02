using System;
using System.Collections.Concurrent;
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

internal class Boney
{
    private static int _boneySlot = 1;
    private static int _slotCount;
    private static int _slotDuration;
    private static int _initialSleepTime;

    private static string _address = "";
    private static readonly List<string> _boneyAddresses = new();
    private static readonly List<string> _bankAddresses = new();
    private static readonly List<int> _boneyIDs = new();

    private static readonly List<Dictionary<int, ServerState>> _serverStates = new();

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
            _ => Command.Invalid
        };
    }

    public static void Main(string[] args)
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
                        _bankAddresses.Add(tokens[3]);
                        break;
                    case Command.BoneyServer:
                        _boneyIDs.Add(int.Parse(tokens[1]));
                        _boneyAddresses.Add(tokens[3]);
                        break;
                    case Command.SlotCount:
                        _slotCount = int.Parse(tokens[1]);
                        break;
                    case Command.WallTime:
                        var currentTime = DateTime.Now;
                        var startTime = Convert.ToDateTime(tokens[1]);
                        if (startTime < currentTime)
                            startTime.AddDays(1);
                        _initialSleepTime = (int)(startTime - currentTime).TotalMilliseconds;
                        break;
                    case Command.SlotDuration:
                        _slotDuration = int.Parse(tokens[1]);
                        break;
                    case Command.SlotState:
                        // Giga Cursed
                        foreach (var c in new[] { ",", "(", ")" })
                            line = line.Replace(c, string.Empty);
                        var fields = line.Split();
                        Dictionary<int, ServerState> states = new();
                        for (var i = 2; i < fields.Length; i += 3)
                        {
                            var _id = int.Parse(fields[i]);
                            if (!_boneyIDs.Contains(_id)) continue;
                            var isFrozen = fields[i + 1] != "N";
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

        _address = _boneyAddresses[id - 1];


        //======================================================FRONTENDS==========================================

        var boneyToBankfrontends = new List<BoneyToBankFrontend>();
        var boneyToBoneyfrontends = new List<BoneyToBoneyFrontend>();

        _bankAddresses.ForEach(serverAddr => boneyToBankfrontends.Add(new BoneyToBankFrontend(id, serverAddr)));
        _boneyAddresses.ForEach(serverAddr => boneyToBoneyfrontends.Add(new BoneyToBoneyFrontend(id, serverAddr)));

        //==================================================CONSENSUS_INFO=========================================


        var slotsHistory = new ConcurrentDictionary<int, int>();
        var slotsInfo = new ConcurrentDictionary<int, Slot>();
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


        //======================================================SERVICES===========================================

        var proposerService = new BoneyProposerService(slotsHistory, slotsInfo, isFrozen, _boneySlot);
        var acceptorService = new BoneyAcceptorService(boneyToBoneyfrontends, slotsInfo, isFrozen, _boneySlot);
        var learnerService = new BoneyLearnerService(boneyToBankfrontends, _boneyAddresses.Count, slotsHistory, isFrozen, _boneySlot);

        var ownUri = new Uri(_address);
        var server = new Server
        {
            Services =
            {
                ProjectBoneyProposerService.BindService(proposerService),
                ProjectBoneyAcceptorService.BindService(acceptorService),
                ProjectBoneyLearnerService.BindService(learnerService)
            },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        PrintHeader();
        Console.WriteLine("Server " + ownUri.Host + " listening on port " + ownUri.Port);

        Paxos(id,
            _slotDuration,
            _slotCount,
            boneyToBoneyfrontends,
            isPerceivedLeader,
            slotsInfo,
            slotsHistory,
            proposerService,
            acceptorService,
            learnerService);

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }

    public static void PrintHeader()
    {
        Console.WriteLine("====================================================");
        Console.WriteLine("$$$$$$$\\   $$$$$$\\  $$\\   $$\\ $$$$$$$$\\ $$\\     $$\\ ");
        Console.WriteLine("$$  __$$\\ $$  __$$\\ $$$\\  $$ |$$  _____|\\$$\\   $$  |");
        Console.WriteLine("$$ |  $$ |$$ /  $$ |$$$$\\ $$ |$$ |       \\$$\\ $$  / ");
        Console.WriteLine("$$$$$$$\\ |$$ |  $$ |$$ $$\\$$ |$$$$$\\      \\$$$$  /  ");
        Console.WriteLine("$$  __$$\\ $$ |  $$ |$$ \\$$$$ |$$  __|      \\$$  /   ");
        Console.WriteLine("$$ |  $$ |$$ |  $$ |$$ |\\$$$ |$$ |          $$ |    ");
        Console.WriteLine("$$$$$$$  | $$$$$$  |$$ | \\$$ |$$$$$$$$\\     $$ |    ");
        Console.WriteLine("\\_______/  \\______/ \\__|  \\__|\\________|    \\__|    ");
        Console.WriteLine("====================================================");
    }

    public static void Paxos(int id,
        int slotDuration,
        int slotCount,
        List<BoneyToBoneyFrontend> frontends,
        Dictionary<int, bool> isPerceivedLeader,
        ConcurrentDictionary<int, Slot> slotsInfo,
        ConcurrentDictionary<int, int> slotHistory,
        BoneyProposerService proposerService,
        BoneyAcceptorService acceptorService,
        BoneyLearnerService learnerService)
    {
        var timestampId = id;
        Slot? slotToPropose;
        int majority = frontends.Count / 2 + 1;

        //========================================BONEY_SLOT_TIMER====================================================

        Timer timer = new(slotDuration);

        void HandleSlotTimer()
        {
            _boneySlot++;
            //if (boneySlot > slotCount)
            // FINISH
            proposerService.CurrentSlot = _boneySlot;
            acceptorService.CurrentSlot = _boneySlot;
            learnerService.CurrentSlot = _boneySlot;
            Console.WriteLine("--NEW SLOT: {0}--", _boneySlot);
        }

        //ACTIVATE BEFORE THE DELIVERY!!!!
        //Console.WriteLine("Waiting for " + _initialSleepTime + " milliseconds");
        //Thread.Sleep(_initialSleepTime);

        timer.Elapsed += (sender, e) => HandleSlotTimer();
        timer.Start();
        Console.WriteLine("--NEW SLOT: {0}--", _boneySlot);

        //======================================================================================================

        while (true)
        {
            //Loop enquanto nao sou lider
            if (!isPerceivedLeader[_boneySlot]) continue;

            var mostRecentslot = slotHistory.Count + 1;

            //sou lider, verificar se tenho valor para propor para o slot mais recente
            if (!slotsInfo.TryGetValue(mostRecentslot, out slotToPropose)) continue;

            // se nao (dicionario vazio ou o menor valor n é o meu), desisto 

            Console.WriteLine("Proposer: " + mostRecentslot + ": STARTING CONSENSUS");

            var value = slotToPropose.CurrentValue;
            var ts = 0;
            var stop = false;

            // se eu for o lider:
            // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
            // espero por maioria de respostas (Promise com valor, id da msg mais recente)
            // escolher valor mais recente das Promises

            if (timestampId != 1)
            {
                var promiseResponses = new List<PromiseReply>();
                Console.WriteLine("Proposer: {0}: Send Prepare with timestamp {1}", mostRecentslot, timestampId);

                frontends.ForEach(server =>
                {
                    new Thread(() =>
                    {
                        var r = server.Prepare(mostRecentslot, timestampId);
                        lock (promiseResponses)
                        {
                            promiseResponses.Add(r);
                            Monitor.PulseAll(promiseResponses);
                        }
                        

                    }).Start(); 
                });

                lock (promiseResponses)
                {
                    //espera pela maioria das respostas
                    while (promiseResponses.Count < majority)
                        Monitor.Wait(promiseResponses);

                    //processa respostas
                    foreach (var res in promiseResponses)
                    {
                        if (res.Value == -1 && res.WriteTimestamp == -1)
                        {
                            Console.WriteLine("Proposer: RECEIVED **NACK**");
                            stop = true;
                            break;
                        }
                        else if (res.WriteTimestamp > ts)
                        {
                            Console.WriteLine("Proposer: RECEIVED **ACK**");
                            value = res.Value;
                            ts = res.WriteTimestamp;
                        }
                    }
                }

                //no caso de ter recebido nack no proprose
                if (stop)
                {
                    timestampId += frontends.Count;
                    continue;
                }
            }

            Console.WriteLine("Proposer: {0}: Send ACCEPT \n ========> Value: {1} / TS: {2}",
                mostRecentslot, value, ts);

            // enviar accept(<value mais recente>) a todos
            // TODO: meter isto assincrono (tasks ou threads?)
            // esperar por maioria (para efeitos de historico)
            var acceptedResponses = new List<bool>();
            frontends.ForEach(server =>
            {
                new Thread(() =>
                {
                    Console.WriteLine($"Proposer: ACCEPT TO {server.ServerAddress} SENT");
                    var status = server.Accept(mostRecentslot, timestampId, value);
                    Console.WriteLine($"Proposer: ACCEPT TO {server.ServerAddress} REPLY");

                    lock (acceptedResponses)
                    {
                        acceptedResponses.Add(status);
                        Monitor.PulseAll(acceptedResponses);
                    }

                }).Start();

            });

            lock (acceptedResponses)
            {
                //espera pela maioria das respostas
                while (acceptedResponses.Count < majority)
                    Monitor.Wait(acceptedResponses);

                //processa respostas
                foreach (var status in acceptedResponses)
                {
                    if (!status)
                    {
                        timestampId += frontends.Count;
                        break;
                    }
                }
            }
        }
    }
}