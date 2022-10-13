using System.Collections.Concurrent;
using Grpc.Core;

namespace DADProject;

public class BoneyProposerService : ProjectBoneyProposerService.ProjectBoneyProposerServiceBase
{
    private readonly int id;
    private readonly ConcurrentDictionary<int, int> slotsHistory;
    private readonly ConcurrentDictionary<int, Slot> slotsInfo;

    public BoneyProposerService(int id, ConcurrentDictionary<int, int> slotsHistory, ConcurrentDictionary<int, Slot> slotsInfo)
    {
        this.id = id;
        this.slotsHistory = slotsHistory;
        this.slotsInfo = slotsInfo;
    }

    public override Task<CompareAndSwapReply> CompareAndSwap(CompareAndSwapRequest request, ServerCallContext context)
    {
        var reply = new CompareAndSwapReply();

        lock (slotsHistory)
        {
            // verificar se slot ja foi decidido, se sim, retorna
            reply.OutValue = slotsHistory.GetValueOrDefault(request.Slot, 0);

            if (reply.OutValue != 0) return Task.FromResult(reply);
        }
            //slotsHistory[request.Slot] = -1; //caso ja tenha começado consensus
        lock (slotsInfo)
        {
            //Verificar se ja tem mensagem para propor
            slotsInfo.TryAdd(request.Slot, new Slot(request.InValue, 0, 0));
        }

        return Task.FromResult(reply);
    }
}