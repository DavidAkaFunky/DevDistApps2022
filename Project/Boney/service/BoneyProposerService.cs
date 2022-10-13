using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private readonly int id;
    private readonly Dictionary<int, bool> isPerceivedLeader = new();
    private readonly List<BoneyToBoneyFrontend> serverFrontends;

    private readonly ConcurrentDictionary<int, int> slotsHistory;

    public BoneyProposerService(int id, List<BoneyToBoneyFrontend> frontends,
        Dictionary<int, List<int>> nonSuspectedServers, ConcurrentDictionary<int, int> slotsHistory)
    {
        this.id = id;
        CurrentSlot = 1;
        serverFrontends = frontends;
        this.slotsHistory = slotsHistory;

        foreach (var slot in nonSuspectedServers)
            // pensa que e lider se a lista de servidores vivos para um slot n estiver vazia (duh)
            // e se o minimo dos valores da lista for ele proprio
            isPerceivedLeader[slot.Key] = slot.Value.Count > 0 && slot.Value.Min() == id;
    }

    public int CurrentSlot { get; set; }

    //public void AbortAllThreads(List<Thread> threads)
    //{
    //    threads.ForEach(thread => thread.Abort());
    //    threads.Clear();
    //}

    //public void CheckMajority(int responses, List<Thread> threads)
    //{
    //    if (responses > serverFrontends.Count / 2)
    //        AbortAllThreads(threads);
    //}

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        Console.WriteLine("TOU AQUI");
        var reply = new CompareAndSwapReply { OutValue = -1 };

        lock (slotsHistory) // If using, lock (firstBlood)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(CurrentSlot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);

            slotsHistory[CurrentSlot] = -1; //caso ja tenha começado consensus
        }

        // se eu for o lider, prossigo
        // se nao (dicionario vazio ou o menor valor n é o meu), desisto
        if (!isPerceivedLeader[CurrentSlot]) return Task.FromResult(reply);

        Console.WriteLine("Proposer: STARTING CONSENSUS FOR SLOT " + request.Slot);

        var value = request.InValue;
        var timestamp = id;
        var stop = false;

        // se eu for o lider:
        // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
        // espero por maioria de respostas (Promise com valor, id da msg mais recente)
        // escolher valor mais recente das Promises

        if (id != 1)
            serverFrontends.ForEach(server =>
            {
                var response = server.Prepare(request.Slot, id);

                if (response.Value == -1 && response.WriteTimestamp == -1)
                {
                    Console.WriteLine("Proposer: RECEIVED **NACK**");
                    stop = true;
                }
                else if (response.WriteTimestamp > timestamp)
                {
                    Console.WriteLine("Procposer: RECEIVED **ACK**");
                    value = response.Value;
                    timestamp = response.WriteTimestamp;
                }
            });

        if (stop) return Task.FromResult(reply);

        Console.WriteLine("Proposer: Send ACCEPT for slot {0} \n ========> Value: {1} / TS: {2}",
            CurrentSlot, value, timestamp);

        // enviar accept(<value mais recente>) a todos
        // TODO: meter isto assincrono (tasks ou threads?)
        // esperar por maioria (para efeitos de historico)
        serverFrontends.ForEach(server =>
        {
            Console.WriteLine($"SENT ACCEPT TO {server.ServerAddress}");
            server.Accept(request.Slot, timestamp, value);
            Console.WriteLine($"SENT ACCEPT TO {server.ServerAddress} 2");
        });

        return Task.FromResult(reply);
    }
}