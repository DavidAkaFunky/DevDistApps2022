using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class MultiPaxos
{
    private readonly ClientInterceptor clientInterceptor = new();
    private readonly List<GrpcChannel> multiPaxosServers = new();

    public MultiPaxos(int id)
    {
        Id = id;
    }

    public int Id { get; private set; }

    public Dictionary<int, int> History { get; } = new();

    public Dictionary<int, Slot> Slots { get; } = new();

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

    /*public void AddServer(string server)
    {
        var channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }

    public void AddOrSetSlot(int slot, Slot values)
    {
        Slots[slot] = values;
    }

    public async void RunConsensus(int slot, int inValue)
    {
        Slots.Add(slot, new Slot(inValue, Id, Id));
        List<Task> tasks = new();
        if (Id > 0)
        {
            var responses = 0;
            foreach (var channel in multiPaxosServers)
                tasks.Add(Task.Run(() =>
                {
                    PromiseReply reply = SendPrepare(channel, slot);
                    if (reply.Id > Id)
                    {
                        Id += multiPaxosServers.Count;
                        return;
                    }

                    if (reply.Id > Slots[slot].ReadTimestamp)
                    {
                        Slots[slot].ReadTimestamp = reply.Id;
                        Slots[slot].CurrentValue = reply.Value;
                    }

                    //lock (responses) doesnt work
                    CheckMajority(++responses, tasks);
                }));
        }

        foreach (var channel in multiPaxosServers)
            tasks.Add(Task.Run(() =>
            {
                if (!SendAccept(channel, slot)) Id += multiPaxosServers.Count;
            }));
    }

    public PromiseReply SendPrepare(GrpcChannel channel, int slot)
    {
        var interceptingInvoker = channel.Intercept(clientInterceptor);
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
        PrepareRequest request = new() { Slot = slot, Id = Id };
        PromiseReply reply = client.Prepare(request);
        return reply;
    }

    public bool SendAccept(GrpcChannel channel, int slot)
    {
        var interceptingInvoker = channel.Intercept(clientInterceptor);
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
        AcceptRequest request = new() { Slot = slot, Id = Slots[slot].ReadTimestamp, Value = Slots[slot].CurrentValue };
        AcceptReply reply = client.Accept(request);
        return reply.Status;
    }

    public bool SendAcceptedToLearners(int slot, int id, int timestamp)
    {
        CallInvoker interceptingInvoker = channel.Intercept(clientInterceptor);
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(interceptingInvoker);
        AcceptedToLearnerRequest request = new()
            { Slot = slot, Id = Slots[slot].ReadTimestamp, Value = Slots[slot].CurrentValue };
        AcceptedToLearnerReply reply = client.AcceptedToLearner(request);
        return reply.Status;
    }*/
}

internal class ClientInterceptor : Interceptor
{
    // private readonly ILogger logger;

    //public GlobalServerLoggerInterceptor(ILogger logger) {
    //    this.logger = logger;
    //}

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var metadata = context.Options.Headers; // read original headers
        if (metadata == null)
            metadata = new Metadata();
        metadata.Add("dad", "dad-value"); // add the additional metadata

        // create new context because original context is readonly
        ClientInterceptorContext<TRequest, TResponse> modifiedContext =
            new(context.Method, context.Host,
                new CallOptions(metadata, context.Options.Deadline,
                    context.Options.CancellationToken, context.Options.WriteOptions,
                    context.Options.PropagationToken, context.Options.Credentials));
        Console.Write("calling server...");
        var response = base.BlockingUnaryCall(request, modifiedContext, continuation);
        return response;
    }
}