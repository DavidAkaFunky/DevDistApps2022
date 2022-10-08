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


namespace DADProject;

public class BoneyAcceptor
{
    private int id;

    private List<GrpcChannel> multiPaxosServers = new();
    private Dictionary<int, Slot> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]>
    private BoneyInterceptor boneyInterceptor = new();

    public BoneyAcceptor(int id, string[] servers)
    {
        this.id = id;
        foreach (string s in servers)
            AddServer(s);
    }

    public int Id
    {
        get { return id; }
    }

    public Dictionary<int, Slot> Slots
    {
        get { return slots; }
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

    public void SendAccepted(int slot, int value)
    {
        foreach (GrpcChannel channel in multiPaxosServers)
        {
            Task.Run(() =>
            {
                CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor);
                var client = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(interceptingInvoker);
                AcceptedToLearnerRequest request = new() { Slot = slot, Id = id, Value = value };
                AcceptedToLearnerReply reply = client.AcceptedToLearner(request);
            });
        }
    }
}
