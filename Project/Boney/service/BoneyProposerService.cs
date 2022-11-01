﻿using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private int currentSlot;
    private readonly object _ackLock = new();
    private int _ack = 0;
    private readonly ConcurrentDictionary<int, int> slotsHistory;
    private readonly ConcurrentDictionary<int, Slot> slotsInfo;
    private readonly Dictionary<int, bool> isFrozen;

    public BoneyProposerService(ConcurrentDictionary<int, int> slotsHistory, ConcurrentDictionary<int, Slot> slotsInfo, Dictionary<int, bool> isFrozen, int currentSlot)
    {
        this.slotsHistory = slotsHistory;
        this.slotsInfo = slotsInfo;
        this.isFrozen = isFrozen;
        this.currentSlot = currentSlot;
    }

    public int CurrentSlot
    {
        get { return currentSlot; }
        set { currentSlot = value; }
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        lock (_ackLock)
        {
            if (request.Seq != _ack + 1)
                return Task.FromResult(new CompareAndSwapReply { OutValue = -1, Ack = _ack });
            _ack = request.Seq;
        }

        var reply = new CompareAndSwapReply { OutValue = -1, Ack = request.Seq };

        if (isFrozen[currentSlot]) return Task.FromResult(reply);

        lock (slotsHistory)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(request.Slot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);
        }

        lock (slotsInfo)
        {
            //Verificar se ja tem mensagem para propor
            slotsInfo.TryAdd(request.Slot, new Slot(request.InValue, 0, 0));
        }

        return Task.FromResult(reply);
    }
}