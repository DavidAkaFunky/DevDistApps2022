using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;

namespace DADProject;

public class BoneyProposer
{
    private int id;
    private List<GrpcChannel> multiPaxosServers = new(); // All servers are proposers, learners and acceptors
    // private BoneyInterceptor boneyInterceptor = new();
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

    public void CheckMajority(int responses, List<Thread> threads)
    {
        if (responses > multiPaxosServers.Count / 2)
        {
            foreach (Thread thread in threads)
            {
                thread.Abort();
            }
            threads.Clear();
        }
    }

    public async void RunConsensus(int slot, int inValue)
    {
        int valueSentToAccept = inValue;
        int mostRecentReadTimestamp = id;
        history[slot] = -1;
        List<Thread> threads = new();
        int idReadOnly = id;
        if (id > 0)
        {
            var responses = 0;
            foreach (var channel in multiPaxosServers)
            {
                Thread thread = new(() =>
                {
                    PromiseReply reply = SendPrepare(channel, slot);
                    if (reply.Id > id)
                    {
                        id = idReadOnly + multiPaxosServers.Count;
                        return; //This probably won't kill the whole function though...
                    }

                    if (reply.Id > mostRecentReadTimestamp)
                    {
                        mostRecentReadTimestamp = reply.Id;
                        valueSentToAccept = reply.Value;
                    }

                    //lock (responses) doesnt work
                    CheckMajority(++responses, threads);
                });
                threads.Add(thread);
                thread.Start();
            }
        }

        foreach (var channel in multiPaxosServers)
        {
            Thread thread = new(() =>
            {
                if (!SendAccept(channel, slot, mostRecentReadTimestamp, valueSentToAccept))
                    id = idReadOnly + multiPaxosServers.Count;
            });
            thread.Start();
        }
    }

    public PromiseReply SendPrepare(GrpcChannel channel, int slot)
    {
        // CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor);
        // var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(interceptingInvoker);
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        PrepareRequest request = new() { Slot = slot, Id = id };
        PromiseReply reply = client.Prepare(request);
        return reply;
    }

    public bool SendAccept(GrpcChannel channel, int slot, int readTimestamp, int value)
    {
        // CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor);
        // var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(interceptingInvoker);
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        AcceptRequest request = new() { Slot = slot, Id = readTimestamp, Value = value };
        AcceptReply reply = client.Accept(request);
        return reply.Status;
    }
}

