using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBoneyFrontend
{
    private readonly int clientID;
    private int seq;
    private readonly GrpcChannel channel;

    public BankToBoneyFrontend(int clientID, string serverAddress)
    {
        this.clientID = clientID;
        seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    public void RequestCompareAndSwap(int slot)
    {
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        Thread thread = new(() =>
        {
            CompareAndSwapRequest request = new() { Slot = slot, InValue = clientID };
            var reply = client.CompareAndSwap(request);
            Console.WriteLine("Request Delivered! Answered: {0}", reply.OutValue);
        });
        thread.Start();
    }
}