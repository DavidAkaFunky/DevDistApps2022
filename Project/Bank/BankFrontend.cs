using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace DADProject
{

    public class BankFrontend
    {
        private List<GrpcChannel> boneyServers = new();
        private ClientInterceptor clientInterceptor = new();

        public BankFrontend() { }

        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            boneyServers.Add(channel);
        }

        public void DeleteServers()
        {
            foreach (GrpcChannel server in boneyServers)
            {
                server.ShutdownAsync().Wait();
                boneyServers.Remove(server);
            }
                
        }

        public void RequestCompareAndSwap(int slot, int perceivedLeader)
        {
            foreach (GrpcChannel channel in boneyServers)
            {
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                var client = new ProjectBankService.ProjectBankServiceClient(interceptingInvoker);
                Task runConsensus = Task.Run(() =>
                {
                    CompareAndSwapRequest request = new() { Slot = slot, InValue = perceivedLeader };
                    CompareAndSwapReply reply = client.CompareAndSwap(request);
                    // if (reply.OutValue > 0)
                        // TODO: Start 2PC?
                });
            }
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
            if (metadata == null)
                metadata = new Metadata();
            metadata.Add("dad", "dad-value"); // add the additional metadata

            // create new context because original context is readonly
            ClientInterceptorContext<TRequest, TResponse> modifiedContext =
                new (context.Method, context.Host,
                    new (metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }
    }
}
