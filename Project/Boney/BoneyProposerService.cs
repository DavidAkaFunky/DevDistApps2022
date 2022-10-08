using Grpc.Core;

namespace DADProject
{
    public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
    {
        private BoneyProposer proposer;

        public BoneyProposerService(int id, string[] servers) 
        { 
            this.proposer = new(id, servers); 
        }

        public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
        {
            int slot = request.Slot;
            int outValue;
            lock (proposer)
            {
                // Make sure it has no concluded consensus value and it hasn't started consensus already
                if (!multiPaxos.History.TryGetValue(slot, out outValue) && !multiPaxos.Slots.TryGetValue(slot))
                {
                    outValue = -1; // Otherwise it will be a positive value and Bank will use it as a "canned" consensus reply

                    Task runConsensus = Task.Run(() => multiPaxos.RunConsensus(slot, request.InValue));
                }
            }
            CompareAndSwapReply reply = new() { OutValue = outValue };
            return Task.FromResult(reply);
        }
    }
}