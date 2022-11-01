using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
internal class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    private readonly object _ackLock = new();
    private readonly BankAccount account = new();

    private readonly int id;
    private readonly ConcurrentDictionary<int, int> primary; //  primary/backup

    private readonly TwoPhaseCommit TwoPC;

    private readonly Dictionary<int, bool> isFrozen;

    //TODO change this to a dictionary where <clientId>, <lastAck> pairs are saved
    //TODO this implies changing proto messages to include clientIds in all the requests
    private Dictionary<int, int[]> _ack = new();

    public BankServerService(int id, ConcurrentDictionary<int, int> primary, TwoPhaseCommit TwoPC, Dictionary<int, bool> isFrozen)
    {
        this.id = id;
        this.primary = primary;
        this.TwoPC = TwoPC;
        this.isFrozen = isFrozen;
    }

    public int CurrentSlot { get; set; } = 1;

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            if (request.Seq != _ack[request.SenderId][0] + 1)
                return Task.FromResult(new ReadBalanceReply { Ack = _ack[request.SenderId][0] });
            _ack[request.SenderId][0] = request.Seq;
        }

        var reply = new ReadBalanceReply { Balance = -1, Ack = request.Seq };

        //=============Check If Leader===================

        if (primary[CurrentSlot] != id || isFrozen[CurrentSlot]) return Task.FromResult(reply);

        //=====================2PC=======================

        TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "R", 0));

        //================Execute and Reply==============

        reply.Balance = account.Balance;
        Console.WriteLine(account.Balance);
        return Task.FromResult(reply);
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            if (request.Seq != _ack[request.SenderId][0] + 1)
                return Task.FromResult(new DepositReply { Ack = _ack[request.SenderId][0] });
            _ack[request.SenderId][0] = request.Seq;
        }

        var reply = new DepositReply { Status = false, Ack = request.Seq };

        //=============Check If Leader===================

        if (primary[CurrentSlot] != id || isFrozen[CurrentSlot]) return Task.FromResult(reply);

        //=====================2PC=======================
        
        TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "D", request.Amount));
        
        //================Execute and Reply==============

        Console.WriteLine("DEPOSIT " + request.Amount + account.Balance);
        account.Deposit(request.Amount);
        Console.WriteLine(account.Balance);

        reply.Status = true;

        return Task.FromResult(reply);
    }

    public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            if (request.Seq != _ack[request.SenderId][0] + 1)
                return Task.FromResult(new WithdrawReply { Ack = _ack[request.SenderId][0] });
            _ack[request.SenderId][0] = request.Seq;
        }

        var reply = new WithdrawReply { Status = -1, Ack = request.Seq };

        //=============Check If Leader===================

        if (primary[CurrentSlot] != id || isFrozen[CurrentSlot]) return Task.FromResult(reply);

        //=====================2PC=======================

        TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "W", request.Amount));

        //================Execute and Reply==============
        Console.WriteLine("WITHDRAW " + account.Balance);

        reply.Status = account.Withdraw(request.Amount) ? 1 : 0;
        Console.WriteLine(account.Balance);

        return Task.FromResult(reply);
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            if (request.Seq != _ack[request.SenderId][1] + 1)
                return Task.FromResult(new CompareSwapReply { Ack = _ack[request.SenderId][1] });
            _ack[request.SenderId][1] = request.Seq;
        }

        Console.WriteLine("Received result for slot {0}: {1}", request.Slot, request.Value);
        
        if (!isFrozen[CurrentSlot])
            primary[request.Slot] = request.Value;

        return Task.FromResult(new CompareSwapReply { Ack = request.Seq });
    }
}