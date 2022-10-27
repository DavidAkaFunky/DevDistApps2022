using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace DADProject;

public class BankToBoneyFrontend
{
    private readonly int id;
    private int seq;
    private readonly ProjectBoneyProposerService.ProjectBoneyProposerServiceClient client;
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup

    public BankToBoneyFrontend(int id, string serverAddress, ConcurrentDictionary<int, int> isPrimary)
    {
        this.id = id;
        this.seq = 0;
        this.client = new(GrpcChannel.ForAddress(serverAddress));
        this.isPrimary = isPrimary;
    }

    public void RequestCompareAndSwap(int slot)
    {
        //Console.WriteLine("SENDING CS FOR SLOT " + slot);
        
        CompareAndSwapRequest request = new() { Slot = slot, InValue = id };
        var reply = client.CompareAndSwap(request);
        
        //Console.WriteLine("GOT INITIAL COMPARE AND SWAP VALUE FOR SLOT " + slot + " AND THE VALUE IS " + reply.OutValue);
        if (reply.OutValue > 0)
        {
            isPrimary[slot] = reply.OutValue;
        }
    }
}