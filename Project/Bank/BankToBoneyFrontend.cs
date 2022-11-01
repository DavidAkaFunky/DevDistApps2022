using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace DADProject;

public class BankToBoneyFrontend
{
    private static readonly int TIMEOUT = 1000;
    private readonly Sender sender;
    private readonly int id;
    private ConcurrentDictionary<int, int> isPrimary; //  primary/backup

    public BankToBoneyFrontend(int id, string serverAddress, ConcurrentDictionary<int, int> isPrimary)
    {
        this.id = id;
        sender = new(GrpcChannel.ForAddress(serverAddress), TIMEOUT, id);
        this.isPrimary = isPrimary;
    }

    public void RequestCompareAndSwap(int slot)
    {
        //Console.WriteLine("SENDING CS FOR SLOT " + slot);
        
        CompareAndSwapRequest request = new() { Slot = slot, InValue = id };
        sender.Send(request).ContinueWith(task =>
        {
            var reply = task.Result;
            //Console.WriteLine("GOT INITIAL COMPARE AND SWAP VALUE FOR SLOT " + slot + " AND THE VALUE IS " + reply.OutValue);
            if (reply.OutValue > 0)
            {
                isPrimary[slot] = reply.OutValue;
            }
        });
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

        public Task<CompareAndSwapReply> Send(CompareAndSwapRequest req)
        {
            var t = new Task<CompareAndSwapReply>(() =>
            {
                var stub = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(_channel);
                CompareAndSwapReply? reply = null;
                lock (_seqLock)
                {
                    req.Seq = _currentSeq++;
                }
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.CompareAndSwap(req);
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