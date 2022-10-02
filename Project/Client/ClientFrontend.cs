using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System.Globalization;

namespace DADProject
{

    public class ClientFrontend
    {
        private List<GrpcChannel> bankServers = new();
        ClientInterceptor clientInterceptor = new();

        public ClientFrontend()
        {
        }

        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            bankServers.Add(channel);
        }

        public void DeleteServers()
        {
            foreach (GrpcChannel server in bankServers)
            {
                server.ShutdownAsync().Wait();
                bankServers.Remove(server);
            }
                
        }

        public void ReadBalance()
        {
            foreach (GrpcChannel channel in bankServers)
            {
                //Keeping this here to use in the future, it's not meant to be here
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                var client = new ProjectService.ProjectServiceClient(interceptingInvoker);
                ReadBalanceRequest request = new();
                ReadBalanceReply reply = client.ReadBalance(request);
                Console.WriteLine("Balance: " + reply.Balance.ToString("C", CultureInfo.CurrentCulture));
            }
        }

        public void Deposit(double amount)
        {
            // TODO: Call Deposit
        }

        public void Withdraw(double amount)
        {
            // TODO: Call Withdraw
        }
    }

    internal class ClientInterceptor : Interceptor
    {
        // private readonly ILogger logger;

        //public GlobalServerLoggerInterceptor(ILogger logger) {
        //    this.logger = logger;
        //}

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context, BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {

            Metadata metadata = context.Options.Headers; // read original headers
            if (metadata == null) { metadata = new Metadata(); }
            metadata.Add("dad", "dad-value"); // add the additional metadata

            // create new context because original context is readonly
            ClientInterceptorContext<TRequest, TResponse> modifiedContext =
                new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                    new CallOptions(metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }
    }
}
