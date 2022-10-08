using Grpc.Net.Client;

namespace DADProject;

public class BoneyProposer
{
    private readonly List<GrpcChannel> multiPaxosServers = new();

    public BoneyProposer(int id, string[] servers)
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

    /*public PromiseReply SendPrepare(GrpcChannel channel, int slot)
    {
        var interceptingInvoker = channel.Intercept(clientInterceptor);
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
        PrepareRequest request = new() { Slot = slot, Id = Id };
        PromiseReply reply = client.Prepare(request);
        return reply;
    }

    public bool SendAccept(GrpcChannel channel, int slot)
    {
        var interceptingInvoker = channel.Intercept(clientInterceptor);
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
        AcceptRequest request = new() { Slot = slot, Id = slots[slot].ReadTimestamp, Value = slots[slot].CurrentValue };
        AcceptReply reply = client.Accept(request);
        return reply.Status;
    }*/
}