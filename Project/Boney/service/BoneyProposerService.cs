using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private int currentSlot;

    public int CurrentSlot
    {
        get => currentSlot;
        set => currentSlot = value;
    }

    private int id;

    private readonly Dictionary<int, int> slotsHistory = new();
    private readonly Dictionary<int, int> firstBlood = new();
    private readonly List<BoneyFrontend> serverFrontends = new();

    public BoneyProposerService(int id, List<BoneyFrontend> frontends)
    {
        this.id = id;
        this.currentSlot = 1;
        this.serverFrontends = frontends;
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        // calcular quem vai ser o "lider" do paxos
        var possibleLeaders = new List<List<int>>();

        var reply = new CompareAndSwapReply();

        // se eu for o lider, prossigo
        // se nao, morro
        if (!possibleLeaders[currentSlot].Contains(id)) return Task.FromResult(reply);

        lock (slotsHistory) lock (firstBlood)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(currentSlot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);

            // verificar se já começou algum consenso para o slot, se sim retorna
            if (firstBlood.ContainsKey(currentSlot)) return Task.FromResult(reply);

            firstBlood.Add(currentSlot, request.InValue);
        }

        var value = request.InValue;
        var timestamp = 0;

        // TODO esperamos por todos mas so precisa maioria (usar a check majority antiga?)
        
        // se eu for o lider:
        // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
        // espero por maioria de respostas (Promise com id da msg mais recente, valor)
        // escolher valor mais recente das Promises
        if (id != 1)
            serverFrontends.ForEach(server =>
            {
                // (value: int, timestampId: int)
                var response = server.Prepare(currentSlot, id);
                if (response.Item2 > timestamp)
                {
                    timestamp = response.Item2;
                    value = response.Item1;
                }
            });
        
        // enviar accept(<value mais recente>) a todos
        // TODO meter isto assincrono (tasks ou threads?)
        // esperar por maioria (para efeitos de historico)
        serverFrontends.ForEach(server =>
        {
            server.Accept(currentSlot, timestamp, value);
        });

        return Task.FromResult(reply);

    }
}