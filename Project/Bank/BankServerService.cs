using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    private int id;
    private readonly BankAccount account = new();
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup
    private int currentSlot = 1;

    public BankServerService(int id, ConcurrentDictionary<int, int> isPrimary) 
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
        Console.WriteLine("Received result for slot {0}: {1}", request.Slot, request.Value);
        isPrimary[request.Slot] = request.Value;

        return Task.FromResult(new CompareSwapReply());
    }
}