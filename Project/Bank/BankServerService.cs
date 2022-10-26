using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
internal class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    private int id;
    private readonly BankAccount account = new();
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup
    private int currentSlot = 1;
    private Bank bank;

    public BankServerService(int id, ConcurrentDictionary<int, int> isPrimary, Bank bank) 
    {
        this.id = id;
        this.isPrimary = isPrimary;
        this.bank = bank;
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
        //Do Clean Up if leader changed
        if (isPrimary[request.Slot] == id && isPrimary[request.Slot] != isPrimary[request.Slot - 1])
        {
            bank.CleanUpTwoPC(request.Slot);
        }
        return Task.FromResult(new CompareSwapReply());
    }
}