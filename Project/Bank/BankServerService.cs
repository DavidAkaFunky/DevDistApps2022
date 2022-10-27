using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
internal class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    private int id;
    private readonly BankAccount account = new();

    private Queue<ClientCommand> receivedCommands;

    private int currentSlot = 1;
    private ConcurrentDictionary<int, int> primary; //  primary/backup

    public BankServerService(int id, ConcurrentDictionary<int, int> primary, Queue<ClientCommand> received) 
    {
        this.id = id;
        this.primary = primary;
        this.receivedCommands = received;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        ReadBalanceReply reply = new() { };

        //=============Check If Leader===================
        
        //temporary 
        if (id != 4) return Task.FromResult(reply);

        //=====================ADD To Queue==============
        Monitor.Enter(receivedCommands);

        //The command might not be right
        receivedCommands.Enqueue(new(0, request.SenderId, request.Seq, "Read"));

        Monitor.PulseAll(receivedCommands);

        Monitor.Exit(receivedCommands);
        
        //================Execute and Reply==============

        //reply.Balance = account.Balance;
        
        return Task.FromResult(reply);
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        DepositReply reply = new();

        //=============Check If Leader===================



        //=====================ADD To Queue==============
        Monitor.Enter(receivedCommands);

        //The command might not be right
        receivedCommands.Enqueue(new(0, request.SenderId, request.Seq, "Read"));

        Monitor.PulseAll(receivedCommands);

        Monitor.Exit(receivedCommands);

        //================Execute and Reply==============

        //account.Deposit(request.Amount);

        return Task.FromResult(reply);
    }

    public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
    {
        WithdrawReply reply = new();

        //=============Check If Leader===================



        //=====================ADD To Queue==============
        Monitor.Enter(receivedCommands);

        //The command might not be right
        receivedCommands.Enqueue(new(0, request.SenderId, request.Seq, "Read"));

        Monitor.PulseAll(receivedCommands);

        Monitor.Exit(receivedCommands);

        //================Execute and Reply==============

        //reply.Status = account.Withdraw(request.Amount);

        return Task.FromResult(reply);
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        //Console.WriteLine("Received result for slot {0}: {1}", request.Slot, request.Value);

        primary[request.Slot] = request.Value;

        return Task.FromResult(new CompareSwapReply());
    }
}