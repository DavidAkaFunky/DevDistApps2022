namespace DADProject;

/*
	rpc ReadBalance(ReadBalanceRequest) returns (ReadBalanceReply);
	rpc Deposit(DepositRequest) returns (DepositReply);
	rpc Withdraw(WithdrawRequest) returns (WithdrawReply);
	rpc AcceptCompareSwapResult (CompareSwapResult) returns (CompareSwapReply);
	
	rpc ListPendingRequests(ListPendingRequestsRequest) returns (ListPendingRequestsReply);
	rpc TwoPCTentative(TwoPCTentativeRequest) returns (TwoPCTentativeReply);
	rpc TwoPCCommit(TwoPCCommitRequest) returns (TwoPCCommitReply);
 */

public class PerfectChannelBankServer
{
    //TODO we can optimize this by saving the messages with larger seq. numbers than expected
    private readonly Dictionary<int, int> _acks = new();

    private int GetAckForClient(int clientId, int seq)
    {
        var lastAck = _acks.GetValueOrDefault(clientId, 0);
        if (seq == lastAck + 1)
            _acks[clientId] = seq;
        return _acks[clientId];
    }

    public ReadBalanceReply SetAck(ReadBalanceRequest request, ReadBalanceReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public DepositReply SetAck(DepositRequest request, DepositReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public WithdrawReply SetAck(WithdrawRequest request, WithdrawReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public CompareSwapReply SetAck(CompareSwapResult request, CompareSwapReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public ListPendingRequestsReply SetAck(ListPendingRequestsRequest request, ListPendingRequestsReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public TwoPCCommitReply SetAck(TwoPCCommitRequest request, TwoPCCommitReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }

    public TwoPCTentativeReply SetAck(TwoPCTentativeRequest request, TwoPCTentativeReply reply)
    {
        reply.Ack = GetAckForClient(request.SenderId, request.Seq);
        reply.Seq = 0;
        reply.SenderId = 0;
        return reply;
    }
}