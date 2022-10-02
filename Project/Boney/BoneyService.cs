using Grpc.Core;

namespace DADProject
{

    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BoneyService : ProjectBoneyService.ProjectBoneyServiceBase
    {
        private MultiPaxos multiPaxos;

        public BoneyService(MultiPaxos multiPaxos) { this.multiPaxos = multiPaxos; }

        public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
        {
            CompareAndSwapReply reply = new CompareAndSwapReply { Slot = request.Slot, OutValue = multiPaxos.RunConsensus(request.Slot) };
            return Task.FromResult(reply);
        }
    }
}
