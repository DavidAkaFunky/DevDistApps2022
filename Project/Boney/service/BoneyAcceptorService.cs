using Grpc.Core;

namespace DADProject;

// ChatServerService is the namespace defined in the protobuf
// ChatServerServiceBase is the generated base implementation of the service
public class BoneyAcceptorService : ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceBase
{
    private List<BoneyFrontend> _serverFrontends;
    private int _ack;
    private int _id;
    private Dictionary<int, Slot> _slots;

    public BoneyAcceptorService(int id, List<BoneyFrontend> frontends)
    {
        _id = id;
        _ack = 0;
        _serverFrontends = frontends;
    }

    public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context)
    {
        lock (acceptor)
        {
            int slot = request.Slot;
            Slot values = new(Slot.Null, 0, request.Id);
            Console.WriteLine("Received prepare request from " + request.Id + " for slot " + slot);
            if (!acceptor.Slots.TryGetValue(slot, out values))
                acceptor.AddOrSetSlot(slot, values);
            else if (request.Id > values.ReadTimestamp)
                acceptor.Slots[slot].ReadTimestamp = request.Id;
            PromiseReply reply = new() { Slot = slot, Id = acceptor.Slots[slot].ReadTimestamp, Value = acceptor.Slots[slot].CurrentValue };
            return Task.FromResult(reply);
        }
    }

    public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context)
    {
        lock (acceptor)
        {
            int slot = request.Slot;
            int value = request.Value;
            bool status = true;
            Console.WriteLine("Received accept request from " + request.Id + " for slot " + slot + " with value " + value);
            Slot values = new(value, request.Id, request.Id);
            if (!acceptor.Slots.TryGetValue(slot, out values) || request.Id >= values.ReadTimestamp)
            {
                acceptor.AddOrSetSlot(slot, values);
                acceptor.SendAcceptedToLearners(slot, value);
            }
            else
                status = false;
            AcceptReply reply = new() { Status = status };
            return Task.FromResult(reply);
        }
    }

}
