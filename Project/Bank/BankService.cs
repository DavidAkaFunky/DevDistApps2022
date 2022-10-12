using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankService : ProjectBankService.ProjectBankServiceBase
{
    private int id;
    private readonly BankAccount account = new();
    private ConcurrentDictionary<int, bool> isPrimary; //  primary/backup
    private int currentSlot = 1;

    public BankService(int id, ConcurrentDictionary<int, bool> isPrimary) 
    {
        this.id = id;
        this.isPrimary = isPrimary;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        ReadBalanceReply reply = new() { Balance = account.Balance };
        return Task.FromResult(reply);
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        lock (account)
        {
            account.Deposit(request.Amount);
            DepositReply reply = new();
            return Task.FromResult(reply);
        }
    }

    public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
    {
        lock (account)
        {
            WithdrawReply reply = new() { Status = account.Withdraw(request.Amount) };
            return Task.FromResult(reply);
        }
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        Console.WriteLine("GOT COMPARE AND SWAP VALUE FOR SLOT " + request.Slot + " AND THE VALUE IS " + request.Value);
        if (request.Value == id)
        {
            Console.WriteLine("I AM THE LEADER FOR THE SLOT " + request.Slot);
            isPrimary[request.Slot] = true;
            // TODO: Eventually run 2PC at the beginning of slot?
        }

        return Task.FromResult(new CompareSwapReply());
    }
}