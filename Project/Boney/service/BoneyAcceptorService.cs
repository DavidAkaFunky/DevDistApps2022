﻿using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DADProject;


public class BoneyAcceptorService : ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceBase
{
    private int id;
    private int ack = 0;
    private List<BoneyToBoneyFrontend> serverFrontends;
    private Dictionary<int, Slot> slots = new();

    public BoneyAcceptorService(int id, List<BoneyToBoneyFrontend> frontends)
    {
        this.id = id;
        this.serverFrontends = frontends;
    }

    // TODO juntar metadata

    public override Task<PromiseReply> Prepare(PrepareRequest request, ServerCallContext context)
    {
        var reply = new PromiseReply();

        lock (slots)
        {
            var slotInfo = slots.GetValueOrDefault(request.Slot, new(Slot.Bottom, 0, 0));

            // compara request.TimestampId com readTimestamp
            if (request.TimestampId >= slotInfo.ReadTimestamp)
            {
                // se request.TimestampId > readTimestamp, responde ack(valor, writeTimestamp)
                // onde valor e o valor mais recente aceite (ou ⊥/Bottom/-1 se n existir), e writeTimestamp e o instante da proposta

                slotInfo.ReadTimestamp = request.TimestampId;

                reply.Value = slotInfo.CurrentValue;
                reply.WriteTimestamp = slotInfo.WriteTimestamp;
            }
            else
            {
                //caso contrario, envia nack(-1, -1)
                reply.Value = -1;
                reply.WriteTimestamp = -1;
            }

            // atualiza tuplo do acceptor para o slot dado
            slots[request.Slot] = slotInfo;
        }

        return Task.FromResult(reply);
    }

    public override Task<AcceptReply> Accept(AcceptRequest request, ServerCallContext context)
    {
        AcceptReply reply = new()
        {
            Status = false
        };

        lock (slots)
        {
            Slot slotInfo = slots.GetValueOrDefault(request.Slot, new(Slot.Bottom, 0, 0));

            //ao receber accept(valor, request.timestampId),
            //o acceptor aceita valor a nao ser que readTimestamp > request.TimestampId.

            if (slotInfo.ReadTimestamp <= request.TimestampId)
            {
                reply.Status = true;

                slotInfo.CurrentValue = request.Value;
                slotInfo.WriteTimestamp = request.TimestampId;

                // atualiza tuplo do acceptor para o slot dado
                slots[request.Slot] = slotInfo;
            }
        }

        // TODO meter isto assincrono (tasks ou threads?)
        //Enviar resultado para os learners
        if (reply.Status)
            serverFrontends.ForEach(server =>
            {
                // (slot: int, timestampId: int, value: int)
                server.AcceptedToLearner(request.Slot, request.TimestampId, request.Value);

            });

        return Task.FromResult(reply);
    }

}
