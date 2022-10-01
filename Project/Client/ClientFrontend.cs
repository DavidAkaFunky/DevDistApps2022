using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Grpc.Core.Interceptors.Interceptor;

namespace DADProject
{

    internal class ClientFrontend
    {
        private List<GrpcChannel> bankServers = new(); // recommended 

        public ClientFrontend()
        {
        }

        public void AddServer(string server)
        {
            var clientInterceptor = new ClientInterceptor();
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            bankServers.Add(channel);

            //Keeping this here to use in the future, it's not meant to be here
            //CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
            //var client = new ProjectService.ProjectServiceClient(interceptingInvoker);
            //PerfectChannelRequest registerRequest = new PerfectChannelRequest { Message = "test" };
            //PerfectChannelReply reply = client.Test(registerRequest);
        }

        public void DeleteServers()
        {
            foreach (GrpcChannel server in bankServers)
            {
                server.ShutdownAsync();
                bankServers.Remove(server);
            }
                
        }
    }

    public class ClientInterceptor : Interceptor
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
