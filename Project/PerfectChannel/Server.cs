namespace DADProject.PerfectChannel;

/*
 * Framework para Implementação de Canais Fiáveis
 * Nos Construtores tanto de Client como Server é necessário adicionar ao dicionário as funções de tratamento / envio
 * Receita: Herdar, Implementar construtor - Importante: casts dentro das funções
 */

public class Metadata
{
    public Metadata(int ack, int seq)
    {
        Ack = ack;
        Seq = seq;
    }

    public int Ack { get; set; }
    public int Seq { get; set; }
}

public class Message
{
    protected Message(object request, Type type)
    {
        Request = request;
        Type = type;
    }

    public Type Type { get; }

    public object Request { get; }

    public static T From<T>(object o)
    {
        if (o is T instance)
            return instance;
        throw new Exception("Illegal Cast");
    }

    public static Message To<T>(T o)
    {
        return new Message(o!, o!.GetType());
    }
}

public abstract class Server
{
    //TODO usar isto
    private readonly Dictionary<int, Message> _replyCache = new();
    private readonly Dictionary<int, Message> _requestBuffer = new();
    protected readonly Dictionary<Type, Func<Message, Message>> _treatmentRoutines = new();
    private int _lastSeen;

    protected abstract Metadata GetMetadata(Message o);

    public List<Message> GetAvailableRequests(Metadata requestMetadata, Message request,
        out Metadata replyMetadata)
    {
        var availableRequests = new List<Message>();

        if (requestMetadata.Seq - 1 == _lastSeen)
        {
            availableRequests.Add(request);
            _lastSeen = requestMetadata.Seq;
            while (_requestBuffer.TryGetValue(_lastSeen + 1, out var req))
            {
                availableRequests.Add(req);
                _requestBuffer.Remove(_lastSeen++);
            }
        }

        if (requestMetadata.Seq - 1 > _lastSeen)
            _requestBuffer.Add(requestMetadata.Seq, request);

        replyMetadata = new Metadata(_lastSeen, 0);
        return availableRequests;
    }

    // returns list of replies for all the requests
    public List<Message> TreatRequests(List<Message> requests)
    {
        var replies = new List<Message>();
        requests.ForEach(req =>
        {
            if (!_treatmentRoutines.TryGetValue(req.Type, out var routine))
                throw new Exception("No treatment routine available for this type");
            replies.Add(routine(req));
        });

        return replies;
    }

    public Message HandleRequest<T>(T request)
    {
        var m = Message.To(request);
        var metadata = GetMetadata(m);
        // check cached replies
        if (_replyCache.TryGetValue(metadata.Seq, out var reply))
        {
            _replyCache.Remove(metadata.Seq);
            return reply;
        }

        var requests = GetAvailableRequests(metadata, m, out var replyMetadata);
        var replies = TreatRequests(requests);
        replies.ForEach(msg => { _replyCache[GetMetadata(msg).Ack] = msg; });
        return _replyCache[metadata.Seq];
    }
}

public abstract class Client
{
    private readonly List<Message> _messageQueue = new();

    protected readonly Dictionary<Type, Func<Metadata, Message, int, Message?>> _senders = new();
    private readonly int _timeout = 10;
    private int _currentSeq = 1;
    private int _lastAcked;

    private Message Send(Message request)
    {
        Metadata metadata;
        lock (_messageQueue)
        {
            metadata = new Metadata(_lastAcked, _currentSeq++);
            _messageQueue.Add(request);
        }

        return Send(metadata, request);
    }

    private Message Send(Metadata metadata, Message request)
    {
        if (!_senders.TryGetValue(request.Type, out var send))
            throw new Exception("No routine to send this type of object");

        Message? reply;
        do
        {
            reply = send(metadata, request, _timeout);
        } while (reply is null);

        return reply;
    }

    protected abstract Metadata GetMetadata(Message o);

    //TODO SendWithRetransmit
    // manda com Send(1)
    // checka ack e seq, se estiver tudo bem retorna
    // cc chama Retransmit
    // chama Send(2) e obtém resposta cached no servidor
    // pode-se fazer flush da cache se resposta já foi reenviada
    // ou seja, if(tryGetValue(..)) responde e apaga da cache
    //TODO server side response caching to allow client to obtain answers in all cases

    //TODO metodo que pegue na reply e verifique o ack, provavelmente metodo abstrato que nas implementacoes concretas \
    // TODO tera switch com Tipos de mensagem para conseguir fazer cast da resposta

    public Task<Message> SendWithRetransmit<T>(T message)
    {
        var req = Message.To(message);
        var task = new Task<Message>(() =>
        {
            var reply = Send(req);

            var metadata = GetMetadata(reply);
            lock (_messageQueue)
            {
                if (metadata.Ack == _lastAcked + 1)
                {
                    _lastAcked++;
                    _messageQueue.RemoveAt(0);
                    return reply;
                }
            }

            Retransmit();

            //get the cached reply
            return Send(new Metadata(0, metadata.Ack + 1), req);
        });
        task.Start();

        return task;
    }

    private async void Retransmit()
    {
        var tasks = new List<Task<Message>>();
        var i = 0;
        lock (_messageQueue)
        {
            _messageQueue.ForEach(m =>
            {
                tasks.Add(new Task<Message>(() => Send(new Metadata(_lastAcked, _lastAcked + ++i), m)));
            });
        }

        //TODO uma otimizacao era devolver aqui a resposta da primeira mensagem, evitava 1 RTT no SendWithRetransmit
        foreach (var task in tasks)
            await task;

        lock (_messageQueue)
        {
            _messageQueue.RemoveRange(0, i);
        }
    }
}