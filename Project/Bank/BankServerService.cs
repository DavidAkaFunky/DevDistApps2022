﻿using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
internal class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    // <clientId, lastAckSent[]> (index[1] is used for Boney -> Bank communication)
    // [0] used for Client -> Bank Communication
    private readonly Dictionary<int, int[]> _ack = new();

    //object used only for incrementing the ack inside a lock
    private readonly object _ackLock = new();
    private readonly BankAccount account;
    private readonly int id;

    // <slot, bool> 
    private readonly Dictionary<int, bool> isFrozen;

    // <slot, primaryId>
    private readonly ConcurrentDictionary<int, int> primary; //  primary/backup

    private readonly TwoPhaseCommit TwoPC;

    public BankServerService(int id, ConcurrentDictionary<int, int> primary, TwoPhaseCommit TwoPC,
        Dictionary<int, bool> isFrozen, BankAccount account)
    {
        this.id = id;
        this.primary = primary;
        this.TwoPC = TwoPC;
        this.isFrozen = isFrozen;
        this.account = account;
    }

    public int CurrentSlot { get; set; } = 1;

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            // if first message received from channel _ack[<clientId>] = 0
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            Console.WriteLine("READ BALANCE REQUEST " + request.Seq + " ACK = " + _ack[request.SenderId][0]);
            // if seq is not the expected number (ack + 1), refuse the message and reply with current ack for this channel
            if (request.Seq != _ack[request.SenderId][0] + 1)
                return Task.FromResult(new ReadBalanceReply { Ack = _ack[request.SenderId][0] });
            // update ack
            _ack[request.SenderId][0] = request.Seq;
        }

        var reply = new ReadBalanceReply { Balance = -1, Ack = request.Seq };

        //=============Check If Leader===================
        // if i am frozen, reply with Balance = -1 (unavailable)
        if (isFrozen[CurrentSlot]) return Task.FromResult(reply);

        // wait while the there is no primary server for this slot
        while (primary[CurrentSlot] == -1) ;

        // if i am not the primary, return
        if (primary[CurrentSlot] != id) return Task.FromResult(reply);

        //================Execute and Reply==============

        lock (account)
        {
            reply.Balance = account.Balance;
            Console.WriteLine($"Account Balance after: {reply.Balance}");
        }

        return Task.FromResult(reply);
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = new int[2] { 0, 0 };
            Console.WriteLine("DEPOSIT " + request.Amount + " REQUEST SEQ = " + request.Seq + " ACK = " +
                              _ack[request.SenderId][0]);
            if (request.Seq != _ack[request.SenderId][0] + 1)
                return Task.FromResult(new DepositReply { Ack = _ack[request.SenderId][0] });
            _ack[request.SenderId][0] = request.Seq;
        }

        var reply = new DepositReply { Status = false, Ack = request.Seq };

        //=============Check If Leader===================

        if (isFrozen[CurrentSlot]) return Task.FromResult(reply);

        Console.WriteLine("NOT FROZEN");

        while (primary[CurrentSlot] == -1) ;

        Console.WriteLine("LEADER IS: " + primary[CurrentSlot] + " I AM " + id);
        if (primary[CurrentSlot] != id) return Task.FromResult(reply);

        //=====================2PC=======================

        reply.Status = TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "D", request.Amount)) ==
                       1;

        Console.WriteLine("RESULT: " + reply.Status);
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

        if (isFrozen[CurrentSlot]) return Task.FromResult(reply);

        while (primary[CurrentSlot] == -1) ;

        if (primary[CurrentSlot] != id) return Task.FromResult(reply);

        //=====================2PC=======================

        reply.Status = TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "W", request.Amount));

        return Task.FromResult(reply);
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        Console.WriteLine("HELLO");
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