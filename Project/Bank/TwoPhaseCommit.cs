using Google.Protobuf.WellKnownTypes;
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
    private int majority;
    private BankAccount account;
    private List<BankToBankFrontend> bankToBankFrontends;
    private ConcurrentDictionary<int, ClientCommand> tentativeCommands = new();
    private ConcurrentDictionary<int, ClientCommand> committedCommands = new();

    public TwoPhaseCommit(int id, List<BankToBankFrontend> frontends, BankAccount account)
    {
        this.id = id;
        this.bankToBankFrontends = frontends;
        this.account = account;
        this.majority = frontends.Count / 2 + 1;
    }

    public bool AddTentative(int seq, ClientCommand cmd)
    {
        bool result = true;
        lock (tentativeCommands)
        {
            if(!tentativeCommands.TryAdd(seq, cmd))
            {
                if (tentativeCommands[seq].Slot < cmd.Slot) tentativeCommands[seq] = cmd;
                else result = false;
            }
        }

        return result;
    }

    public void AddCommitted(int seq, ClientCommand cmd)
    {
        lock (committedCommands)
        {
            // Check if the command isn't in this server yet
            if (!committedCommands.Where(kvp => kvp.Value.ClientID == cmd.ClientID && kvp.Value.ClientSeqNumber == cmd.ClientSeqNumber).Any())
            {
                RunCommand(cmd);
                committedCommands[seq] = cmd;
            }
        }
    }

    public void CleanUp2PC(int slot)
    {
        var responses = new List<ListPendingRequestsReply>();
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
                new Thread(() =>
                {
                    var reply = frontend.ListPendingTwoPCRequests(committedCommands.Keys.Max());
                    lock (responses)
                    {
                        responses.Add(reply);
                        Monitor.PulseAll(responses);
                    }

                }).Start();
            }
        });

        lock (responses)
        {
            //espera pela maioria das respostas
            while (responses.Count < majority)
            {
                Monitor.Wait(responses);
            }

            //processa respostas
            foreach (var res in responses)
            {
                foreach (var cmd in res.Commands)
                {
                    var clientCommandTuple = new Tuple<int, int>(cmd.ClientId, cmd.ClientSeqNumber);
                    if (commandsToCommit.TryGetValue(clientCommandTuple, out var sameCommand) && cmd.Slot > sameCommand.Item2.Slot)
                        commandsToCommit[clientCommandTuple] = new(cmd.GlobalSeqNumber, ClientCommand.CreateCommandFromGRPC(cmd));
                }
            }

        }


        var commandsToSend = commandsToCommit.Values.ToList();
        commandsToSend.OrderBy(cmd => cmd.Item1).ThenBy(cmd => cmd.Item2.Slot);

        foreach (var cmd in commandsToSend)
        {
            cmd.Item2.Slot = slot;
            Run(cmd.Item2, cmd.Item1);
        }
    }

    public bool Run(ClientCommand cmd)
    {
        var seqNumber = committedCommands.IsEmpty ? 0 : committedCommands.Keys.Max();
        return Run(cmd, seqNumber + 1);
    }

    protected bool Run(ClientCommand cmd, int seq)
    {
        bool result;
        var responses = new List<bool>();

        lock (tentativeCommands)
        lock(committedCommands)
        {
            
            //This should work
            tentativeCommands[seq] = cmd;

            //send tentative with seq for command(cmd)
            bankToBankFrontends.ForEach(server =>
            {
                if(server.Id != id)
                {
                    new Thread(() =>
                    {
                        var status = server.SendTwoPCTentative(cmd, seq).Status;
                        lock (responses)
                        {
                            responses.Add(status);
                            Monitor.PulseAll(responses);
                        }

                    }).Start();
                }
            });

            lock (responses)
            {
                //espera pela maioria das respostas
                while (responses.Count < majority)
                {
                    Monitor.Wait(responses);
                }

                foreach(var status in responses)
                {
                    if (!status) return false;
                }

            }

            //This should work
            committedCommands[seq] = cmd;

            result = RunCommand(cmd);

            //send commit to all replicas
            bankToBankFrontends.ForEach(server =>
            {
                if (server.Id != id)
                {
                    new Thread(() =>
                    {
                        server.SendTwoPCCommit(cmd, seq);

                    }).Start();
                }

            });
        }

        return result;
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

    public bool RunCommand(ClientCommand cmd)
    {
        if (cmd.Type == "D")
        {
            lock(account)
                account.Deposit(cmd.Amount);
            return true;
        }
        else if (cmd.Type == "W")
        {   
            lock(account)
                return account.Withdraw(cmd.Amount);
        }
        return false;
    }
}

