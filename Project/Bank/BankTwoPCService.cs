using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankTwoPCService : ProjectBankTwoPCService.ProjectBankTwoPCServiceBase
{
    private int id;
    private ConcurrentDictionary<int, bool> isPrimary; //  primary/backup
    private int currentSlot = 1;

    public BankTwoPCService(int id, ConcurrentDictionary<int, bool> isPrimary)
    {
        this.id = id;
        this.isPrimary = isPrimary;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    public override Task<TwoPCTentativeReply> TwoPCTentative(TwoPCTentativeRequest request, ServerCallContext context)
    {
        // TODO
    }

    public override Task<TwoPCCommitReply> TwoPCCommit(TwoPCCommitRequest request, ServerCallContext context)
    {
        // TODO
    }

}