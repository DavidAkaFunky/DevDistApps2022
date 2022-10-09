using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Data;
using System.Diagnostics;
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

        StreamReader inputFile;
        try
        {
            inputFile = new StreamReader(args[1]);
        }
        catch (Exception)
        {
            throw; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        int id = int.Parse(args[0]);
        List<string> boneyServers = new();
        List<string> bankClients = new();
        string address = null;

        while (inputFile.ReadLine() is { } line)
        {
            var tokens = line.Split(' ');

            if (tokens[0] == "P")
            {
                if (tokens.Length < 3)
                    throw new Exception("At least 3 arguments needed for 'P' lines");
                if (tokens[2] == "boney")
                {
                    if (tokens.Length != 4)
                        throw new Exception("Exactly 4 arguments needed for 'P boney' lines");
                    boneyServers.Add(tokens[3]);
                    if (int.Parse(tokens[1]) == id)
                        address = tokens[3];
                }
                else if (tokens[2] == "bank")
                {
                    if (tokens.Length != 4)
                        throw new Exception("Exactly 4 arguments needed for 'P bank' lines");
                    bankClients.Add(tokens[3]);
                }
            }
        }

        if (address == null)
            throw new Exception("(This shouldn never happen but) the config file doesn't contain an address for the server.");

        Uri ownUri = new Uri(address);
        int currentSlot = 1;
        int slotDuration = 100000;
        int[] suspectedServers = new int[] { };
        bool frozen = false;

        Server server = new()
        {
            Services = { ProjectBoneyProposerService.BindService(new BoneyProposerService(id, boneyServers)),
                         ProjectBoneyAcceptorService.BindService(new BoneyAcceptorService(address, boneyServers)),
                         ProjectBoneyLearnerService.BindService(new BoneyLearnerService(boneyServers, bankClients)) },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("ChatServer server listening on port " + ownUri.Port);

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