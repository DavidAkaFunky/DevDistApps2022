﻿using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    private readonly int id;
    private int seq;
    private static readonly int TIMEOUT = 1000;
    private readonly string serverAddress;
    private readonly Sender sender;

    public BankToBankFrontend(int id, string serverAddress)
    {
        this.id = id;
        this.serverAddress = serverAddress;
        seq = 0;
        sender = new(GrpcChannel.ForAddress(serverAddress), TIMEOUT, id);
    }

    public int Id
    {
        get { return id; }
    }

    public string ServerAddress
    {
        get { return serverAddress; }
    }


    public ListPendingRequestsReply ListPendingTwoPCRequests(int lastKnownSequenceNumber)
    {
        var request = new ListPendingRequestsRequest
        {
            SenderId = id,
            GlobalSeqNumber = lastKnownSequenceNumber
        };
        return sender.Send(request).Result;
    }

    public TwoPCTentativeReply SendTwoPCTentative(ClientCommand command, int tentativeSeqNumber)
    {
        var request = new TwoPCTentativeRequest
        {
            SenderId = id,
            Command = command.CreateCommandGRPC(tentativeSeqNumber)
        };
        return sender.Send(request).Result;
    }

    public void SendTwoPCCommit(ClientCommand command, int committedSeqNumber)
    {
        var request = new TwoPCCommitRequest
        {
            SenderId = id,
            Command = command.CreateCommandGRPC(committedSeqNumber)
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

        public Task<ListPendingRequestsReply> Send(ListPendingRequestsRequest req)
        {
            var t = new Task<ListPendingRequestsReply>(() =>
            {
                var stub = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(_channel);
                ListPendingRequestsReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
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