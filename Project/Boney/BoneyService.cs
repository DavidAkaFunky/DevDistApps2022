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
            lock (multiPaxos)
            {
                CompareAndSwapReply reply = new() { Slot = request.Slot, OutValue = multiPaxos.RunConsensus(request.Slot, request.InValue) };
                return Task.FromResult(reply);
            }
        }

        public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context)
        {
            lock (multiPaxos)
            { 
                int slot = request.Slot;
                int[] values = new int[3] { -1, 0, request.Id };
                if (!multiPaxos.Slots.TryGetValue(slot, out values))
                    multiPaxos.AddOrSetSlot(slot, values);
                else if (request.Id > values[2])
                    multiPaxos.Slots[slot][2] = request.Id;
                PromiseReply reply = new() { Slot = slot, Id = values[2], Value = values[0] };
                return Task.FromResult(reply);
            }
        }

        public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context)
        {   
            lock (multiPaxos)
            {
                int slot = request.Slot;
                bool status = true;
                int[] values = new int[3] { request.Value, request.Id, request.Id };
                if (!multiPaxos.Slots.TryGetValue(slot, out values)) // Just in case it didn't get any of the former messages
                    multiPaxos.AddOrSetSlot(slot, values);
                if (values[1] != values[2])
                    status = false;
                else
                    // It's ugly because we're doing it twice if it didn't exist initially,
                    // but only once if it already existed
                    multiPaxos.AddOrSetSlot(slot, values); 
                AcceptReply reply = new() { Status = status };
                return Task.FromResult(reply);
            }
           
        }
    }
}
