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

        // se eu for o lider, prossigo
        // se nao (dicionario vazio ou o menor valor n é o meu), desisto
        if (!isPerceivedLeader[request.Slot]) return Task.FromResult(reply);

        Console.WriteLine("I THINK I AM THE LEADER FOR SLOT " + request.Slot);

        lock (slotsHistory) // If using, lock (firstBlood)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(currentSlot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);

            slotsHistory[currentSlot] = -1;

            // pode n ser preciso 
            // verificar se já começou algum consenso para o slot, se sim retorna
            //if (firstBlood.ContainsKey(currentSlot)) return Task.FromResult(reply);

            //firstBlood.Add(currentSlot, request.InValue);
        }

        Console.WriteLine("STARTING CONSENSUS FOR SLOT " + request.Slot);

        var value = request.InValue;
        var timestamp = id;

        // se eu for o lider:
        // vou mandar um prepare(n) para todos os acceptors (assumindo que nao sou o primeiro)
        // espero por maioria de respostas (Promise com valor, id da msg mais recente)
        // escolher valor mais recente das Promises
        var responses = 0;
        var threads = new List<Thread>();
        var stop = false;
        sendAccept[currentSlot] = true;

        //if (id != 1)
        {
            sendAccept[currentSlot] = false;
            serverFrontends.ForEach(server =>
            {
                // (value: int, timestampId: int)
                var reply = server.Prepare(currentSlot, id);

                //TODO stop on nack (-1, -1)
                if (reply.Value == -1 && reply.WriteTimestamp == -1)
                {
                    //AbortAllThreads(threads);
                    stop = true;
                    Console.WriteLine("RECEIVED NACK FROM SERVER");
                    return; // ISTO PROVAVELMENTE N VAI FECHAR TUDO :/
                }

                Console.WriteLine("RECEIVED **ACK** FROM SERVER");

                if (reply.WriteTimestamp > timestamp)
                {
                    Console.WriteLine("Updating value...");
                    value = reply.Value;
                    timestamp = reply.WriteTimestamp;
                }

                //CheckMajority(++responses, threads);
            });
        }

        // isto é ultra feio, eu sei, aceito sugestões melhores
        //while (!sendAccept[currentSlot]);

        Console.WriteLine("STOP? " + stop);
        if (stop) return Task.FromResult(reply);

        // enviar accept(<value mais recente>) a todos
        // TODO: meter isto assincrono (tasks ou threads?)
        // esperar por maioria (para efeitos de historico)
        Console.WriteLine("SENDING ACCEPT: SLOT " + currentSlot + " VALUE: " + value);
        serverFrontends.ForEach(server =>
        {
            server.Accept(currentSlot, timestamp, value);
        });

        return Task.FromResult(reply);

    }
}