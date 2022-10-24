using System.Globalization;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private readonly int _clientId = 0;
    private readonly List<PerfectChannel> bankServers = new();

    public ClientFrontend(List<string> servers)
    {
        servers.ForEach(server => bankServers.Add(new PerfectChannel
        {
            Channel = GrpcChannel.ForAddress(server),
            ClientId = _clientId
        }));
    }

    public void CloseChannels()
    {
        var taskList = new List<Task?>();
        bankServers.ForEach(channel => taskList.Add(channel.Close()));
        taskList.ForEach(t => t?.Wait());
    }

    //TODO these methods should be changed to be async and return the first value (just the value) and the output string ...
    //TODO ... should be formatted on the Client.cs
    public void ReadBalance()
    {
        // TODO fazer alguma coisa com isto?
        var taskList = new List<Task>();

        bankServers.ForEach(channel =>
        {
            taskList.Add(
                Task.Run(() =>
                {
                    channel.SafeSend(new ReadBalanceRequest(), reply =>
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
        // TODO fazer alguma coisa com isto?
        var taskList = new List<Task>();

        bankServers.ForEach(channel =>
        {
            taskList.Add(
                Task.Run(() =>
                {
                    channel.SafeSend(new DepositRequest { Amount = amount }, reply =>
                    {
                        if (reply is not null)
                            Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
                        else
                            Console.WriteLine("Oops"); // TODO
                    });
                })
            );
        });
    }

    public void Withdraw(double amount)
    {
        // TODO fazer alguma coisa com isto?
        var taskList = new List<Task>();

        bankServers.ForEach(channel =>
        {
            taskList.Add(
                Task.Run(() =>
                {
                    channel.SafeSend(new WithdrawRequest { Amount = amount }, reply =>
                    {
                        if (reply is not null)
                            Console.WriteLine("Withdrawal of " + amount.ToString("C", CultureInfo.CurrentCulture));
                        else
                            Console.WriteLine("Oops"); // TODO
                    });
                })
            );
        });
    }
}