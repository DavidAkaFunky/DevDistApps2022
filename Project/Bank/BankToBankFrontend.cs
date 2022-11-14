using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    // the timeout for waiting for a response
    private static readonly int TIMEOUT = 1000;

    //the bank's id

    // the instance of the perfect channel sender
    private readonly Sender sender;

    // the server's ip address / hostname and port

    // old code, unused
    private int seq;

    public BankToBankFrontend(int id, string serverAddress)
    {
        Id = id;
        ServerAddress = serverAddress;
        seq = 0;
        sender = new Sender(GrpcChannel.ForAddress(serverAddress), TIMEOUT, id);
    }

    public int Id { get; }

    public string ServerAddress { get; }


    public ListPendingRequestsReply ListPendingTwoPCRequests(int lastKnownSequenceNumber)
    {
        var request = new ListPendingRequestsRequest
        {
            SenderId = Id,
            GlobalSeqNumber = lastKnownSequenceNumber
        };
        return sender.Send(request).Result;
    }

    public TwoPCTentativeReply SendTwoPCTentative(ClientCommand command, int tentativeSeqNumber)
    {
        var request = new TwoPCTentativeRequest
        {
            SenderId = Id,
            Command = command.CreateCommandGRPC(tentativeSeqNumber)
        };
        return sender.Send(request).Result;
    }

    public void SendTwoPCCommit(ClientCommand command, int committedSeqNumber)
    {
        var request = new TwoPCCommitRequest
        {
            SenderId = Id,
            Command = command.CreateCommandGRPC(committedSeqNumber)
        };
        sender.Send(request);
    }

    private class Sender
    {
        //the grpc channel to communicate with the server
        private readonly GrpcChannel _channel;

        // used for timeouts
        private readonly Random _random = new();

        // this client's id
        private readonly int _senderID;

        // used for incrementing the seq
        private readonly Mutex _seqLock = new();

        private readonly int _timeout;

        // the current seq
        private int _currentSeq = 1;

        public Sender(GrpcChannel channel, int timeout, int senderID)
        {
            _channel = channel;
            _timeout = timeout;
            _senderID = senderID;
        }

        public Task<ListPendingRequestsReply> Send(ListPendingRequestsRequest req)
        {
            var t = new Task<ListPendingRequestsReply>(() =>
            {
                // create a stub to send the rpc
                var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(_channel);
                ListPendingRequestsReply? reply = null;
                // get a seq number for the message
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }

                // stamp our id
                req.SenderId = _senderID;
                // will keep trying until we receive a response and it matches the expected ack (seq + 1)
                while (true)
                {
                    try
                    {
                        reply = stub.ListPendingRequests(req);
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
            // async so we don't have to wait indefinitely before sending more messages
            t.Start();
            return t;
        }

        public Task<TwoPCTentativeReply> Send(TwoPCTentativeRequest req)
        {
            var t = new Task<TwoPCTentativeReply>(() =>
            {
                var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(_channel);
                TwoPCTentativeReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }

                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.TwoPCTentative(req);
                    }
                    catch (RpcException)
                    {
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

        public Task<TwoPCCommitReply> Send(TwoPCCommitRequest req)
        {
            var t = new Task<TwoPCCommitReply>(() =>
            {
                var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(_channel);
                TwoPCCommitReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }

                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.TwoPCCommit(req);
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