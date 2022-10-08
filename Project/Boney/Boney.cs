using Grpc.Core.Interceptors;
using Grpc.Core;

namespace DADProject
{
    class Boney
    {
        static void Main()
        {
            string serverHostname = "localhost";
            int serverPort = 1000;

            string[] servers = {"http://localhost:8000"};

            int currentSlot = 1;
            int slotDuration = 100000;

            int[] suspectedServers = new int[] {};
            bool frozen = false;
            
            Server server = new()
            {
                Services = { ProjectBoneyProposerService.BindService(new BoneyProposerService(id, servers))
                                                        .BindService(new BoneyAcceptorService(id, servers))
                                                        .BindService(new BoneyLearnerService(id, servers))
                                                        .Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
            };
            server.Start();

            Console.WriteLine("ChatServer server listening on port " + serverPort);

            void HandleTimer()
            {
                currentSlot++;
                Console.WriteLine("--NEW SLOT: {0}--", currentSlot);
            }

            System.Timers.Timer timer = new(interval: slotDuration);
            timer.Elapsed += (sender, e) => HandleTimer();
            timer.Start();

            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();

        }
    }

    public class ServerInterceptor : Interceptor
    {

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }

    }
}