using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private int id;
    private int ack = 0;
    private readonly int majority;

    private readonly List<BoneyToBankFrontend> boneyToBankFrontends;

    private readonly Dictionary<int, List<int>> receivedAccepts = new();
    private ConcurrentDictionary<int, int> slotsHistory;

    public BoneyLearnerService(int id, List<BoneyToBankFrontend> boneyToBankFrontends, int numberOfAcceptors, ConcurrentDictionary<int, int> slotsHistory)
    {
        this.id = id;
        this.boneyToBankFrontends = boneyToBankFrontends;
        this.majority = numberOfAcceptors / 2;
        Console.WriteLine(majority.ToString());
        this.slotsHistory = slotsHistory;
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request, ServerCallContext context)
    {
        lock(receivedAccepts) lock (slotsHistory)
        {
                if (slotsHistory.ContainsKey(request.Slot)) return Task.FromResult(new AcceptedToLearnerReply());

                //change var name
                //List -> [valor, timestamp, contador]
                int[] aux = { request.Value, request.TimestampId, 0 };
                var acceptCounter = receivedAccepts.GetValueOrDefault(request.Slot, new List<int>(aux));

                
                if (request.TimestampId == acceptCounter[1] && request.Value == acceptCounter[0])
                    //se value e timestamp coincidem com accepts anteriors aumenta o contador
                    receivedAccepts[request.Slot][2]++;
                else if (request.TimestampId > acceptCounter[1])
                {
                    //caso tenha um timestamp maior que o guardado, recomeca o contador
                    receivedAccepts[request.Slot][0] = request.Value;
                    receivedAccepts[request.Slot][2] = 1;
                    Console.WriteLine("Learner: Got new Accept with new TS {1} / Count: {2}"
                        , request.Slot, request.TimestampId, receivedAccepts[request.Slot][2]);
                }

                //verificar se o contador atingiu a maioria
                if (receivedAccepts[request.Slot][2] == majority)
                {

                    Console.WriteLine("Learner: Got majority for slot {0}", request.Slot);
                    slotsHistory[request.Slot] = receivedAccepts[request.Slot][1];

                    boneyToBankFrontends.ForEach(server =>
                    {
                        server.SendCompareSwapResult(request.Slot, receivedAccepts[request.Slot][1]);
                    });
                }
            }

        return Task.FromResult(new AcceptedToLearnerReply());
    }

}
