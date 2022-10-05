using Grpc.Core.Interceptors;
using Grpc.Core;

namespace DADProject
{
    class Bank
    {
        static void Main()
        {
            const int serverPort = 5000;
            const string serverHostname = "localhost";

            
            string serverString = "http://localhost:5000";
            string type = "primary";//primary, onHold(?? if needed while deciding who's primary,
                                    //change var to bool if not needed), backup
            int serverID = 1;

            int slotDuration = 50000;
            int currentSlot = 1;

            int[] suspectedServers = new int[] { };
            bool frozen = false;

            //-----------------------------------------Read states from input

            Server server = new()
            {
                Services = { ProjectBankService.BindService(new BankService()).Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
            };
            server.Start();

            BankFrontend frontend = new(id: serverID);
            
            //for (string server: args)
            frontend.AddServer(serverString);

            Console.WriteLine("ChatServer server listening on port " + serverPort);

            void HandleTimer()
            {
                currentSlot++;
                Console.WriteLine("--NEW SLOT: {0}--", currentSlot);
                frontend.RequestCompareAndSwap(currentSlot);
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
