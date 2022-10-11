using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class BankFrontend
{
    private readonly List<GrpcChannel> bankServers = new();
    private readonly List<GrpcChannel> boneyServers = new();
    private readonly ClientInterceptor clientInterceptor = new();
    private readonly int serverID;

    public BankFrontend(int id, List<string> bankServers, List<string> boneyServers)
    {
        serverID = id;
        foreach (string s in bankServers)
            AddChannel(this.bankServers, s);
        foreach (string s in boneyServers)
            AddChannel(this.boneyServers, s);
    }

    public void AddChannel(List<GrpcChannel> channels, string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        channels.Add(channel);
    }

    public void DeleteServers()
    {
        foreach (var server in boneyServers)
        {
            server.ShutdownAsync().Wait();
            boneyServers.Remove(server);
        }
    }

    public void RequestCompareAndSwap(int slot)
    {
        foreach (var channel in boneyServers)
        {
            var interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectBankService.ProjectBankServiceClient(interceptingInvoker);
            Thread thread = new(() =>
            {
                CompareAndSwapRequest request = new() { Slot = slot, InValue = serverID };
                var reply = client.CompareAndSwap(request);
                Console.WriteLine("Request Delivered! Answered: {0}", reply.OutValue);
            });
            thread.Start();
        }
    }
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