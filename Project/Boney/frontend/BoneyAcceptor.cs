using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyAcceptor
{
    private List<GrpcChannel> multiPaxosServers = new();
    private Dictionary<int, Slot> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]>
    // private BoneyInterceptor boneyInterceptor;
    private readonly int id;

    public BoneyAcceptor(int id, List<string> servers)
    {
        this.id = id;
        // this.boneyInterceptor = new(address);
        foreach (string s in servers)
            AddServer(s);
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

    public void SendAcceptedToLearners(int slot, int value)
    {
        foreach (GrpcChannel channel in multiPaxosServers)
        {
            Thread thread = new(() =>
            {
                // CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor).Intercept();
                var client = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(channel);
                AcceptedToLearnerRequest request = new() { Slot = slot, Id = id, Value = value };
                AcceptedToLearnerReply reply = client.AcceptedToLearner(request);
            });
            thread.Start();
        }
    }
}
