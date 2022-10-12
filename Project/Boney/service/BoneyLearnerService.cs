using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private int id;
    private int ack = 0;
    private readonly List<BoneyToBankFrontend> boneyToBankFrontends;
    private readonly int numberOfAcceptors;
    private readonly Dictionary<int, Dictionary<int, int>> acceptedValues = new();
    private ConcurrentDictionary<int, int> slotsHistory;

    public BoneyLearnerService(int id, List<BoneyToBankFrontend> boneyToBankFrontends, int numberOfAcceptors, ConcurrentDictionary<int, int> slotsHistory)
    {
        this.id = id;
        this.boneyToBankFrontends = boneyToBankFrontends;
        this.numberOfAcceptors = numberOfAcceptors;
        this.slotsHistory = slotsHistory;
    }

    public bool HasConsensualMajority(int slot)
    {
        return acceptedValues[slot].Count > numberOfAcceptors / 2
            && new List<int>(acceptedValues[slot].Values.Distinct()).Count == 1;
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request, ServerCallContext context)
    {
        lock(acceptedValues) lock (slotsHistory)
        {
            if (!acceptedValues.ContainsKey(request.Slot))
                acceptedValues.Add(request.Slot, new Dictionary<int, int>());
            acceptedValues[request.Slot][id] = request.Value;

            if (HasConsensualMajority(request.Slot))
            {
                boneyToBankFrontends.ForEach(server =>
                {
                    server.SendCompareSwapResult(request.Slot, request.Value);
                });

                slotsHistory[request.Slot] = request.Value;
            }
        }

        return Task.FromResult(new AcceptedToLearnerReply());
    }

}
