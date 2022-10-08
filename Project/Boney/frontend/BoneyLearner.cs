using Grpc.Net.Client;

namespace DADProject;

public class BoneyLearner
{
    private readonly List<GrpcChannel> multiPaxosServers = new();

    public BoneyLearner(int id, string[] servers)
    {
        Id = id;
        foreach (var s in servers) AddServer(s);
    }

    public int Id { get; }

    public Dictionary<int, int> History { get; } = new();

    public void AddServer(string server)
    {
        var channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }
}