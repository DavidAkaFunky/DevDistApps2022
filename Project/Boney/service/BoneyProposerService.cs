using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private int id;
    private int currentSlot;
    private readonly List<BoneyToBoneyFrontend> serverFrontends;

    private readonly ConcurrentDictionary<int, int> slotsHistory;
    private readonly Dictionary<int, int> firstBlood = new(); // Might not be needed
    private readonly Dictionary<int, bool> sendAccept = new();
    private readonly Dictionary<int, bool> isPerceivedLeader = new();

    public BoneyProposerService(int id, List<BoneyToBoneyFrontend> frontends, Dictionary<int, List<int>> nonSuspectedServers, ConcurrentDictionary<int, int> slotsHistory)
    {
        this.id = id;
        this.currentSlot = 1;
        this.serverFrontends = frontends;
        this.slotsHistory = slotsHistory;

        foreach (KeyValuePair<int, List<int>> slot in nonSuspectedServers)
        {
            // pensa que e lider se a lista de servidores vivos para um slot n estiver vazia (duh)
            // e se o minimo dos valores da lista for ele proprio
            isPerceivedLeader[slot.Key] = slot.Value.Count > 0 && slot.Value.Min() == id;
        }
    }

    public int CurrentSlot
    {
        get => currentSlot;
        set => currentSlot = value;
    }

    public void AbortAllThreads(List<Thread> threads)
    {
        threads.ForEach(thread => thread.Abort());
        threads.Clear();
    }

    public void CheckMajority(int responses, List<Thread> threads)
    {
        if (responses > serverFrontends.Count / 2)
            AbortAllThreads(threads);
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        var reply = new CompareAndSwapReply() { OutValue = -1 };

        Console.WriteLine("I THINK I AM THE LEADER FOR SLOT " + request.Slot);

        lock (slotsHistory) // If using, lock (firstBlood)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(currentSlot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);

            slotsHistory[currentSlot] = -1; //caso ja tenha começado consensus

            // pode n ser preciso 
            // verificar se já começou algum consenso para o slot, se sim retorna
            //if (firstBlood.ContainsKey(currentSlot)) return Task.FromResult(reply);

            //firstBlood.Add(currentSlot, request.InValue);
        }

        // se eu for o lider, prossigo
        // se nao (dicionario vazio ou o menor valor n é o meu), desisto
        if (!isPerceivedLeader[request.Slot]) return Task.FromResult(reply);

        Console.WriteLine("STARTING CONSENSUS FOR SLOT " + request.Slot);

        var value = request.InValue;
        var timestamp = id;
        var stop = false;

        // se eu for o lider:
        // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
        // espero por maioria de respostas (Promise com valor, id da msg mais recente)
        // escolher valor mais recente das Promises

        if (id != 1)
        {
            serverFrontends.ForEach(server =>
            {
                var response = server.Prepare(currentSlot, id);

                //TODO stop on nack (-1, -1)
                if (response.Value == -1 && response.WriteTimestamp == -1)
                {
                    Console.WriteLine("RECEIVED NACK FROM SERVER");
                    stop = true;
                } 
                else if (response.WriteTimestamp > timestamp)
                {
                    Console.WriteLine("RECEIVED **ACK** FROM SERVER");
                    value = response.Value;
                    timestamp = response.WriteTimestamp;
                }
            });
        }

        if (stop) return Task.FromResult(reply);

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