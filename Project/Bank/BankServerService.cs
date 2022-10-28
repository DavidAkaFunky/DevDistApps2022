﻿using System.Collections.Concurrent;
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

    //TODO change this to a dictionary where <clientId>, <lastAck> pairs are saved
    //TODO this implies changing proto messages to include clientIds in all the requests
    private int _ack;

    public BankServerService(int id, ConcurrentDictionary<int, int> primary, TwoPhaseCommit TwoPC)
    {
        this.id = id;
        this.primary = primary;
        this.TwoPC = TwoPC;
    }

    public int CurrentSlot { get; set; } = 1;

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (request.Seq != _ack + 1)
                return Task.FromResult(new ReadBalanceReply { Ack = _ack });
            _ack = request.Seq;
        }


        ReadBalanceReply reply = new() { Balance = -1 };

        //=============Check If Leader===================

        //temporary 
        if (id != 4) return Task.FromResult(reply);

        //if (primary[currentSlot] != id) return Task.FromResult(reply);

        //=====================2PC=======================

        TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "R", 0));

        //================Execute and Reply==============

        reply.Balance = account.Balance;

        return Task.FromResult(reply);
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (request.Seq != _ack + 1)
                return Task.FromResult(new DepositReply { Ack = _ack });
            _ack = request.Seq;
        }

        DepositReply reply = new() { Status = false };

        //=============Check If Leader===================

        //temporary 
        if (id != 4) return Task.FromResult(reply);

        //if (primary[currentSlot] != id) return Task.FromResult(reply);

        //=====================2PC=======================
        try
        {
            TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "D", request.Amount));
        }
        catch (Exception e) // TODO: REMOVE THIS!!!
        {
            Console.WriteLine(e);
            Thread.Sleep(100000);
        }
        //================Execute and Reply==============

        account.Deposit(request.Amount);

        reply.Status = true;

        return Task.FromResult(reply);
    }

    public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (request.Seq != _ack + 1)
                return Task.FromResult(new WithdrawReply { Ack = _ack });
            _ack = request.Seq;
        }

        WithdrawReply reply = new() { Status = -1 };

        //=============Check If Leader===================

        //temporary 
        if (id != 4) return Task.FromResult(reply);

        //if (primary[currentSlot] != id) return Task.FromResult(reply);

        //=====================2PC=======================

        TwoPC.Run(new ClientCommand(CurrentSlot, request.SenderId, request.Seq, "W", request.Amount));

        //================Execute and Reply==============

        reply.Status = account.Withdraw(request.Amount) ? 1 : 0;

        return Task.FromResult(reply);
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        //Console.WriteLine("Received result for slot {0}: {1}", request.Slot, request.Value);

        primary[request.Slot] = request.Value;

        return Task.FromResult(new CompareSwapReply());
    }
}