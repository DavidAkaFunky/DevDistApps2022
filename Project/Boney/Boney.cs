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

        List<string> boneyServers = new();
        List<string> bankClients = new();
        string? address = null;
        int numberOfSlots = -1; // Is this needed for Boney servers? Hmmm
        int i = 0;
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
                        if (int.Parse(tokens[1]) == id)
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
            else if (tokens[0] == "T" || tokens[0] == "D")
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

        Dictionary<int, Dictionary<int, string>> serversStatus = new();
        Dictionary<int, bool> isFrozen = new();

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
            int slotNumber; 
            if (tokens[0] != "F")
            {
                Console.Error.WriteLine("Invalid slot details");
                return;
            }
        
            try
            {
                slotNumber = int.Parse(tokens[1]);
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Invalid slot details");
                return;
            }

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
                if (id == serverID)
                    isFrozen[slotNumber] = tokens[j+1] == "F";
                else
                {
                    if (!serversStatus.ContainsKey(slotNumber))
                        serversStatus[slotNumber] = new();
                    serversStatus[slotNumber][serverID] = tokens[j+2];
                }
            }
            ++i;
        }

        if (address is null)
            throw new Exception("(This should never happen but) the config file doesn't contain an address for the server.");
        if (numberOfSlots < 0)
            throw new Exception("No number of slots given.");

        Uri ownUri = new Uri(address);
        int currentSlot = 1;
        int slotDuration = 100000;

        Server server = new()
        {
            Services = { ProjectBoneyProposerService.BindService(new BoneyProposerService(id, boneyServers)),
                         ProjectBoneyAcceptorService.BindService(new BoneyAcceptorService(address, boneyServers)),
                         ProjectBoneyLearnerService.BindService(new BoneyLearnerService(boneyServers, bankClients)) },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("Boney server listening on port " + ownUri.Port);

        // BoneyAcceptor = new BoneyAcceptor();

        void HandleTimer()
        {
            currentSlot++;
            Console.WriteLine("--NEW SLOT: {0}--", currentSlot);
        }

        Timer timer = new(slotDuration);
        timer.Elapsed += (sender, e) => HandleTimer();
        timer.Start();

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.ShutdownAsync().Wait();
    }
}

public class ServerInterceptor : Interceptor
{
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var callId = context.RequestHeaders.GetValue("dad");
        Console.WriteLine("DAD header: " + callId);
        return base.UnaryServerHandler(request, context, continuation);
    }
}