using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BankServerService : ProjectBankServerService.ProjectBankServerServiceBase
{
    private readonly BankAccount _account = new();
    private readonly PerfectChannelBankServer _channel = new();
    private readonly int _id;
    private readonly ConcurrentDictionary<int, int> _isPrimary; //  primary/backup

    public BankServerService(int id, ConcurrentDictionary<int, int> isPrimary)
    {
        _id = id;
        _isPrimary = isPrimary;
    }

    public int CurrentSlot { get; set; } = 1;

    public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
    {
        ReadBalanceReply reply = new();

        lock (_account)
        {
            reply.Balance = _account.Balance;
        }

        return Task.FromResult(_channel.SetAck(request, reply));
    }

    public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
    {
        lock (_account)
        {
            _account.Deposit(request.Amount);
        }

        DepositReply reply = new();
        return Task.FromResult(_channel.SetAck(request, reply));
    }

    public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
    {
        WithdrawReply reply = new();

        lock (_account)
        {
            reply.Status = _account.Withdraw(request.Amount);
        }

        return Task.FromResult(_channel.SetAck(request, reply));
    }

    public override Task<CompareSwapReply> AcceptCompareSwapResult(CompareSwapResult request, ServerCallContext context)
    {
        Console.WriteLine("Received result for slot {0}: {1}", request.Slot, request.Value);
        _isPrimary[request.Slot] = request.Value;
        //Do Clean Up if leader changed
        if (_isPrimary[request.Slot] == _id && _isPrimary[request.Slot] != _isPrimary[request.Slot - 1]) CleanUp2PC();
        return Task.FromResult(_channel.SetAck(request, new CompareSwapReply())));
    }
}