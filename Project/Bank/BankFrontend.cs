using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;
/*
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
} */

public class BankFrontend
{
    private readonly int _clientId;
    private int _seq;
    private readonly List<GrpcChannel> _bankServers = new();

    public BankFrontend(int clientId, List<string> serverAddress)
    {
        _clientId = clientId;
        _seq = 0;
        serverAddress.ForEach(addr => _bankServers.Add(GrpcChannel.ForAddress(addr)));
    }

    public async void ReadBalance()
    {
        Metadata metadata = new();
        metadata.SenderId = _clientId;
        metadata.Seq = _seq++;
        metadata.Ack = -1;
        _bankServers.ForEach(channel =>
        {
            var stub = new ProjectBankService.ProjectBankServiceClient(channel);
        });

    }

    public void Deposit(double amount)
    {
        Metadata metadata = new();
        metadata.SenderId = _clientId;
        metadata.Seq = _seq++;
        metadata.Ack = -1;
    }

    public void Withdraw(double amount)
    {
        Metadata metadata = new();
        metadata.SenderId = _clientId;
        metadata.Seq = _seq++;
        metadata.Ack = -1;
    }

    public void SendCompareSwapResult(int slot, int value)
    {
        Metadata metadata = new();
        metadata.SenderId = _clientId;
        metadata.Seq = _seq++;
        metadata.Ack = -1;

    }
}