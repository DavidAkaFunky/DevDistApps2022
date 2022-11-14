using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankTwoPCService : ProjectBankTwoPCService.ProjectBankTwoPCServiceBase
{
    private readonly Dictionary<int, int> _ack = new();
    private readonly object _ackLock = new();
    private readonly Dictionary<int, bool> isFrozen;
    private readonly ConcurrentDictionary<int, int> isPrimary; //  primary/backup

    private readonly TwoPhaseCommit TwoPC;

    public BankTwoPCService(ConcurrentDictionary<int, int> isPrimary, TwoPhaseCommit TwoPC,
        Dictionary<int, bool> isFrozen)
    {
        this.isPrimary = isPrimary;
        this.TwoPC = TwoPC;
        this.isFrozen = isFrozen;
    }

    public int CurrentSlot { get; set; } = 1;

    // All tentatives/commits coming from anyone other than the leader will be ignored!
    private bool CheckLeadership(int slot, int senderID)
    {
        // maybe don't wait here
        // or maybe while isPrimary[slot] == -1
        while (isFrozen[CurrentSlot]) ;

        if (slot > CurrentSlot)
            return false;

        // Checking if the leader has always been senderID since the given slot
        for (var i = slot; i <= CurrentSlot - 1; ++i)
            if (isPrimary[i] != senderID)
                return false;

        if (isPrimary[CurrentSlot] != senderID && isPrimary[CurrentSlot] != -1)
            return false;

        return true;
    }

    public override Task<ListPendingRequestsReply> ListPendingRequests(ListPendingRequestsRequest request,
        ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = 0;
            if (request.Seq != _ack[request.SenderId] + 1)
                return Task.FromResult(new ListPendingRequestsReply { Ack = _ack[request.SenderId] });
            _ack[request.SenderId] = request.Seq;
        }

        if (isFrozen[CurrentSlot])
            return Task.FromResult(new ListPendingRequestsReply { Status = false, Ack = request.Seq });

        return Task.FromResult(TwoPC.ListPendingRequest(request.GlobalSeqNumber, request.Seq));
    }

    public override Task<TwoPCTentativeReply> TwoPCTentative(TwoPCTentativeRequest request, ServerCallContext context)
    {
        // Should we assume the commands will never be in the dictionary more than once?
        // I.e., since the leader coordinates the messages, even if the receiver had it once,
        // cleanup will remove it, so the new version can arrive without repetition

        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = 0;
            if (request.Seq != _ack[request.SenderId] + 1)
                return Task.FromResult(new TwoPCTentativeReply { Ack = _ack[request.SenderId] });
            _ack[request.SenderId] = request.Seq;
        }

        var reply = new TwoPCTentativeReply { Status = -1, Ack = request.Seq };

        if (CheckLeadership(request.Command.Slot, request.SenderId))
            reply.Status = TwoPC.AddTentative(
                request.Command.GlobalSeqNumber,
                new ClientCommand(request.Command.Slot,
                    request.Command.ClientId,
                    request.Command.ClientSeqNumber,
                    request.Command.Type,
                    request.Command.Amount))
                ? 1
                : 0;

        return Task.FromResult(reply);
    }

    public override Task<TwoPCCommitReply> TwoPCCommit(TwoPCCommitRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = 0;
            if (request.Seq != _ack[request.SenderId] + 1)
                return Task.FromResult(new TwoPCCommitReply { Ack = _ack[request.SenderId] });
            _ack[request.SenderId] = request.Seq;
        }

        var reply = new TwoPCCommitReply { Ack = request.Seq };

        if (CheckLeadership(request.Command.Slot, request.SenderId))
            TwoPC.AddCommitted(
                request.Command.GlobalSeqNumber,
                new ClientCommand(request.Command.Slot,
                    request.Command.ClientId,
                    request.Command.ClientSeqNumber,
                    request.Command.Type,
                    request.Command.Amount));

        return Task.FromResult(reply);
    }
}