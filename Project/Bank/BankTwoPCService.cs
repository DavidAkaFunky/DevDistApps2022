using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankTwoPCService : ProjectBankTwoPCService.ProjectBankTwoPCServiceBase
{
    private readonly int id;
    private int currentSlot = 1;
    
    private readonly ConcurrentDictionary<int, int> isPrimary; //  primary/backup

    private readonly TwoPhaseCommit TwoPC;
    private readonly Dictionary<int, bool> isFrozen;

    public BankTwoPCService(int id, ConcurrentDictionary<int, int> isPrimary, TwoPhaseCommit TwoPC, Dictionary<int, bool> isFrozen)
    {
        this.id = id;
        this.isPrimary = isPrimary;
        this.TwoPC = TwoPC;
        this.isFrozen = isFrozen;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    // All tentatives/commits coming from anyone other than the leader will be ignored!
    private bool CheckLeadership(int slot, int senderID) {

        if (slot > currentSlot || isFrozen[currentSlot]) // Dumb check (it should not happen!)
            return false;

        // Checking if the leader has always been senderID since the given slot
        for (int i = slot; i <= currentSlot; ++i)
            if (isPrimary[i] != senderID)
                return false;

        return true;
    }

    public override Task<ListPendingRequestsReply> ListPendingRequests(ListPendingRequestsRequest request, ServerCallContext context)
    {
        return Task.FromResult(TwoPC.ListPendingRequest(request.GlobalSeqNumber));
    }

    public override Task<TwoPCTentativeReply> TwoPCTentative(TwoPCTentativeRequest request, ServerCallContext context)
    {
        // Should we assume the commands will never be in the dictionary more than once?
        // I.e., since the leader coordinates the messages, even the receiver had it once,
        // cleanup will remove it, so the new version can arrive without repetition

        var reply = new TwoPCTentativeReply() { Status = false };

        if (CheckLeadership(request.Command.Slot, request.SenderId))
        {
            reply.Status = TwoPC.AddTentative(
                request.Command.GlobalSeqNumber, 
                new(request.Command.Slot, 
                    request.Command.ClientId, 
                    request.Command.ClientSeqNumber, 
                    request.Command.Type, 
                    request.Command.Amount));
            
        }

        return Task.FromResult(reply);
    }

    public override Task<TwoPCCommitReply> TwoPCCommit(TwoPCCommitRequest request, ServerCallContext context)
    {
        var reply = new TwoPCCommitReply() { Status = false };

        if (CheckLeadership(request.Command.Slot, request.SenderId))
        {
            reply.Status = true;
                
            TwoPC.AddCommitted(
                request.Command.GlobalSeqNumber,
                new(request.Command.Slot,
                    request.Command.ClientId,
                    request.Command.ClientSeqNumber,
                    request.Command.Type,
                    request.Command.Amount));
        }

        return Task.FromResult(reply);
    }

}