using Grpc.Core;
using System.Collections.Concurrent;

namespace DADProject;

public class BoneyAcceptorService : ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceBase
{
    private readonly int id;
    private int ack = 0;
    private readonly List<BoneyToBoneyFrontend> serverFrontends;
    private readonly ConcurrentDictionary<int, Slot> slotsInfo;

    public BoneyAcceptorService(int id, List<BoneyToBoneyFrontend> frontends, ConcurrentDictionary<int, Slot> slotsInfo)
    {
        this.id = id;
        serverFrontends = frontends;
        this.slotsInfo = slotsInfo;
    }

    // TODO juntar metadata

    public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context)
    {
        var reply = new PromiseReply();

        lock (slotsInfo)
        {
            var slotInfo = slotsInfo.GetValueOrDefault(request.Slot, new Slot(Slot.Bottom, 0, 0));

            // compara request.TimestampId com readTimestamp
            if (request.TimestampId >= slotInfo.ReadTimestamp)
            {
                // se request.TimestampId > readTimestamp, responde ack(valor, writeTimestamp)
                // onde valor e o valor mais recente aceite (ou ⊥/Bottom/-1 se n existir), e writeTimestamp e o instante da proposta
                Console.WriteLine("Acceptor: {0}: ACCEPTED Prepare with timestamp {1}", request.Slot, request.TimestampId);
                slotInfo.ReadTimestamp = request.TimestampId;

                reply.Value = slotInfo.CurrentValue;
                reply.WriteTimestamp = slotInfo.WriteTimestamp;
            }
            else
            {
                Console.WriteLine("Acceptor: {0}: REJECTED Prepare with timestamp {1}", request.Slot, request.TimestampId);
                //caso contrario, envia nack(-1, -1)
                reply.Value = -1;
                reply.WriteTimestamp = -1;
            }

            // atualiza tuplo do acceptor para o slot dado
            slotsInfo[request.Slot] = slotInfo;
        }

        return Task.FromResult(reply);
    }

    public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context)
    {
        var reply = new AcceptReply { Status = false };

        lock (slotsInfo)
        {
            var slotInfo = slotsInfo.GetValueOrDefault(request.Slot, new Slot(Slot.Bottom, 0, 0));

            //ao receber accept(valor, request.timestampId),
            //o acceptor aceita valor a nao ser que readTimestamp > request.TimestampId.
            if (slotInfo.ReadTimestamp <= request.TimestampId)
            {
                Console.WriteLine("Acceptor: {0}: ACCEPTED Accept with timestamp {1} and value {2}",
                    request.Slot, request.TimestampId, request.Value);
                
                reply.Status = true;

                slotInfo.CurrentValue = request.Value;
                slotInfo.WriteTimestamp = request.TimestampId;

                // atualiza tuplo do acceptor para o slot dado
                slotsInfo[request.Slot] = slotInfo;
            } else
            {
                Console.WriteLine("Acceptor: {0}: REJECTED Accept with timestamp {1} and value {2}",
                    request.Slot, request.TimestampId, request.Value);
            }
        }

        // Se foi aceite, avisar os learners
        if (reply.Status)
            serverFrontends.ForEach(server =>
            {
                Console.WriteLine("Acceptor : -- Sent Accepted To Learner");
                server.AcceptedToLearner(request.Slot, request.TimestampId, request.Value);
                Console.WriteLine("Acceptor : -- Returned from sending Accepted To Learner");
            });

        Console.WriteLine("Returning from accept routine");
        return Task.FromResult(reply);
    }
}