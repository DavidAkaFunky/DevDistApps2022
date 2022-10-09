using Grpc.Core;
using Grpc.Core.Interceptors;
using Timer = System.Timers.Timer;

namespace DADProject;

internal class Bank
{
    private static void Main(string[] args)
    {
        if (args.Length != 2)
            return;
        const int serverPort = 5000;
        const string serverHostname = "localhost";


        var serverString = "http://localhost:5000";
        var type = "primary"; //primary, onHold(?? if needed while deciding who's primary,
        //change var to bool if not needed), backup
        var serverID = 1;

        var slotDuration = 50000;
        var currentSlot = 1;

        int[] suspectedServers = { };
        var frozen = false;

        //-----------------------------------------Read states from input

        Server server = new()
        {
            Services = { ProjectBankService.BindService(new BankService()).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
        };
        server.Start();

        BankFrontend frontend = new(serverID);

        //for (string server: args)
        frontend.AddServer(serverString);

        Console.WriteLine("ChatServer server listening on port " + serverPort);

        void HandleTimer()
        {
            currentSlot++;
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