using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyProposer
{
    private int id;
    private List<GrpcChannel> multiPaxosServers = new(); // All servers are proposers, learners and acceptors
    private BoneyInterceptor boneyInterceptor = new();
    private Dictionary<int, int> history = new();

    public BoneyProposer(int id, List<string> servers)
    {
        this.id = id;
        foreach (string s in servers)
            AddServer(s);
    }

    public int Id
    {
        get { return id; }
    }

    public Dictionary<int, int> History
    {
        get { return history; }
    }

    public void AddServer(string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }

    public void AddOrSetHistory(int slot, int result)
    {
        history[slot] = result;
    }

    public void CheckMajority(int responses, List<Task> tasks)
    {
        if (responses == multiPaxosServers.Count)
            // foreach(Task task in tasks)
            // {
            //     if(!task.IsCompleted)
            //         // TODO
            // }
            tasks.Clear();
    }

    public async void RunConsensus(int slot, int inValue)
    {
        int valueSentToAccept = inValue;
        int mostRecentReadTimestamp = id;
        history[slot] = -1;
        List<Task> tasks = new();
        if (id > 0)
        {
            var responses = 0;
            foreach (var channel in multiPaxosServers)
                tasks.Add(Task.Run(() =>
                {
                    PromiseReply reply = SendPrepare(channel, slot);
                    if (reply.Id > id)
                    {
                        id += multiPaxosServers.Count;
                        return; //This probably won't kill the whole function though...
                    }

                    if (reply.Id > mostRecentReadTimestamp)
                    {
                        mostRecentReadTimestamp = reply.Id;
                        valueSentToAccept = reply.Value;
                    }

                    //lock (responses) doesnt work
                    CheckMajority(++responses, tasks);
                }));
        }

        foreach (var channel in multiPaxosServers)
            tasks.Add(Task.Run(() =>
            {
                if (!SendAccept(channel, slot, mostRecentReadTimestamp, valueSentToAccept))
                    id += multiPaxosServers.Count;
            }));
    }

    public PromiseReply SendPrepare(GrpcChannel channel, int slot)
    {
        CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor);
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(interceptingInvoker);
        PrepareRequest request = new() { Slot = slot, Id = id };
        PromiseReply reply = client.Prepare(request);
        return reply;
    }

    public bool SendAccept(GrpcChannel channel, int slot, int readTimestamp, int value)
    {
        CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor);
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(interceptingInvoker);
        AcceptRequest request = new() { Slot = slot, Id = readTimestamp, Value = value };
        AcceptReply reply = client.Accept(request);
        return reply.Status;
    }
}

