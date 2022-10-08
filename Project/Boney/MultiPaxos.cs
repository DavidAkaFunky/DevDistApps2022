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
    /*internal class PTask {
        
        private Task _task;
        private Thread _thread;
        private int _result;

        public Task Task {
            get => _task;
            set {
                _task = value;
            }
        }

        public Thread Thread {
            get => _thread;
            set {
                _thread = value;
            }
        }

        public int Result {
            get => _result;
            set {
                _result = value;
            }
        }

        public void Kill() {
            _thread.Abort();
        }
    }*/

    public class MultiPaxos
    {
        private List<GrpcChannel> multiPaxosServers = new();
        private ClientInterceptor clientInterceptor = new();
        private int id;
        private Dictionary<int, int> history = new();
        private Dictionary<int, Slot> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]>

        public MultiPaxos(int id) { this.id = id; }

        public int Id
        {
            get { return id; }
        }

        public Dictionary<int, int> History
        {
            get { return history; }
        }

        public Dictionary<int, Slot> Slots
        {
            get { return slots; }
        }

        public void CheckMajority(int responses, List<Task> tasks)
        {
            if (responses == multiPaxosServers.Count)
            {
                // foreach(Task task in tasks)
                // {
                //     if(!task.IsCompleted)
                //         // TODO
                // }
                tasks.Clear();
            }
        }

        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            multiPaxosServers.Add(channel);
        }

        public void AddOrSetSlot(int slot, Slot values)
        {
            slots[slot] = values;
        }

        public async void RunConsensus(int slot, int inValue)
        {
            slots.Add(slot, new(inValue, id, id));
            List<Task> tasks = new();
            if (id > 0)
            {
                int responses = 0;
                foreach (GrpcChannel channel in multiPaxosServers)
                {
                    tasks.Add(Task.Run(() => {
                        PromiseReply reply = SendPrepare(channel, slot);
                        if (reply.Id > id)
                        {
                            id += multiPaxosServers.Count;
                            return;
                        }
                        else if (reply.Id > slots[slot].ReadTimestamp)
                        {
                            slots[slot].ReadTimestamp = reply.Id;
                            slots[slot].CurrentValue = reply.Value;
                        }
                        //lock (responses) doesnt work
                        CheckMajority(++responses, tasks);
                    }));
                }
            }
            foreach (GrpcChannel channel in multiPaxosServers)
            {
                tasks.Add(Task.Run(() => {
                    if (!SendAccept(channel, slot))
                    {
                        id += multiPaxosServers.Count;
                        return;
                    }
                }));
            }
        }

        public PromiseReply SendPrepare(GrpcChannel channel, int slot)
        {
            CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
            PrepareRequest request = new() { Slot = slot, Id = id };
            PromiseReply reply = client.Prepare(request);
            return reply;
        }

        public bool SendAccept(GrpcChannel channel, int slot)
        {
            CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
            AcceptRequest request = new() { Slot = slot, Id = slots[slot].ReadTimestamp, Value = slots[slot].CurrentValue };
            AcceptReply reply = client.Accept(request);
            return reply.Status;
        }
        
        public bool SendAcceptedToLearners(int slot, int id, int timestamp){
            CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
            AcceptedToLearnerRequest request = new() { Slot = slot, Id = slots[slot].ReadTimestamp, Value = slots[slot].CurrentValue };
            AcceptedToLearnerReply reply = client.AcceptedToLearner(request);
            return reply.Status;
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
                new(context.Method, context.Host,
                    new(metadata, context.Options.Deadline,
                        context.Options.CancellationToken, context.Options.WriteOptions,
                        context.Options.PropagationToken, context.Options.Credentials));
            Console.Write("calling server...");
            TResponse response = base.BlockingUnaryCall(request, modifiedContext, continuation);
            return response;
        }
    }
}
