using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyToBankFrontend
{
    private static readonly int TIMEOUT = 1000;
    private readonly Sender sender;

    public BoneyToBankFrontend(int clientID, string serverAddress)
    {
        sender = new(GrpcChannel.ForAddress(serverAddress), TIMEOUT, clientID);
    }

    public void SendCompareSwapResult(int slot, int value)
    {
        CompareSwapResult request = new()
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

        public Task<CompareSwapReply> Send(CompareSwapResult req)
        {
            var t = new Task<CompareSwapReply>(() =>
            {
                var stub = new ProjectBankServerService.ProjectBankServerServiceClient(_channel);
                CompareSwapReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.AcceptCompareSwapResult(req);
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