using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject.PerfectChannel;

public abstract class Message
{
    public abstract int Ack { get; set; }
    public abstract int Seq { get; set; }

    public abstract Message? Send(GrpcChannel channel, int timeout);
}

public class CompareAndSwapRequest : Message
{
    public CompareAndSwapRequest(DADProject.CompareAndSwapRequest o)
    {
        Original = o;
    }


    public DADProject.CompareAndSwapRequest Original { get; }


    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        try
        {
            var reply = stub.CompareAndSwap(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new CompareAndSwapReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class TwoPCTentativeRequest : Message
{
    public TwoPCTentativeRequest(DADProject.TwoPCTentativeRequest o)
    {
        Original = o;
    }


    public DADProject.TwoPCTentativeRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(channel);
        try
        {
            var reply = stub.TwoPCTentative(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new TwoPCTentativeReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class AcceptRequest : Message
{
    public AcceptRequest(DADProject.AcceptRequest o)
    {
        Original = o;
    }


    public DADProject.AcceptRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        try
        {
            var reply = stub.Accept(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new AcceptReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class CompareSwapResult : Message
{
    public CompareSwapResult(DADProject.CompareSwapResult o)
    {
        Original = o;
    }


    public DADProject.CompareSwapResult Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
        try
        {
            var reply = stub.AcceptCompareSwapResult(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new CompareSwapReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class ListPendingRequestsRequest : Message
{
    public ListPendingRequestsRequest(DADProject.ListPendingRequestsRequest o)
    {
        Original = o;
    }


    public DADProject.ListPendingRequestsRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(channel);
        try
        {
            var reply = stub.ListPendingRequests(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new ListPendingRequestsReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class PrepareRequest : Message
{
    public PrepareRequest(DADProject.PrepareRequest o)
    {
        Original = o;
    }


    public DADProject.PrepareRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        try
        {
            var reply = stub.Prepare(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new PromiseReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class ResultToProposerReply : Message
{
    public ResultToProposerReply(DADProject.ResultToProposerReply o)
    {
        Original = o;
    }


    public DADProject.ResultToProposerReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class TwoPCCommitRequest : Message
{
    public TwoPCCommitRequest(DADProject.TwoPCCommitRequest o)
    {
        Original = o;
    }


    public DADProject.TwoPCCommitRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(channel);
        try
        {
            var reply = stub.TwoPCCommit(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new TwoPCCommitReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class CompareSwapReply : Message
{
    public CompareSwapReply(DADProject.CompareSwapReply o)
    {
        Original = o;
    }


    public DADProject.CompareSwapReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class AcceptReply : Message
{
    public AcceptReply(DADProject.AcceptReply o)
    {
        Original = o;
    }


    public DADProject.AcceptReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class PromiseReply : Message
{
    public PromiseReply(DADProject.PromiseReply o)
    {
        Original = o;
    }


    public DADProject.PromiseReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class TwoPCCommitReply : Message
{
    public TwoPCCommitReply(DADProject.TwoPCCommitReply o)
    {
        Original = o;
    }


    public DADProject.TwoPCCommitReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class ListPendingRequestsReply : Message
{
    public ListPendingRequestsReply(DADProject.ListPendingRequestsReply o)
    {
        Original = o;
    }


    public DADProject.ListPendingRequestsReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class WithdrawReply : Message
{
    public WithdrawReply(DADProject.WithdrawReply o)
    {
        Original = o;
    }


    public DADProject.WithdrawReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class ResultToProposerRequest : Message
{
    public ResultToProposerRequest(DADProject.ResultToProposerRequest o)
    {
        Original = o;
    }


    public DADProject.ResultToProposerRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        try
        {
            var reply = stub.ResultToProposer(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new ResultToProposerReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class ReadBalanceReply : Message
{
    public ReadBalanceReply(DADProject.ReadBalanceReply o)
    {
        Original = o;
    }


    public DADProject.ReadBalanceReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class DepositRequest : Message
{
    public DepositRequest(DADProject.DepositRequest o)
    {
        Original = o;
    }

    public DADProject.DepositRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
        try
        {
            var reply = stub.Deposit(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new DepositReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class DepositReply : Message
{
    public DepositReply(DADProject.DepositReply o)
    {
        Original = o;
    }


    public DADProject.DepositReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class CompareAndSwapReply : Message
{
    public CompareAndSwapReply(DADProject.CompareAndSwapReply o)
    {
        Original = o;
    }


    public DADProject.CompareAndSwapReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class AcceptedToLearnerReply : Message
{
    public AcceptedToLearnerReply(DADProject.AcceptedToLearnerReply o)
    {
        Original = o;
    }


    public DADProject.AcceptedToLearnerReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}

public class ReadBalanceRequest : Message
{
    public ReadBalanceRequest(DADProject.ReadBalanceRequest o)
    {
        Original = o;
    }


    public DADProject.ReadBalanceRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
        try
        {
            var reply = stub.ReadBalance(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new ReadBalanceReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class WithdrawRequest : Message
{
    public WithdrawRequest(DADProject.WithdrawRequest o)
    {
        Original = o;
    }


    public DADProject.WithdrawRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
        try
        {
            var reply = stub.Withdraw(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new WithdrawReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class AcceptedToLearnerRequest : Message
{
    public AcceptedToLearnerRequest(DADProject.AcceptedToLearnerRequest o)
    {
        Original = o;
    }


    public DADProject.AcceptedToLearnerRequest Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        var stub = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(channel);
        try
        {
            var reply = stub.AcceptedToLearner(Original, null, DateTime.Now.AddMilliseconds(timeout));
            return new AcceptedToLearnerReply(reply);
        }
        catch (RpcException) // deadline exceeded
        {
            return null;
        }
    }
}

public class TwoPCTentativeReply : Message
{
    public TwoPCTentativeReply(DADProject.TwoPCTentativeReply o)
    {
        Original = o;
    }


    public DADProject.TwoPCTentativeReply Original { get; }

    public override int Ack
    {
        get => Original.Ack;
        set => Original.Ack = value;
    }

    public override int Seq
    {
        get => Original.Seq;
        set => Original.Seq = value;
    }

    public override Message? Send(GrpcChannel channel, int timeout)
    {
        throw new NotImplementedException();
    }
}