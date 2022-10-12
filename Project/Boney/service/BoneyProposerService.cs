using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private int id;
    private int currentSlot;
    private readonly List<BoneyToBoneyFrontend> serverFrontends;

    private readonly Dictionary<int, int> slotsHistory = new();//change to concurrentDic
    private readonly Dictionary<int, int> firstBlood = new();

    public BoneyProposerService(int id, List<BoneyToBoneyFrontend> frontends)
    {
        this.id = id;
        this.currentSlot = 1;
        this.serverFrontends = frontends;
    }

    public int CurrentSlot
    {
        get => currentSlot;
        set => currentSlot = value;
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        // calcular quem vai ser o "lider" do paxos
        var possibleLeaders = new List<List<int>>();

        var reply = new CompareAndSwapReply();

        lock (slotsHistory) lock (firstBlood)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(currentSlot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);

            // slotsHistory[currentSlot] = -1 

            // pode n ser preciso 
            // verificar se já começou algum consenso para o slot, se sim retorna
            if (firstBlood.ContainsKey(currentSlot)) return Task.FromResult(reply);

            firstBlood.Add(currentSlot, request.InValue);
        }

        // se eu for o lider, prossigo
        // se nao, morro
        // TODO: Isto tem de ser trocado para ver qual é o menor valor!
        if (!possibleLeaders[currentSlot].Contains(id)) return Task.FromResult(reply);

        var value = request.InValue;
        var timestamp = id;

        // TODO esperamos por todos mas so precisa maioria (usar a check majority antiga?)

        // se eu for o lider:
        // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
        // espero por maioria de respostas (Promise com valor, id da msg mais recente)
        // escolher valor mais recente das Promises
        if (id != 1)
            serverFrontends.ForEach(server =>
            {
                // (value: int, timestampId: int)
                var reply = server.Prepare(currentSlot, id);

                //TODO stop on nack (-1, -1)
                if (reply.Value == -1 && reply.WriteTimestamp == -1)
                    return; // ISTO PROVAVELMENTE N VAI FECHAR TUDO :/

                if (reply.WriteTimestamp > timestamp)
                {
                    value = reply.Value;
                    timestamp = reply.WriteTimestamp;
                }
            });
        
        // enviar accept(<value mais recente>) a todos
        // TODO: meter isto assincrono (tasks ou threads?)
        // esperar por maioria (para efeitos de historico)
        serverFrontends.ForEach(server =>
        {
            server.Accept(currentSlot, timestamp, value);
        });

        return Task.FromResult(reply);

    }
}