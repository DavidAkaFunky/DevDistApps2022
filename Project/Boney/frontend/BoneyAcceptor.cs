using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyAcceptor
{
    private readonly BoneyInterceptor boneyInterceptor = new();

    private readonly List<GrpcChannel> multiPaxosServers = new();
    private Dictionary<int, Slot> slots = new(); // <slot, [currentValue, writeTimestamp, readTimestamp]>

    public BoneyAcceptor(int id, string[] servers)
    {
        Id = id;
        foreach (var s in servers) AddServer(s);
    }

    public int Id { get; }

    public void AddServer(string server)
    {
        var channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }

    // public void SendAccepted(int value)
    // {
    //     foreach (var channel in multiPaxosServers)
    //         Task.Run(() =>
    //         {
    //             var interceptingInvoker = channel.Intercept(boneyInterceptor);
    //             var client = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(interceptingInvoker);
    //             AcceptedToLearnerRequest request = new() { Slot = slot, Id = Id, Value = value };
    //             AcceptedToLearnerReply reply = client.AcceptedToLearner(request);
    //         })
    // }
}