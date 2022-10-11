using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class BankFrontend
{
    private readonly List<GrpcChannel> bankServers = new();
    private readonly List<GrpcChannel> boneyServers = new();
    private readonly int serverID;

    public BankFrontend(int id, List<string> bankServers, List<string> boneyServers)
    {
        serverID = id;
        foreach (string s in bankServers)
            AddChannel(this.bankServers, s);
        foreach (string s in boneyServers)
            AddChannel(this.boneyServers, s);
    }

    public void AddChannel(List<GrpcChannel> channels, string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        channels.Add(channel);
    }

    public void DeleteServers()
    {
        foreach (var server in boneyServers)
        {
            server.ShutdownAsync().Wait();
            boneyServers.Remove(server);
        }
    }

    public void RequestCompareAndSwap(int slot)
    {
        foreach (var channel in boneyServers)
        {
            var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
            Thread thread = new(() =>
            {
                CompareAndSwapRequest request = new() { Slot = slot, InValue = serverID };
                var reply = client.CompareAndSwap(request);
                Console.WriteLine("Request Delivered! Answered: {0}", reply.OutValue);
            });
            thread.Start();
        }
    }
}