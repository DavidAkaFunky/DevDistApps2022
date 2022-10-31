using Grpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADProject;

public class TwoPhaseCommit
{
    private int id;
    private List<BankToBankFrontend> bankToBankFrontends;
    private ConcurrentDictionary<int, ClientCommand> tentativeCommands = new();
    private ConcurrentDictionary<int, ClientCommand> committedCommands = new();

    public TwoPhaseCommit(int id, List<BankToBankFrontend> frontends)
    {
        this.id = id;
        this.bankToBankFrontends = frontends;
    }

    public bool AddTentative(int seq, ClientCommand cc)
    {
        bool result = true;
        lock (tentativeCommands)
        {
            if(!tentativeCommands.TryAdd(seq, cc))
            {
                if (tentativeCommands[seq].Slot < cc.Slot) tentativeCommands[seq] = cc;
                else result = false;
            }
        }

        return result;
    }

    public void AddCommitted(int seq, ClientCommand cc)
    {
        
        lock (committedCommands)
        {
            committedCommands[seq] = cc;
        }

        //if (tentativeCommands.TryGetValue(seq, out var value) && value.ClientID == cc.ClientID && value.ClientSeqNumber == cc.ClientSeqNumber)
        //{
        //    committedCommands[seq] = cc;
        //    result = true;
        //}

        // Remove message from tentative commands -> shouldnt remove
        //var item = tentativeCommands.First(kvp => kvp.Value.ClientID == cc.ClientID && kvp.Value.ClientSeqNumber == cc.ClientSeqNumber);
    }

    public void CleanUp2PC(int slot)
    {
        var commandsToCommit = new Dictionary<Tuple<int, int>, Tuple<int, ClientCommand>>();

        foreach(var kvp in tentativeCommands)
        {
            commandsToCommit[new(kvp.Value.ClientID, kvp.Value.ClientSeqNumber)] = new(kvp.Key, kvp.Value); //cria copia
        }

        //listPendingRequests(lastKnownSequenceNumber) to all
        bankToBankFrontends.ForEach(frontend =>
        {
            if (frontend.Id != id)
            {
                var reply = frontend.ListPendingTwoPCRequests(committedCommands.Keys.Max());

                foreach (var cmd in reply.Commands)
                {
                    var clientCommandTuple = new Tuple<int, int>(cmd.ClientId, cmd.ClientSeqNumber);
                    if (commandsToCommit.TryGetValue(clientCommandTuple, out var sameCommand) && cmd.Slot > sameCommand.Item2.Slot)
                        commandsToCommit[clientCommandTuple] = new(cmd.GlobalSeqNumber, ClientCommand.CreateCommandFromGRPC(cmd));
                }
            }
        });

        //TODO: wait for majority 

        var commandsToSend = commandsToCommit.Values.ToList();
        commandsToSend.OrderBy(cmd => cmd.Item1).ThenBy(cmd => cmd.Item2.Slot);

        foreach (var cmd in commandsToSend)
        {
            cmd.Item2.Slot = slot;
            Run(cmd.Item2, cmd.Item1);
        }

        //TODO: wait for majority 

    }

    public void Run(ClientCommand cmd)
    {
        var seqNumber = committedCommands.IsEmpty ? 0 : committedCommands.Keys.Max();
        Run(cmd, seqNumber + 1);
    }

    protected void Run(ClientCommand cmd, int seq)
    {
        lock(tentativeCommands)
        lock(committedCommands)
        {
            
            //This should work
            tentativeCommands[seq] = cmd;

            //send tentative with seq for command(cmd)
            bankToBankFrontends.ForEach(server =>
            {
                if(server.Id != id) server.SendTwoPCTentative(cmd, seq);

            });

            //TODO: wait for acknowledgement of majority

            //This should work
            committedCommands[seq] = cmd;

            //send commit to all replicas
            bankToBankFrontends.ForEach(server =>
            {
                if(server.Id != id)  server.SendTwoPCCommit(cmd, seq);

            });
        }
        
        //TODO: wait for acknowledgement of majority ???????????
    }

    public ListPendingRequestsReply ListPendingRequest(int minSeq)
    {
        var reply = new ListPendingRequestsReply();

        lock (tentativeCommands)
        {
            foreach (var kvp in tentativeCommands)
            {
                if (kvp.Key > minSeq)
                {
                    reply.Commands.Add(kvp.Value.CreateCommandGRPC(kvp.Key));
                }
            }
        }

        return reply;
    }
}

