using Grpc.Core;
using Grpc.Core.Interceptors;
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
        else if (args.Length > 2)
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
        int numberOfSlots = -1; // Is this needed for Boney servers? Hmmm
        int i = 0;
        int slotDuration = -1;
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
                        int boneyServerID = int.Parse(tokens[1]);
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
            foreach (var c in new string[] { ",", "(", ")" })
                lines[i] = lines[i].Replace(c, string.Empty);

            string[] tokens = lines[i].Split();
            int slotNumber = int.Parse(tokens[1]); 

            for (int j = 2; j < tokens.Length; j += 3)
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
                    nonSuspectedServers[slotNumber] = new();
                if (id == serverID)
                {
                    isFrozen[slotNumber] = tokens[j + 1] == "F";
                    if (!isFrozen[slotNumber])
                        nonSuspectedServers[slotNumber].Add(serverID);
                }
                else if (tokens[j + 2] == "NS")
                    nonSuspectedServers[slotNumber].Add(serverID);
            }
            ++i;
        }

        if (address is null)
            throw new Exception("(This should never happen but) the config file doesn't contain an address for the server.");
        
        if (numberOfSlots < 0)
            throw new Exception("No number of slots given.");
        
        if (slotDuration < 0)
            throw new Exception("No slot duration given.");

        var boneyToBankfrontends = new List<BoneyToBankFrontend>();
        var boneyToBoneyfrontends = new List<BoneyToBoneyFrontend>();
        
        bankClients.ForEach(serverAddr => boneyToBankfrontends.Add(new(id, serverAddr)));
        boneyServers.ForEach(serverAddr => boneyToBoneyfrontends.Add(new(id, serverAddr)));

        var proposerService = new BoneyProposerService(id, boneyToBoneyfrontends);
        var acceptorService = new BoneyAcceptorService(id, boneyToBoneyfrontends);
        var learnerService = new BoneyLearnerService(id, boneyToBankfrontends, boneyToBoneyfrontends);

        var ownUri = new Uri(address);
        var server = new Server()
        {
            Services = { ProjectBoneyProposerService.BindService(proposerService),
                         ProjectBoneyAcceptorService.BindService(acceptorService),
                         ProjectBoneyLearnerService.BindService(learnerService) },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        PrintHeader();

        Console.WriteLine("Server " + ownUri.Host + " listening on port " + ownUri.Port);

        // BoneyAcceptor = new BoneyAcceptor();

        void HandleTimer()
        {
            proposerService.CurrentSlot++;
            //if (currentSlot > numberOfSlots)
            //{
            //    // TODO: Maybe wait until everything was finished, but how?
            //    server.ShutdownAsync().Wait();
            //    Environment.Exit(0);
            //}
            Console.WriteLine("--NEW SLOT: {0}--", proposerService.CurrentSlot);
        }

        Timer timer = new(slotDuration);
        timer.Elapsed += (sender, e) => HandleTimer();
        timer.Start();

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
}