using System.Collections.Concurrent;
using Grpc.Core;
using Timer = System.Timers.Timer;

namespace DADProject;

internal class Boney
{
    private static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Too few arguments: [id] [configPath]");
            return;
        }

        if (args.Length > 2)
        {
            Console.Error.WriteLine("Too many arguments: [id] [configPath]");
            return;
        }

        string[] lines;
        int id;
        try
        {
            lines = File.ReadAllLines(args[1]);
            id = int.Parse(args[0]);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        var boneyServerIDs = new List<int>();
        var boneyServers = new List<string>();
        var bankClients = new List<string>();
        string? address = null;
        var numberOfSlots = -1; // Is this needed for Boney servers? Hmmm
        var i = 0;
        var slotDuration = -1;
        while (i < lines.Length)
        {
            var tokens = lines[i].Split(' ');

            if (tokens[0] == "F")
                break;

            if (tokens[0] == "P")
            {
                if (tokens.Length < 3)
                    throw new Exception("At least 3 arguments needed for 'P' lines");
                if (tokens[2] == "boney")
                {
                    if (tokens.Length != 4)
                        throw new Exception("Exactly 4 arguments needed for 'P boney' lines");
                    boneyServers.Add(tokens[3]);
                    try
                    {
                        var boneyServerID = int.Parse(tokens[1]);
                        boneyServerIDs.Add(boneyServerID);
                        if (boneyServerID == id)
                            address = tokens[3];
                    }
                    catch (FormatException)
                    {
                        Console.Error.WriteLine("Invalid id for Boney server");
                        return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
                    }
                }
                else if (tokens[2] == "bank")
                {
                    if (tokens.Length != 4)
                        throw new Exception("Exactly 4 arguments needed for 'P bank' lines");
                    bankClients.Add(tokens[3]);
                }
            }
            else if (tokens[0] == "S")
            {
                if (tokens.Length != 2)
                    throw new Exception("Exactly 2 arguments needed for 'S' lines");
                try
                {
                    numberOfSlots = int.Parse(tokens[1]);
                }
                catch (FormatException)
                {
                    Console.Error.WriteLine("Invalid value for number of slots");
                    return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
                }
            }
            else if (tokens[0] == "D")
            {
                try
                {
                    slotDuration = int.Parse(tokens[1]);
                }
                catch (FormatException)
                {
                    Console.Error.WriteLine("Invalid value for the slot duration");
                    return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
                }
            }
            else if (tokens[0] == "T")
            {
                ++i;
                continue;
            }
            else
            {
                Console.Error.WriteLine("Invalid line");
                return;
            }

            ++i;
        }

        var nonSuspectedServers = new Dictionary<int, List<int>>();
        var isFrozen = new Dictionary<int, bool>();

        if (lines.Length - i != numberOfSlots)
        {
            Console.Error.WriteLine("Invalid number of slot details");
            return;
        }

        while (i < lines.Length)
        {
            foreach (var c in new[] { ",", "(", ")" })
                lines[i] = lines[i].Replace(c, string.Empty);

            var tokens = lines[i].Split();
            var slotNumber = int.Parse(tokens[1]);

            for (var j = 2; j < tokens.Length; j += 3)
            {
                int serverID;
                try
                {
                    serverID = int.Parse(tokens[j]);
                }
                catch (FormatException)
                {
                    Console.Error.WriteLine("Invalid slot details");
                    return;
                }

                if (!boneyServerIDs.Contains(serverID))
                    continue;
                if (!nonSuspectedServers.ContainsKey(slotNumber))
                    nonSuspectedServers[slotNumber] = new List<int>();
                if (id == serverID)
                {
                    isFrozen[slotNumber] = tokens[j + 1] == "F";
                    if (!isFrozen[slotNumber])
                        nonSuspectedServers[slotNumber].Add(serverID);
                }
                else if (tokens[j + 2] == "NS")
                {
                    nonSuspectedServers[slotNumber].Add(serverID);
                }
            }

            ++i;
        }

        if (address is null)
            throw new Exception(
                "(This should never happen but) the config file doesn't contain an address for the server.");

        if (numberOfSlots < 0)
            throw new Exception("No number of slots given.");

        if (slotDuration < 0)
            throw new Exception("No slot duration given.");


        //======================================================FRONTENDS==========================================

        var boneyToBankfrontends = new List<BoneyToBankFrontend>();
        var boneyToBoneyfrontends = new List<BoneyToBoneyFrontend>();

        bankClients.ForEach(serverAddr => boneyToBankfrontends.Add(new BoneyToBankFrontend(id, serverAddr)));
        boneyServers.ForEach(serverAddr => boneyToBoneyfrontends.Add(new BoneyToBoneyFrontend(id, serverAddr)));

        //==================================================CONSENSUS_INFO=========================================


        var slotsHistory = new ConcurrentDictionary<int, int>();
        var slotsInfo = new ConcurrentDictionary<int, Slot>();
        var isPerceivedLeader = new Dictionary<int, bool>(); 

        foreach (var slot in nonSuspectedServers)
            // pensa que e lider se a lista de servidores vivos para um slot n estiver vazia (duh)
            // e se o minimo dos valores da lista for ele proprio
            isPerceivedLeader[slot.Key] = slot.Value.Count > 0 && slot.Value.Min() == id;

        //======================================================SERVICES===========================================

        var proposerService = new BoneyProposerService(id, slotsHistory, slotsInfo);
        var acceptorService = new BoneyAcceptorService(id, boneyToBoneyfrontends, slotsInfo);
        var learnerService = new BoneyLearnerService(id, boneyToBankfrontends, boneyServers.Count, slotsHistory);

        var ownUri = new Uri(address);
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
        Thread.Sleep(5000);

        PrintHeader();
        Console.WriteLine("Server " + ownUri.Host + " listening on port " + ownUri.Port);

        Paxos(
            id,
            slotDuration,
            boneyToBoneyfrontends,
            isPerceivedLeader, 
            slotsInfo,
            slotsHistory);

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

    public static void Paxos(
        int id,
        int slotDuration,
        List<BoneyToBoneyFrontend> frontends,
        Dictionary<int, bool> isPerceivedLeader, 
        ConcurrentDictionary<int, Slot> slotsInfo,
        ConcurrentDictionary<int, int> slotHistory)
    {
        int boneySlot = 1;
        int timestampId = id;
        Slot slotToPropose;

        //========================================BONEY_SLOT_TIMER====================================================

        Timer timer = new(slotDuration);

        void HandleSlotTimer()
        {
            boneySlot++;
            Console.WriteLine("--NEW SLOT: {0}--", boneySlot);
        }

        timer.Elapsed += (sender, e) => HandleSlotTimer();
        timer.Start();
        Console.WriteLine("--NEW SLOT: {0}--", boneySlot);

        //======================================================================================================
        //TODO ----> Locks

        while(true)
        {
            //Loop enquanto nao sou lider
            if (!isPerceivedLeader[boneySlot]) continue;

            var mostRecentslot = slotHistory.Count + 1;

            //sou lider, verificar se tenho valor para propor para o slot mais recente
            if (!slotsInfo.TryGetValue(mostRecentslot, out slotToPropose)) continue;

            // se nao (dicionario vazio ou o menor valor n é o meu), desisto 

            Console.WriteLine("Proposer: " + mostRecentslot + ": STARTING CONSENSUS" );

            var value = slotToPropose.CurrentValue;
            var ts = 0;
            var stop = false;

            // se eu for o lider:
            // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
            // espero por maioria de respostas (Promise com valor, id da msg mais recente)
            // escolher valor mais recente das Promises

            if (timestampId != 1)
            {
                Console.WriteLine("Proposer: {0}: Send Prepare with timestamp {1}", mostRecentslot, timestampId);

                frontends.ForEach(server =>
                {
                    var response = server.Prepare(mostRecentslot, timestampId);

                    if (response.Value == -1 && response.WriteTimestamp == -1)
                    {
                        Console.WriteLine("Proposer: RECEIVED **NACK**");
                        stop = true;
                    }
                    else if (response.WriteTimestamp > ts)
                    {
                        Console.WriteLine("Procposer: RECEIVED **ACK**");
                        value = response.Value;
                        ts = response.WriteTimestamp;
                    }
                });
            }

            if (stop)
            {
                //MAYBE aumentar timestampId
                continue;
            }

            Console.WriteLine("Proposer: {0}: Send ACCEPT \n ========> Value: {1} / TS: {2}",
                mostRecentslot, value, ts);

            // enviar accept(<value mais recente>) a todos
            // TODO: meter isto assincrono (tasks ou threads?)
            // esperar por maioria (para efeitos de historico)
            frontends.ForEach(server =>
            {
                Console.WriteLine($"Proposer: ACCEPT TO {server.ServerAddress} SENT");
                server.Accept(mostRecentslot, timestampId, value);
                Console.WriteLine($"Proposer: ACCEPT TO {server.ServerAddress} REPLY");
            });

        }
    }
}