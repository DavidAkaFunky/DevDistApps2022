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
    public class BoneyProposer
    {
        private int id;
        private List<GrpcChannel> multiPaxosServers = new();

        public BoneyProposer(int id, string[] servers)
        {
            this.id = id;
            foreach (string s in servers)
            {
                AddServer(s);
            }
        }

        public int Id
        {
            get { return id; }
        }

        public void AddServer(string server)
        {
            GrpcChannel channel = GrpcChannel.ForAddress(server);
            multiPaxosServers.Add(channel);
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
    }
}

