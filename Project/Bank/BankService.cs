using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankService : ProjectBankService.ProjectBankServiceBase
{
    private int id;
    private readonly BankAccount account = new();
    private readonly List<BankToBoneyFrontend> bankToBoneyFrontends = new();
    private bool primary = false; //  primary/backup
    private int currentSlot = 1 ;

    public BankService(int id, List<BankToBoneyFrontend> bankToBoneyFrontends) 
    {
        this.id = id;
        this.bankToBoneyFrontends = bankToBoneyFrontends;
    }

    public bool Primary
    {
        get { return primary; }
        set { primary = value; }
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
        Console.WriteLine("GOT RESPONSE FOR SLOT " + request.Slot + ": " + request.Value);
        if (request.Slot == currentSlot && request.Value == id)
            primary = true;
            // TODO: Eventually run 2PC at the beginning of slot?

        return Task.FromResult(new CompareSwapReply());
    }
}