using System.Globalization;
using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private static readonly int TIMEOUT = 100;
    private readonly List<Sender> _bankServers = new();

    public ClientFrontend(List<string> bankServers)
    {
        bankServers.ForEach(s => _bankServers.Add(new Sender(GrpcChannel.ForAddress(s), TIMEOUT)));
    }

    public void AddServer(string server)
    {
        _bankServers.Add(new Sender(GrpcChannel.ForAddress(server), TIMEOUT));
    }

    public void DeleteServers()
    {
        _bankServers.ForEach(s => s.Close());
    }

    public void ReadBalance()
    {
        AutoResetEvent gotResult = new(false);
        var finished = false;
        object finishedLock = new();
        var responseCount = 0;
        _bankServers.ForEach(s =>
        {
            //TODO maybe change to read majority and return the newest value
            s.Send(new ReadBalanceRequest()).ContinueWith(task =>
            {
                lock (finishedLock)
                {
                    responseCount++;

                    if (!finished && task.Result.Balance > 0)
                    {
                        Console.WriteLine("Balance: " + task.Result.Balance.ToString("C", CultureInfo.CurrentCulture));
                        finished = true;
                    }

                    gotResult.Set();
                }
            });
        });

        while (true)
        {
            var cond = false;
            lock (finishedLock)
            {
                cond = !finished && responseCount != _bankServers.Count;
            }

            if (!cond)
                gotResult.WaitOne();
            else
                break;
        }
    }


    public void Deposit(double amount)
    {
        AutoResetEvent gotResult = new(false);
        var finished = false;
        object finishedLock = new();
        var responseCount = 0;
        _bankServers.ForEach(s =>
        {
            //TODO maybe change to read majority and return the newest value
            s.Send(new DepositRequest { Amount = amount }).ContinueWith(task =>
            {
                lock (finishedLock)
                {
                    responseCount++;

                    if (!finished && !task.Result.Status)
                    {
                        Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
                        finished = true;
                    }

                    gotResult.Set();
                }
            });
        });

        while (true)
        {
            var cond = false;
            lock (finishedLock)
            {
                cond = !finished && responseCount != _bankServers.Count;
            }

            if (!cond)
                gotResult.WaitOne();
            else
                break;
        }
    }


    public void Withdraw(double amount)
    {
        AutoResetEvent gotResult = new(false);
        var finished = false;
        object finishedLock = new();
        var responseCount = 0;
        _bankServers.ForEach(s =>
        {
            //TODO maybe change to read majority and return the newest value
            s.Send(new WithdrawRequest { Amount = amount }).ContinueWith(task =>
            {
                lock (finishedLock)
                {
                    responseCount++;

                    if (!finished)
                    {
                        switch (task.Result.Status)
                        {
                            case > 0:
                                Console.WriteLine("Deposit of " + amount.ToString("C", CultureInfo.CurrentCulture));
                                break;
                            case 0:
                                Console.Error.WriteLine("The account's balance is not high enough");
                                break;
                        }

                        finished = true;
                    }

                    gotResult.Set();
                }
            });
        });

        while (true)
        {
            var cond = false;
            lock (finishedLock)
            {
                cond = !finished && responseCount != _bankServers.Count;
            }

            if (!cond)
                gotResult.WaitOne();
            else
                break;
        }
    }
}

public class Sender
{
    private readonly GrpcChannel _channel;
    private readonly Random _random = new();
    private readonly Mutex _seqLock = new();
    private readonly int _timeout;
    private int _currentSeq = 1;

    public Sender(GrpcChannel channel, int timeout)
    {
        _channel = channel;
        _timeout = timeout;
    }

    public Task<ReadBalanceReply> Send(ReadBalanceRequest req)
    {
        var t = new Task<ReadBalanceReply>(() =>
        {
            var stub = new ProjectBankServerService.ProjectBankServerServiceClient(_channel);
            ReadBalanceReply? reply = null;
            lock (_seqLock)
            {
                req.Seq = _currentSeq++;
            }

            while (true)
            {
                try
                {
                    reply = stub.ReadBalance(req, null,
                        DateTime.Now.AddMilliseconds(_timeout + _random.Next() % _timeout));
                }
                catch (RpcException)
                {
                    reply = null;
                    continue;
                }

                if (reply.Ack <= req.Seq)
                    continue;
                break;
            }

            return reply;
        });
        t.Start();
        return t;
    }

    public Task<DepositReply> Send(DepositRequest req)
    {
        var t = new Task<DepositReply>(() =>
        {
            var stub = new ProjectBankServerService.ProjectBankServerServiceClient(_channel);
            DepositReply? reply = null;
            lock (_seqLock)
            {
                req.Seq = _currentSeq++;
            }

            while (true)
            {
                try
                {
                    reply = stub.Deposit(req, null,
                        DateTime.Now.AddMilliseconds(_timeout + _random.Next() % _timeout));
                }
                catch (RpcException)
                {
                    reply = null;
                    continue;
                }

                if (reply.Ack <= req.Seq)
                    continue;
                break;
            }

            return reply;
        });
        t.Start();
        return t;
    }

    public Task<WithdrawReply> Send(WithdrawRequest req)
    {
        var t = new Task<WithdrawReply>(() =>
        {
            var stub = new ProjectBankServerService.ProjectBankServerServiceClient(_channel);
            WithdrawReply? reply = null;
            lock (_seqLock)
            {
                req.Seq = _currentSeq++;
            }

            while (true)
            {
                try
                {
                    reply = stub.Withdraw(req, null,
                        DateTime.Now.AddMilliseconds(_timeout + _random.Next() % _timeout));
                }
                catch (RpcException)
                {
                    reply = null;
                    continue;
                }

                if (reply.Ack <= req.Seq)
                    continue;
                break;
            }

            return reply;
        });
        t.Start();
        return t;
    }

    public void Close()
    {
        _channel.ShutdownAsync().Wait();
    }
}