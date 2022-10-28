using System.Globalization;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private readonly List<GrpcChannel> bankServers = new();
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

    public void TestSuccess(bool success)
    {
        if (!success)
            Console.Error.WriteLine("The command could not be executed. Please try again!");
    }

    public void ReadBalance()
    {
        var success = false;
        foreach (var channel in bankServers)
        {
            var client = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
            Thread thread = new(() =>
            {
                try
                {
                    ReadBalanceRequest request = new();
                    var reply = client.ReadBalance(request);
                    if (reply.Balance > -1)
                    {
                        success = true;
                        Console.WriteLine("Balance: " + reply.Balance.ToString("C", CultureInfo.CurrentCulture));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            thread.Start();
        }
        TestSuccess(success);
    }

    public void Deposit(double amount)
    {
        var success = false;
        foreach (var channel in bankServers)
        {
            var client = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
            Thread thread = new(() =>
            {
                try
                {
                    DepositRequest request = new() { Amount = amount };
                    var reply = client.Deposit(request);
                    if (reply.Status)
                    {
                        success = true;
                        Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
                    }
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            thread.Start();
        }
        TestSuccess(success);
    }

    public void Withdraw(double amount)
    {
        var success = false;
        foreach (var channel in bankServers)
        {
            var client = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
            Thread thread = new(() =>
            {
                try
                {
                    WithdrawRequest request = new() { Amount = amount };
                    var reply = client.Withdraw(request);
                    if (reply.Status < 0)
                        return;
                    if (reply.Status == 0)
                        Console.Error.WriteLine("The account's balance is not high enough");
                    if (reply.Status > 0)
                        Console.WriteLine("Withdrawal of " + amount.ToString("C", CultureInfo.CurrentCulture));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            thread.Start();
        }
        TestSuccess(success);
    }
}