using System.Globalization;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private readonly List<GrpcChannel> _bankServers = new();
    private readonly List<PerfectChannel> bankServers = new();
    private readonly int clientId = 0;

    public ClientFrontend(List<string> servers)
    {
        servers.ForEach(server => bankServers.Add(new PerfectChannel
        {
            Channel = GrpcChannel.ForAddress(server)
        }));
    }

    public void CloseChannels()
    {
        var taskList = new List<Task?>();
        bankServers.ForEach(channel => taskList.Add(channel.Close()));
        taskList.ForEach(t => t?.Wait());
    }

    public void ReadBalance()
    {
        // TODO fazer alguma coisa com isto?
        var taskList = new List<Task>();

        bankServers.ForEach(channel =>
        {
            taskList.Add(
                Task.Run(() =>
                {
                    //seq is set inside SafeSend, the others should probably be too (for code consistency)
                    channel.SafeSend(new ReadBalanceRequest
                    {
                        SenderId = clientId,
                        Ack = 0
                    }, reply =>
                    {
                        if (reply is not null)
                            Console.WriteLine("Balance: " + reply.Balance.ToString("C", CultureInfo.CurrentCulture));
                        else
                            Console.WriteLine("Oops"); // TODO
                    });
                })
            );
        });
    }

    public void Deposit(double amount)
    {
        foreach (var channel in _bankServers)
        {
            var client = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
            Thread thread = new(() =>
            {
                try
                {
                    DepositRequest request = new() { Amount = amount };
                    var reply = client.Deposit(request);
                    Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
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
        foreach (var channel in _bankServers)
        {
            var client = new ProjectBankServerService.ProjectBankServerServiceClient(channel);
            Thread thread = new(() =>
            {
                try
                {
                    WithdrawRequest request = new() { Amount = amount };
                    var reply = client.Withdraw(request);
                    Console.WriteLine("Withdrawal of " + amount.ToString("C", CultureInfo.CurrentCulture));
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