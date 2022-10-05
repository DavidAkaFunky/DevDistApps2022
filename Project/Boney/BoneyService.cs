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
            int slot = request.Slot;
            int outValue;
            lock (multiPaxos)
            {
                if (!multiPaxos.History.TryGetValue(slot, out outValue))
                {
                    outValue = -1; // Otherwise it will be a positive value and Bank will use it as a "canned" consensus reply

                    Task runConsensus = Task.Run(() => multiPaxos.RunConsensus(slot, request.InValue));
                }
            }
            CompareAndSwapReply reply = new() { OutValue = outValue };
            return Task.FromResult(reply);
        }

        public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context)
        {
            lock (multiPaxos)
            {
                int slot = request.Slot;
                Slot values = new(-1, 0, request.Id);
                if (!multiPaxos.Slots.TryGetValue(slot, out values))
                    multiPaxos.AddOrSetSlot(slot, values);
                else if (request.Id > values.ReadTimestamp)
                    multiPaxos.Slots[slot].ReadTimestamp = request.Id;
                PromiseReply reply = new() { Slot = slot, Id = values.ReadTimestamp, Value = values.CurrentValue };
                return Task.FromResult(reply);
            }
        }

        public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context)
        {
            lock (multiPaxos)
            {
                int slot = request.Slot;
                bool status = true;
                Slot values = new(request.Value, request.Id, request.Id);
                if (!multiPaxos.Slots.TryGetValue(slot, out values)) // Just in case it didn't get any of the former messages
                    multiPaxos.AddOrSetSlot(slot, values);
                if (values.WriteTimestamp != values.ReadTimestamp)
                    status = false;
                else
                    // It's ugly because we're doing it twice if it didn't exist initially,
                    // but only once if it already existed
                    multiPaxos.AddOrSetSlot(slot, values);
                // TODO: It must be sent to *all* channels
                // TODO #2: It must send to *all bank clients* if it reached a majority
                AcceptReply reply = new() { Status = status };
                return Task.FromResult(reply);
            }
        }
    }
}