using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyToBoneyFrontend
{
    private static readonly int TIMEOUT = 1000;
    private readonly Sender sender;

    public BoneyToBoneyFrontend(int clientID, string serverAddress)
    {
        ServerAddress = serverAddress;
        ClientId = clientID;
        sender = new(GrpcChannel.ForAddress(serverAddress), TIMEOUT, clientID);
    }

    public int ClientId { get; }

    public string ServerAddress { get; set; }

    // TODO add metadata

    // Proposer
    public PromiseReply Prepare(int slot, int id)
    {
        var request = new PrepareRequest
        {
            Slot = slot,
            TimestampId = id
        };
        return sender.Send(request).Result;
    }

    public bool Accept(int slot, int id, int value)
    {
        var request = new AcceptRequest
        {
            TimestampId = id,
            Slot = slot,
            Value = value
        };
        return sender.Send(request).Result.Status;
    }

    // Acceptor 
    public void AcceptedToLearner(int slot, int id, int value)
    {
        var request = new AcceptedToLearnerRequest
        {
            Slot = slot,
            TimestampId = id,
            Value = value
        };
        sender.Send(request);
    }

    // Learner
    public void ResultToProposer(int slot, int value)
    {
        var request = new ResultToProposerRequest
        {
            Slot = slot,
            Value = value
        };
        sender.Send(request);
    }

    private class Sender
    {
        private readonly GrpcChannel _channel;
        private readonly Random _random = new();
        private readonly Mutex _seqLock = new();
        private readonly int _timeout;
        private int _currentSeq = 1;
        private int _senderID;

        public Sender(GrpcChannel channel, int timeout, int senderID)
        {
            _channel = channel;
            _timeout = timeout;
            _senderID = senderID;
        }

        public Task<PromiseReply> Send(PrepareRequest req)
        {
            var t = new Task<PromiseReply>(() =>
            {
                var stub = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(_channel);
                PromiseReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.Prepare(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
                        break;
                }

                return reply;
            });
            t.Start();
            return t;
        }

        public Task<AcceptReply> Send(AcceptRequest req)
        {
            var t = new Task<AcceptReply>(() =>
            {
                var stub = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(_channel);
                AcceptReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.Accept(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
                        break;
                }

                return reply;
            });
            t.Start();
            return t;
        }

        public Task<AcceptedToLearnerReply> Send(AcceptedToLearnerRequest req)
        {
            var t = new Task<AcceptedToLearnerReply>(() =>
            {
                var stub = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(_channel);
                AcceptedToLearnerReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.AcceptedToLearner(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
                        break;
                }

                return reply;
            });
            t.Start();
            return t;
        }

        public Task<ResultToProposerReply> Send(ResultToProposerRequest req)
        {
            var t = new Task<ResultToProposerReply>(() =>
            {
                var stub = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(_channel);
                ResultToProposerReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.ResultToProposer(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
                        break;
                }

                return reply;
            });
            t.Start();
            return t;
        }
    }
}