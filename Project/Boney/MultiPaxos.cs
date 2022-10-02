using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace DADProject
{
    public class MultiPaxos
    {
        private List<GrpcChannel> multiPaxosServers = new();
        private ClientInterceptor clientInterceptor = new();
        private int id = 0;

        public MultiPaxos() { }

        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            multiPaxosServers.Add(channel);
        }

        public int RunConsensus(int slot, int inValue)
        {
            foreach (GrpcChannel channel in multiPaxosServers)
            {
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                var client = new ProjectBoneyService.ProjectBoneyServiceClient(interceptingInvoker);
                if (id > 0)
                    // TODO: Call Prepare(slot, id, inValue);
                    // Receive Promises, store in array, use most recent value if the most recent id <= own id
                    // (includes case where all are null except for its own because it sends the message to itself)
                    // Stop if id > own id, send accept otherwise
            }
            return 0;
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
