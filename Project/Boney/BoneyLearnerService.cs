using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DADProject
{

    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BoneyLearnerService : ProjectBoneyLearnerService.ProjectBoneyLearnerServiceBase
    {
        private BoneyLearner acceptor;

        public BoneyLearnerService(int id, string[] servers)
        {
            acceptor = new(id, servers);
        }

        public override Task<AcceptedToLearnerReply> AcceptedToLearner(AcceptedToLearnerRequest request, ServerCallContext context) {
            // TODO
            AcceptedToLearnerReply reply = new();
            return Task.FromResult(reply);
        }

    }
}
