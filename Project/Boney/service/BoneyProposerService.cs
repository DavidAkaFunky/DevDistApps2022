using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private int currentSlot;
    private readonly object _ackLock = new();
    private Dictionary<int, int> _ack = new();
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
            if (!_ack.ContainsKey(request.SenderId))
                _ack[request.SenderId] = 0;
            if (request.Seq != _ack[request.SenderId] + 1)
                return Task.FromResult(new CompareAndSwapReply { Ack = _ack[request.SenderId] });
            _ack[request.SenderId] = request.Seq;
        }

        var reply = new CompareAndSwapReply { OutValue = -1, Ack = request.Seq };

        //if (isFrozen[currentSlot]) return Task.FromResult(reply);
        while (isFrozen[currentSlot]);

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