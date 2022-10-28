using Grpc.Net.Client;

namespace DADProject.PerfectChannel;

public class Server
{
    private readonly Dictionary<int, Message> _replyCache = new();

    private readonly Dictionary<int, Message> _requestBuffer = new();

    private readonly int _timeout = 10;

    private int _lastAckSent;

    public Server(GrpcChannel channel)
    {
        Channel = channel;
    }

    public GrpcChannel Channel { get; }

    public void HandleRequest(Message message)
    {
        lock (_requestBuffer)
        {
            if (message.Seq >= _lastAckSent + 1) _requestBuffer[message.Seq] = message;
        }
    }

    public void AddReplyToCache(int seq, Message message)
    {
        _replyCache[seq] = message;
    }

    public Message? GetReplyFromCache(int seq)
    {
        return _replyCache.TryGetValue(seq, out var reply) ? reply : null;
    }

    private List<Message> GetProcessableRequests()
    {
        var requests = new List<Message>();
        lock (_requestBuffer)
        {
            var seq = _lastAckSent + 1;

            while (_requestBuffer.TryGetValue(seq, out var req))
            {
                req.Ack = seq;
                requests.Add(req);
                _lastAckSent = seq;
                _requestBuffer.Remove(seq++);
            }
        }

        return requests;
    }
}

public class Client
{
    private readonly Mutex _lastAckMutex = new();
    private readonly Dictionary<int, Message> _messageQueue = new();
    private readonly int _timeout = 10;
    private int _currentSeq = 1;
    private int _lastAckSeen;

    public Client(GrpcChannel channel)
    {
        Channel = channel;
    }

    public GrpcChannel Channel { get; }

    public void Close()
    {
        Channel.ShutdownAsync().RunSynchronously();
    }

    private Message? UnsafeSend(Message message)
    {
        return message.Send(Channel, _timeout);
    }

    private Message SafeSend(Message message)
    {
        Message? reply;
        do
        {
            reply = UnsafeSend(message);
        } while (reply is null);

        return reply;
    }

    public Task<Message> Send(Message message)
    {
        message.Seq = _currentSeq++;
        // possible race condition but i dont't think so
        _messageQueue[message.Seq] = message;
        var task = new Task<Message>(() =>
        {
            var reply = SafeSend(message);
            if (reply.Ack == message.Seq)
            {
                lock (_lastAckMutex)
                {
                    _lastAckSeen = reply.Seq;
                }

                _messageQueue.Remove(message.Seq);
                return reply;
            }

            if (reply.Ack < message.Seq - 1)
            {
                Retransmit();
                reply = SafeSend(message);
                return reply;
            }

            reply = SafeSend(message);
            return reply;
        });

        task.Start();
        return task;
    }

    private void Retransmit()
    {
        int i, initial, total, finished;
        AutoResetEvent finishedEvent = new(false);
        finishedEvent.Reset();
        Mutex m = new();
        lock (_messageQueue)
        {
            lock (_lastAckMutex)
            {
                i = _lastAckSeen + 1;
            }

            initial = i;
            finished = 0;
            while (i < _currentSeq && _messageQueue.TryGetValue(i++, out var req))
                Task.Run(() =>
                {
                    SafeSend(req);
                    lock (m)
                    {
                        finished++;
                    }

                    finishedEvent.Set();
                });
            total = i - finished;
        }

        while (finished != total)
        {
            finishedEvent.Reset();
            finishedEvent.WaitOne();
        }

        lock (_messageQueue)
        {
            for (var j = initial; j < i; j++)
                _messageQueue.Remove(j);
        }
    }
}