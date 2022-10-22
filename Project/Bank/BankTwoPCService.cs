using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankTwoPCService : ProjectBankTwoPCService.ProjectBankTwoPCServiceBase
{
    private int id;
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup
    private ConcurrentDictionary<ClientInfo, int> tentativeCommands = new();
    private ConcurrentDictionary<int, ClientCommand> committedCommands = new();
    private int currentSlot = 1;

    internal struct ClientInfo
    {
        private int clientID;
        private int clientSeqNumber;

        internal ClientInfo(int clientID, int clientSeqNumber)
        {
            this.clientID = clientID;
            this.clientSeqNumber = clientSeqNumber;
        }
    }

    public BankTwoPCService(int id, ConcurrentDictionary<int, int> isPrimary)
    {
        this.id = id;
        this.isPrimary = isPrimary;
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

    public override Task<TwoPCTentativeReply> TwoPCTentative(TwoPCTentativeRequest request, ServerCallContext context)
    {
        // Should we assume the commands will never be in the dictionary more than once?
        // I.e., since the leader coordinates the messages, even the receiver had it once,
        // cleanup will remove it, so the new version can arrive without repetition

        var reply = new TwoPCTentativeReply() { Status = false };

        if (CheckLeadership(request.Slot, request.SenderId))
        {
            reply.Status = true;
            tentativeCommands[new(request.ClientId, request.ClientSeqNumber)] = request.GlobalSeqNumber;
        }

        return Task.FromResult(reply);
    }

    public override Task<TwoPCCommitReply> TwoPCCommit(TwoPCCommitRequest request, ServerCallContext context)
    {
        var reply = new TwoPCCommitReply() { Status = false };

        if (CheckLeadership(request.Slot, request.SenderId))
        {
            reply.Status = true;
            tentativeCommands.TryRemove(new(request.ClientId, request.ClientSeqNumber), out _);
            committedCommands[request.GlobalSeqNumber] = new(request.ClientId, request.ClientSeqNumber, request.Message);
        }

        return Task.FromResult(reply);
    }

}