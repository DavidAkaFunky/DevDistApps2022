using Grpc.Core;
using Grpc.Core.Interceptors;
using Timer = System.Timers.Timer;

namespace DADProject;

internal class Bank
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
        int id;
        try
        {
            inputFile = new StreamReader(args[1]);
            id = int.Parse(args[0]);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        List<string> boneyServers = new();
        List<string> bankServers = new();
        string address = null;
        int numberOfSlots = -1;

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
                    bankServers.Add(tokens[3]);
                }
            }
            if (tokens[0] == "S")
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
        }

        if (address == null)
            throw new Exception("(This should never happen but) the config file doesn't contain an address for the server.");

        if (numberOfSlots < 0)
            throw new Exception("No number of slots given.");

        Uri ownUri = new Uri(address);

        var type = "primary"; //primary, onHold(?? if needed while deciding who's primary,
        var slotDuration = 50000;
        var currentSlot = 1;
        int[] suspectedServers = { };
        var frozen = false;

        //-----------------------------------------Read states from input

        Server server = new()
        {
            Services = { ProjectBankService.BindService(new BankService()).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(ownUri.Host, ownUri.Port, ServerCredentials.Insecure) }
        };
        server.Start();

        BankFrontend frontend = new(id, bankServers, boneyServers);

        Console.WriteLine("Bank server listening on port " + ownUri.Port);

        void HandleTimer()
        {
            currentSlot++;
            if (currentSlot > numberOfSlots)
            {
                // TODO: Maybe wait until everything was finished, but how?
                return;
            }
            Console.WriteLine("--NEW SLOT: {0}--", currentSlot);
            frontend.RequestCompareAndSwap(currentSlot);
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