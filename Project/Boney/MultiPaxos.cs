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
        private Dictionary<int, int[]> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]

        public MultiPaxos(int id) { this.id = id; }

        public int Id
        { 
            get { return id; } 
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

        public int RunConsensus(int slot, int inValue)
        {
            slots.Add(slot, new int[] { inValue, id, id });
            if (id > 0)
            {
                foreach (GrpcChannel channel in multiPaxosServers)
                {
                    CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                    var client = new ProjectBoneyService.ProjectBoneyServiceClient(interceptingInvoker);
                    PrepareRequest request = new PrepareRequest { Slot = slot, Id = id };
                    PromiseReply reply = client.Prepare(request);
                    if (reply.Id > id)
                    {
                        // TODO: Stop? id += 3 and call RunConsensus again?
                    }
                    else if (reply.Id > slots[slot][2])
                    {
                        slots[slot][2] = reply.Id;
                        slots[slot][0] = reply.Value;
                    }
                }
            }
            // while (!majority)
            // x = WaitForAnswer(); //callback
            foreach (GrpcChannel channel in multiPaxosServers)
            {
                CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
                var client = new ProjectBoneyService.ProjectBoneyServiceClient(interceptingInvoker);
                AcceptRequest request = new AcceptRequest { Slot = slot, Id = slots[slot][2], Value = slots[slot][0] };
                AcceptReply reply = client.Accept(request);
                if (!reply.Status)
                {
                    // TODO: ???
                }
            }
            return slots[slot][0];
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
