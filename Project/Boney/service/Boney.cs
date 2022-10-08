using Grpc.Core;
using Grpc.Core.Interceptors;
using Timer = System.Timers.Timer;

namespace DADProject.service;

internal class Boney
{
    private static void Main()
    {
        var serverHostname = "localhost";
        var serverPort = 1000;

        string[] servers = { "http://localhost:8000" };

        int currentSlot = 1;
        int slotDuration = 100000;

        int[] suspectedServers = new int[] { };
        bool frozen = false;

        Server server = new()
        {
            Services = { ProjectBoneyProposerService.BindService(new BoneyProposerService(currentSlot, servers)).Intercept(new ServerInterceptor()),
                         ProjectBoneyAcceptorService.BindService(new BoneyAcceptorService(currentSlot, servers)).Intercept(new ServerInterceptor()),
                         ProjectBoneyLearnerService.BindService(new BoneyLearnerService(currentSlot, servers)).Intercept(new ServerInterceptor()) },
            Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
        };
        server.Start();

        Console.WriteLine("ChatServer server listening on port " + serverPort);

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