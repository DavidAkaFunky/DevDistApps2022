using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankTwoPCService : ProjectBankTwoPCService.ProjectBankTwoPCServiceBase
{
    private int id;
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup
    private ConcurrentDictionary<int, ClientCommand> tentativeCommands;
    private ConcurrentDictionary<int, ClientCommand> committedCommands;
    private int currentSlot = 1;

    public BankTwoPCService(int id, ConcurrentDictionary<int, int> isPrimary, ConcurrentDictionary<int, ClientCommand> tentativeCommands, ConcurrentDictionary<int, ClientCommand> committedCommands)
    {
        this.id = id;
        this.isPrimary = isPrimary;
        this.tentativeCommands = tentativeCommands;
        this.committedCommands = committedCommands;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    // All tentatives/commits coming from anyone other than the leader will be ignored!
    private bool CheckLeadership(int slot, int senderID) {

        if (slot > currentSlot) // Dumb check (it should not happen!)
            return false;

        // Checking if the leader has always been senderID since the given slot
        for (int i = slot; i <= currentSlot; ++i)
            if (isPrimary[i] != senderID)
                return false;

        return true;
    }

    public override Task<ListPendingRequestsReply> ListPendingRequests(ListPendingRequestsRequest request, ServerCallContext context)
    {
        var reply = new ListPendingRequestsReply();

        foreach(var kvp in tentativeCommands)
        {
            if (kvp.Key > request.GlobalSeqNumber)
            {
                tentativeCommands.TryRemove(kvp.Key, out var value);
                reply.Commands.Add(value.CreateCommandGRPC(kvp.Key));
            }
        }

        return Task.FromResult(reply);
    }

    public override Task<TwoPCTentativeReply> TwoPCTentative(TwoPCTentativeRequest request, ServerCallContext context)
    {
        // Should we assume the commands will never be in the dictionary more than once?
        // I.e., since the leader coordinates the messages, even the receiver had it once,
        // cleanup will remove it, so the new version can arrive without repetition

        var reply = new TwoPCTentativeReply() { Status = false };

        if (CheckLeadership(request.Slot, request.SenderId))
        {
            reply.Status = true;
            
            lock(tentativeCommands)
                tentativeCommands[request.Command.GlobalSeqNumber] = new(request.Command.ClientId, request.Command.ClientSeqNumber, request.Command.Message);
        }

        return Task.FromResult(reply);
    }

    public override Task<TwoPCCommitReply> TwoPCCommit(TwoPCCommitRequest request, ServerCallContext context)
    {
        var reply = new TwoPCCommitReply() { Status = false };

        if (CheckLeadership(request.Slot, request.SenderId))
        {
            reply.Status = true;

            lock (tentativeCommands) lock (committedCommands)
            {
                // Remove message from tentative commands
                var item = tentativeCommands.First(kvp => kvp.Value.ClientID == request.Command.ClientId && kvp.Value.ClientSeqNumber == request.Command.ClientSeqNumber);
                tentativeCommands.TryRemove(item.Key, out var _);

                committedCommands[request.Command.GlobalSeqNumber] = new(request.Command.ClientId, request.Command.ClientSeqNumber, request.Command.Message);
            }
        }

        return Task.FromResult(reply);
    }

}