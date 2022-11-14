using System.Collections.Concurrent;

namespace DADProject;

public class TwoPhaseCommit
{
    // the bank account
    private readonly BankAccount account;

    // our address
    private readonly string address;

    // the bank needs to talk to other banks
    private readonly List<BankToBankFrontend> bankToBankFrontends;

    // <sequence number, command>
    private readonly ConcurrentDictionary<int, ClientCommand> committedCommands = new();

    // system's current time slot

    // how many responses in a majority
    private readonly int majority;

    // <slot, isPrimary : bool>
    private readonly ConcurrentDictionary<int, int> primary; //  primary/backup

    // <sequence number, command>
    private readonly ConcurrentDictionary<int, ClientCommand> tentativeCommands = new();

    public TwoPhaseCommit(ConcurrentDictionary<int, int> primary, int currentSlot, string address,
        List<BankToBankFrontend> frontends, BankAccount account)
    {
        this.primary = primary;
        CurrentSlot = currentSlot;
        this.address = address;
        bankToBankFrontends = frontends;
        this.account = account;
        majority = frontends.Count / 2 + 1;
    }

    public int CurrentSlot { get; set; }

    public bool AddTentative(int seq, ClientCommand cmd)
    {
        var result = true;
        lock (tentativeCommands)
        {
            // Console.WriteLine(tentativeCommands.Count);

            if (!tentativeCommands.TryAdd(seq, cmd))
            {
                if (tentativeCommands[seq].Slot < cmd.Slot) tentativeCommands[seq] = cmd;
                else result = false;
            }

            // Console.WriteLine(tentativeCommands.Count);
        }


        return result;
    }

    public void AddCommitted(int seq, ClientCommand cmd)
    {
        lock (committedCommands)
        {
            // Check if the command isn't in this server yet
            if (!committedCommands.Where(kvp =>
                    kvp.Value.ClientID == cmd.ClientID && kvp.Value.ClientSeqNumber == cmd.ClientSeqNumber).Any())
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

        foreach (var kvp in tentativeCommands)
            commandsToCommit[new Tuple<int, int>(kvp.Value.ClientID, kvp.Value.ClientSeqNumber)] =
                new Tuple<int, ClientCommand>(kvp.Key, kvp.Value); //cria copia

        //listPendingRequests(lastKnownSequenceNumber) to all
        bankToBankFrontends.ForEach(frontend =>
        {
            if (frontend.ServerAddress != address)
                new Thread(() =>
                {
                    // possible race condition here too
                    var seqNumber = committedCommands.IsEmpty ? 0 : committedCommands.Keys.Max();
                    var reply = frontend.ListPendingTwoPCRequests(seqNumber);
                    lock (responses)
                    {
                        responses.Add(reply);
                        Monitor.PulseAll(responses);
                    }
                }).Start();
        });


        lock (responses)
        {
            //espera pela maioria das respostas
            while (responses.Count != bankToBankFrontends.Count - 1)
            {
                //PROSSEGUE, caso ja tenha uma maioria de respostas de servidores normais
                if (responses.FindAll(r => r.Status).Count + 1 >= majority)
                    break;

                Monitor.Wait(responses);
            }

            //caso uma maioria de servidores esteja frozen
            if (responses.FindAll(r => r.Status).Count + 1 < majority)
            {
                primary[CurrentSlot] = -1;
                return;
            }

            //processa respostas
            foreach (var res in responses)
            foreach (var cmd in res.Commands)
            {
                var clientCommandTuple = new Tuple<int, int>(cmd.ClientId, cmd.ClientSeqNumber);
                if (!commandsToCommit.TryGetValue(clientCommandTuple, out var sameCommand) ||
                    cmd.Slot > sameCommand.Item2.Slot)
                    commandsToCommit[clientCommandTuple] = new Tuple<int, ClientCommand>(cmd.GlobalSeqNumber,
                        ClientCommand.CreateCommandFromGRPC(cmd));
            }
        }

        var commandsToSend = commandsToCommit.Values.ToList();
        commandsToSend.OrderBy(cmd => cmd.Item1).ThenBy(cmd => cmd.Item2.Slot);

        lock (committedCommands)
        lock (tentativeCommands)
        {
            foreach (var cmd in commandsToSend)
            {
                Console.WriteLine("SENDING NEW COMMAND");
                cmd.Item2.Slot = slot;
                Run(cmd.Item2, cmd.Item1);
            }
        }
    }

    public int Run(ClientCommand cmd)
    {
        lock (committedCommands)
        lock (tentativeCommands)
        {
            var seqNumber = committedCommands.IsEmpty ? 0 : committedCommands.Keys.Max();
            return Run(cmd, seqNumber + 1);
        }
    }

    protected int Run(ClientCommand cmd, int seq)
    {
        int result;
        var responses = new List<int>();

        //This should work
        tentativeCommands[seq] = cmd;

        lock (responses)
        {
            responses.Add(1);
        }

        //send tentative with seq for command(cmd)
        bankToBankFrontends.ForEach(server =>
        {
            if (server.ServerAddress != address)
                new Thread(() =>
                {
                    var status = server.SendTwoPCTentative(cmd, seq).Status;
                    lock (responses)
                    {
                        responses.Add(status);
                        Monitor.PulseAll(responses);
                    }
                }).Start();
        });

        lock (responses)
        {
            //espera pela maioria das respostas

            while (responses.Count != bankToBankFrontends.Count)
            {
                if (responses.FindAll(x => x == 1).Count >= majority)
                    break;
                Monitor.Wait(responses);
            }

            foreach (var status in responses)
                if (status == 0)
                {
                    Console.WriteLine($"2PC response status {status}");
                    return -1;
                }


            if (responses.FindAll(x => x == 1).Count < majority)
                return 1; // It was successful because cleanup will eventually commit it!
        }

        //This should work
        committedCommands[seq] = cmd;

        result = RunCommand(cmd);

        //send commit to all replicas
        bankToBankFrontends.ForEach(server =>
        {
            if (server.ServerAddress != address)
                new Thread(() => server.SendTwoPCCommit(cmd, seq)).Start();
        });

        return result;
    }

    public ListPendingRequestsReply ListPendingRequest(int minSeq, int ack)
    {
        var reply = new ListPendingRequestsReply { Status = true, Ack = ack };

        lock (tentativeCommands)
        {
            foreach (var kvp in tentativeCommands)
                if (kvp.Key > minSeq)
                    reply.Commands.Add(kvp.Value.CreateCommandGRPC(kvp.Key));
        }

        return reply;
    }

    public int RunCommand(ClientCommand cmd)
    {
        if (cmd.Type == "D")
        {
            lock (account)
            {
                account.Deposit(cmd.Amount);
                Console.WriteLine("1-INSIDE 2PC lock");
            }

            return 1;
        }

        if (cmd.Type == "W")
            lock (account)
            {
                Console.WriteLine("2-INSIDE 2PC lock");
                return account.Withdraw(cmd.Amount) ? 1 : 0;
            }

        return 0;
    }
}