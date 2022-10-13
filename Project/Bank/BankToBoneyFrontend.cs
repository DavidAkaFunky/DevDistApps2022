using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace DADProject;

public class BankToBoneyFrontend
{
    private readonly int clientID;
    private int seq;
    private readonly GrpcChannel channel;
    private ConcurrentDictionary<int, bool> isPrimary; //  primary/backup

    public BankToBoneyFrontend(int clientID, string serverAddress, ConcurrentDictionary<int, bool> isPrimary)
    {
        this.clientID = clientID;
        this.seq = 0;
        this.channel = GrpcChannel.ForAddress(serverAddress);
        this.isPrimary = isPrimary;
    }

    public void RequestCompareAndSwap(int slot)
    {
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        Console.WriteLine("SENDING COMPARE AND SWAP REQUEST FOR SLOT " + slot);
        CompareAndSwapRequest request = new() { Slot = slot, InValue = clientID };
        var reply = client.CompareAndSwap(request);
        Console.WriteLine("GOT INITIAL COMPARE AND SWAP VALUE FOR SLOT " + slot + " AND THE VALUE IS " + reply.OutValue);
        if (reply.OutValue > 0 && reply.OutValue == clientID)
        {
            Console.WriteLine("I AM THE LEADER FOR THE SLOT " + slot);
            isPrimary[slot] = true;
        }
    }
}