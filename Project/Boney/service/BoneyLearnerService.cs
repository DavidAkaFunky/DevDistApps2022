using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
{
    private BoneyLearner learner;

    public BoneyLearnerService(List<string> servers, List<string> clients)
    {
        learner = new(servers, clients);
    }

    public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request, ServerCallContext context)
    {
        learner.ReceiveAccepted(request.Slot, context.RequestHeaders.GetValue("learnerAddress"), request.Id, request.Value);
        AcceptedToLearnerReply reply = new();
        return Task.FromResult(reply);
    }

}
