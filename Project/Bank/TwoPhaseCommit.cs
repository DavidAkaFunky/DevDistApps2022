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
        Dictionary<int, ClientCommand> commandsToCommit = new();

        foreach(var kvp in tentativeCommands)
        {
            commandsToCommit[kvp.Key] = new(kvp.Value);//cria copia
        }

        //listPendingRequests(lastKnownSequenceNumber) to all
        bankToBankFrontends.ForEach(frontend =>
        {
            if (frontend.Id != id)
            {
                var reply = frontend.ListPendingTwoPCRequests(committedCommands.Keys.Max());

                foreach (var cmd in reply.Commands)
                {
                    //Remove duplicates and prefer the most recent tentative version 
                    if (!commandsToCommit.ContainsKey(cmd.GlobalSeqNumber))
                    {
                        commandsToCommit.Add(
                            cmd.GlobalSeqNumber,
                            new(cmd.Slot,
                                cmd.ClientId,
                                cmd.ClientSeqNumber,
                                cmd.Type,
                                cmd.Amount));
                    }
                    else
                    {
                        if (commandsToCommit[cmd.GlobalSeqNumber].Slot < cmd.Slot)
                        {
                            commandsToCommit[cmd.GlobalSeqNumber] = new(cmd.Slot, cmd.ClientId, cmd.ClientSeqNumber, cmd.Type, cmd.Amount);
                        }

                    }
                }
                
            }
        });

        var Keys = commandsToCommit.Keys.ToList();
        Keys.Sort();

        foreach (int seq in Keys)
        {
            commandsToCommit[seq].Slot = slot;
            Run(commandsToCommit[seq], seq);
        }

        //TODO: wait for majority 
    }

    public void Run(ClientCommand cmd)
    {
        Run(cmd, committedCommands.Keys.Max() + 1);
    }

    protected void Run(ClientCommand cmd, int seq)
    {
        lock (tentativeCommands)
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
                    //tentativeCommands.TryRemove(kvp.Key, out var value);//cant remove things
                    reply.Commands.Add(kvp.Value.CreateCommandGRPC(kvp.Key));
                }
            }
        }

        return reply;
    }
}

