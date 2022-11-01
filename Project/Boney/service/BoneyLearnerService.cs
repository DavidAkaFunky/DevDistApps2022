using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private int _ack = 0;
    private int currentSlot;
    private readonly int majority;
    private readonly List<BoneyToBankFrontend> boneyToBankFrontends;
    private readonly Dictionary<int, bool> isFrozen;
    private readonly ConcurrentDictionary<int, int> slotsHistory;
    private readonly ConcurrentDictionary<int, List<int>> receivedAccepts = new();
    private readonly object _ackLock = new();

    public BoneyLearnerService(List<BoneyToBankFrontend> boneyToBankFrontends, int numberOfAcceptors,
                               ConcurrentDictionary<int, int> slotsHistory, Dictionary<int, bool> isFrozen, int currentSlot)
    {
        this.boneyToBankFrontends = boneyToBankFrontends;
        majority = (numberOfAcceptors >> 1) + 1;
        this.slotsHistory = slotsHistory;
        this.isFrozen = isFrozen;
        this.currentSlot = currentSlot;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request,
        ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (request.Seq != _ack + 1)
                return Task.FromResult(new AcceptedToLearnerReply { Ack = _ack });
            _ack = request.Seq;
        }

        var reply = new AcceptedToLearnerReply { Ack = request.Seq };

        if (isFrozen[currentSlot]) return Task.FromResult(reply);

        lock (receivedAccepts)
        lock (slotsHistory)
        {
            if (slotsHistory.ContainsKey(request.Slot) && slotsHistory[request.Slot] > 0)
                return Task.FromResult(reply);


            //List -> [valor, timestamp, contador]
            int[] aux = { request.Value, request.TimestampId, 0 };
            var acceptCounter = receivedAccepts.GetValueOrDefault(request.Slot, new List<int>(aux));
            if (!receivedAccepts.TryAdd(request.Slot, acceptCounter)) receivedAccepts[request.Slot] = acceptCounter;


            if (request.TimestampId == acceptCounter[1] && request.Value == acceptCounter[0])
            {
                //se value e timestamp coincidem com accepts anteriors aumenta o contador
                Console.WriteLine("Learner: {0}: NEW Accept\n =======> TS: {1} / Count: {2}"
                    , request.Slot, request.TimestampId, receivedAccepts[request.Slot][2]);
                receivedAccepts[request.Slot][2]++;
            }
            else if (request.TimestampId > acceptCounter[1])
            {
                //caso tenha um timestamp maior que o guardado, recomeca o contador
                Console.WriteLine("Learner: {0}: NEW Accept\n =======> New TS: {1} / Count: {2}"
                    , request.Slot, request.TimestampId, receivedAccepts[request.Slot][2]);
                receivedAccepts[request.Slot][0] = request.Value;
                receivedAccepts[request.Slot][1] = request.TimestampId;
                receivedAccepts[request.Slot][2] = 1;
            }
            else
            {
                Console.WriteLine("Learner: {0}: NEW Accept\n =======> IGNORED", request.Slot);
                return Task.FromResult(reply);
            }

            //verificar se o contador atingiu a maioria
            if (receivedAccepts[request.Slot][2] >= majority)
            {
                Console.WriteLine(
                    "---------------------------Learner: {0}: GOT MAJORITY-------------------------",
                    request.Slot);
                slotsHistory[request.Slot] = receivedAccepts[request.Slot][0];

                boneyToBankFrontends.ForEach(server =>
                {
                    server.SendCompareSwapResult(request.Slot, receivedAccepts[request.Slot][0]);
                });
            }
        }

        //Console.WriteLine("Returned from Learner Routine");
        return Task.FromResult(reply);
    }
}
