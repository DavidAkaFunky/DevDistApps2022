using Grpc.Core;

namespace DADProject;

public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private int id;
    private int ack = 0;
    private readonly List<BoneyToBankFrontend> boneyToBankFrontends;
    private readonly List<BoneyToBoneyFrontend> boneyToBoneyFrontends;
    private readonly Dictionary<int, Tuple<int, int>> acceptedMessagess = new();
    private readonly Dictionary<int, int> slotsHistory = new();//change to concurrentDic

    public BoneyLearnerService(int id, List<BoneyToBankFrontend> boneyToBankFrontends, List<BoneyToBoneyFrontend> boneyToBoneyFrontends)
    {
        this.id = id;
        this.boneyToBankFrontends = boneyToBankFrontends;
        this.boneyToBoneyFrontends = boneyToBoneyFrontends;
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request, ServerCallContext context)
    {
        

        if(true)
        {
            boneyToBankFrontends.ForEach(server =>
            {
                server.SendCompareSwapResult(request.Slot, request.Value);
            });

            //might not be needed
            boneyToBoneyFrontends.ForEach(server =>
            {
                server.ResultToProposer(request.Slot, request.Value);
                
            });
        }

        AcceptedToLearnerReply reply = new();
        return Task.FromResult(reply);
    }

}
