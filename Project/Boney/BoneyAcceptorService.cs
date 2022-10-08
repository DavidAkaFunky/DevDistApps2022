using Grpc.Core;

namespace DADProject
{

    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BoneyAcceptorService : ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceBase
    {
        private BoneyAcceptor acceptor;

        public BoneyAcceptorService(int id, string[] servers)
        {
            acceptor = new(id, servers);
        }

        public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context) {
            lock (acceptor)
              {
                  int slot = request.Slot;
                  Slot values = new(Slot.Null, 0, request.Id);
                  if (!acceptor.Slots.TryGetValue(slot, out values))
                      acceptor.AddOrSetSlot(slot, values);
                  else if (request.Id > values.ReadTimestamp)
                      acceptor.Slots[slot].ReadTimestamp = request.Id;
                  PromiseReply reply = new() { Slot = slot, Id = values.ReadTimestamp, Value = values.CurrentValue };
                  return Task.FromResult(reply);
              }
          }

          public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context) {
            lock (acceptor)
              {
                  int slot = request.Slot;
                  bool status = true;
                  Slot values = new(request.Value, request.Id, request.Id);
                  if (!acceptor.Slots.TryGetValue(slot, out values)) // Just in case it didn't get any of the former messages
                      acceptor.AddOrSetSlot(slot, values);
                  if (values.WriteTimestamp != values.ReadTimestamp)
                      status = false;
                  else
                      // It's ugly because we're doing it twice if it didn't exist initially,
                      // but only once if it already existed
                      acceptor.AddOrSetSlot(slot, values);
                  // TODO: It must be sent to *all* channels
                  // TODO #2: It must send to *all bank clients* if it reached a majority
                  AcceptReply reply = new() { Status = status };
                  return Task.FromResult(reply);
              }
          }
        
    }
}
