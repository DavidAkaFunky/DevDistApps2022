using System.Globalization;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private readonly List<GrpcChannel> bankServers = new();
    private readonly ClientInterceptor clientInterceptor = new();

    public ClientFrontend(List<string> bankServers)
    {
        foreach (string s in bankServers)
            AddServer(s);
    }

    public void AddServer(string server)
    {
        var channel = GrpcChannel.ForAddress(server);
        bankServers.Add(channel);
    }

    public void DeleteServers()
    {
        foreach (var server in bankServers)
        {
            server.ShutdownAsync().Wait();
            bankServers.Remove(server);
        }
    }

    public void ReadBalance()
    {
        foreach (var channel in bankServers)
        {
            var interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectClientService.ProjectClientServiceClient(interceptingInvoker);
            Thread thread = new(() =>
            {
                try
                {
                    ReadBalanceRequest request = new();
                    var reply = client.ReadBalance(request);
                    Console.WriteLine("Balance: " + reply.Balance.ToString("C", CultureInfo.CurrentCulture));
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Oops"); // TODO
                }
            });
            thread.Start();
        }
    }

    public void Deposit(double amount)
    {
        foreach (var channel in bankServers)
        {
            var interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectClientService.ProjectClientServiceClient(interceptingInvoker);
            Thread thread = new(() =>
            {
                try
                {
                    DepositRequest request = new() { Amount = amount };
                    var reply = client.Deposit(request);
                    Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Oops"); // TODO
                }
            });
            thread.Start();
        }
    }

    public void Withdraw(double amount)
    {
        foreach (var channel in bankServers)
        {
            var interceptingInvoker = channel.Intercept(clientInterceptor);
            var client = new ProjectClientService.ProjectClientServiceClient(interceptingInvoker);
            Thread thread = new(() =>
            {
                try
                {
                    WithdrawRequest request = new() { Amount = amount };
                    var reply = client.Withdraw(request);
                    Console.WriteLine("Withdrawal of " + amount.ToString("C", CultureInfo.CurrentCulture));
                    return;
                }
                catch (Exception)
                {
                    Console.WriteLine("Oops"); // TODO
                }
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
        if (metadata == null) metadata = new Metadata();
        metadata.Add("dad", "dad-value"); // add the additional metadata

        // create new context because original context is readonly
        var modifiedContext =
            new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                new CallOptions(metadata, context.Options.Deadline,
                    context.Options.CancellationToken, context.Options.WriteOptions,
                    context.Options.PropagationToken, context.Options.Credentials));
        Console.Write("calling server...");
        var response = base.BlockingUnaryCall(request, modifiedContext, continuation);
        return response;
    }
}