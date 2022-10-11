using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private BoneyProposer proposer;
    private int boneySlot;
    private Dictionary<int, List<int>> nonSuspectedServers;

    public BoneyProposerService(int id, Dictionary<int, List<int>> nonSuspectedServers, List<string> servers, int boneySlot)
    {
        this.nonSuspectedServers = nonSuspectedServers;
        this.proposer = new(id, servers);
        this.boneySlot = boneySlot;
    }
    public int BoneySlot {
        get { return boneySlot; }
        set { boneySlot = value; }
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        int slot = request.Slot;
        int outValue;
        // TODO: Check if its ID is the lowest on the list without suspicion, otherwise outValue = -1
        lock (proposer)
        {
            bool assumeLeader = nonSuspectedServers[boneySlot].Any() ? nonSuspectedServers[boneySlot].Min() == proposer.Id : false;
            // Make sure it has no concluded consensus value and it hasn't started consensus already
            if (!assumeLeader)
                outValue = -1;
            else if (!proposer.History.TryGetValue(slot, out outValue) || outValue < 0){
                outValue = -1;   
                Task runConsensus = Task.Run(() => proposer.RunConsensus(slot, request.InValue));
            }
        }
        Console.WriteLine($"Proposer {proposer.Id} received a CompareAndSwap request for slot {slot} with inValue {request.InValue} and outValue {outValue}");
        CompareAndSwapReply reply = new() { OutValue = outValue };
        return Task.FromResult(reply);
    }

    public override Task<ResultToProposerReply> ResultToProposer(ResultToProposerRequest request, ServerCallContext context)
    {
        lock (proposer)
        {
            proposer.AddOrSetHistory(request.Slot, request.Value);
        }
        ResultToProposerReply reply = new();
        return Task.FromResult(reply);
    }

}