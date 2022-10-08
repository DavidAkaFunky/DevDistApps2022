using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private BoneyProposer proposer;

    public BoneyProposerService(int id, string[] servers)
    {
        proposer = new(id, servers);
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        int slot = request.Slot;
        int outValue;
        lock (proposer)
        {
            // Make sure it has no concluded consensus value and it hasn't started consensus already
            if (!proposer.History.TryGetValue(slot, out outValue) && !proposer.Slots.TryGetValue(slot))
            {
                outValue = -1; // Otherwise it will be a positive value and Bank will use it as a "canned" consensus reply

                Task runConsensus = Task.Run(() => proposer.RunConsensus(slot, request.InValue));
            }
        }
        CompareAndSwapReply reply = new() { OutValue = outValue };
        return Task.FromResult(reply);
    }
}