using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private readonly List<BoneyToBankFrontend> boneyToBankFrontends;
    private readonly int majority;

    private readonly Dictionary<int, List<int>> receivedAccepts = new();
    private readonly ConcurrentDictionary<int, int> slotsHistory;
    private int ack = 0;
    private int id;

    public BoneyLearnerService(int id, List<BoneyToBankFrontend> boneyToBankFrontends, int numberOfAcceptors,
        ConcurrentDictionary<int, int> slotsHistory)
    {
        this.id = id;
        this.boneyToBankFrontends = boneyToBankFrontends;
        majority = (numberOfAcceptors >> 1) + 1;
        Console.WriteLine(majority.ToString());
        this.slotsHistory = slotsHistory;
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request,
        ServerCallContext context)
    {
        lock (receivedAccepts)
        lock (slotsHistory)
        {
            if (slotsHistory[request.Slot] != -1) return Task.FromResult(new AcceptedToLearnerReply());

            //change var name
            //List -> [valor, timestamp, contador]
            int[] aux = { request.Value, request.TimestampId, 0 };
            var acceptCounter = receivedAccepts.GetValueOrDefault(request.Slot, new List<int>(aux));
            receivedAccepts.Add(request.Slot, acceptCounter);


            if (request.TimestampId == acceptCounter[1] && request.Value == acceptCounter[0])
            {
                //se value e timestamp coincidem com accepts anteriors aumenta o contador
                receivedAccepts[request.Slot][2]++;
                Console.WriteLine("Learner: NEW Accept for slot {0} \n =======> TS: {1} / Count: {2}"
                    , request.Slot, request.TimestampId, receivedAccepts[request.Slot][2]);
            }
            else if (request.TimestampId > acceptCounter[1])
            {
                //caso tenha um timestamp maior que o guardado, recomeca o contador
                receivedAccepts[request.Slot][0] = request.Value;
                receivedAccepts[request.Slot][1] = request.TimestampId;
                receivedAccepts[request.Slot][2] = 1;
                Console.WriteLine("Learner: NEW Accept for slot {0} \n =======> New TS: {1} / Count: {2}"
                    , request.Slot, request.TimestampId, receivedAccepts[request.Slot][2]);
            }
            else
            {
                Console.WriteLine("Learner: NEW Accept for slot {0} \n =======> IGNORED", request.Slot);
                return Task.FromResult(new AcceptedToLearnerReply());
            }

            //verificar se o contador atingiu a maioria
            if (receivedAccepts[request.Slot][2] >= majority)
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