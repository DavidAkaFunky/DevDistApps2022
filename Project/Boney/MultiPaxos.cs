using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Security.Principal;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Channels;

namespace DADProject
{
    public class MultiPaxos
    {
        private List<GrpcChannel> multiPaxosServers = new();
        private ClientInterceptor clientInterceptor = new();
        private int id;
        private Dictionary<int, int> history = new();
        private Dictionary<int, int[]> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]>

        public MultiPaxos(int id) { this.id = id; }

        public int Id
        { 
            get { return id; } 
        }

        public Dictionary<int, int> History
        {
            get { return history; }
        }

        public Dictionary<int, int[]> Slots
        {
            get { return slots; }
        }


        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            multiPaxosServers.Add(channel);
        }

        public void AddOrSetSlot(int slot, int[] values)
        { 
            slots[slot] = values;
        }

        public void RunConsensus(int slot, int inValue)
        {
            slots.Add(slot, new int[] { inValue, id, id });
            if (id > 0)
            {
                // TODO: This is running synchronously + It only needs to wait for a majority :)
                foreach (GrpcChannel channel in multiPaxosServers)
                {
                    CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                    var client = new ProjectBoneyService.ProjectBoneyServiceClient(interceptingInvoker);
                    PrepareRequest request = new() { Slot = slot, Id = id };
                    PromiseReply reply = client.Prepare(request);
                    if (reply.Id > id)
                    {
                        id += 3;
                        return;
                    }
                    else if (reply.Id > slots[slot][2])
                    {
                        slots[slot][2] = reply.Id;
                        slots[slot][0] = reply.Value;
                    }
                }
            }
            // TODO: This is also running synchronously + It only needs to wait for a majority :)
            foreach (GrpcChannel channel in multiPaxosServers)
            {
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                var client = new ProjectBoneyService.ProjectBoneyServiceClient(interceptingInvoker);
                AcceptRequest request = new() { Slot = slot, Id = slots[slot][2], Value = slots[slot][0] };
                AcceptReply reply = client.Accept(request);
                if (!reply.Status)
                {
                    id += 3;
                    return;
                }
            }
            // TODO: Call clients to inform of the consensus' value (Should it be done here?)
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
