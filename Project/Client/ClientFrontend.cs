using System.Globalization;
using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class ClientFrontend
{
    private static readonly int TIMEOUT = 1000;
    private readonly List<Sender> _bankServers = new();

    public ClientFrontend(List<string> bankServers, int senderID)
    {
        bankServers.ForEach(s => _bankServers.Add(new Sender(GrpcChannel.ForAddress(s), TIMEOUT, senderID)));
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
                cond = finished || responseCount == _bankServers.Count;
            }
            if (cond)
                break;
            gotResult.WaitOne();
        }

        if (!finished)
            Console.WriteLine("The command couldn't be processed. Please try again!");
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
                cond = finished || responseCount == _bankServers.Count;
            }
            if (cond)
                break;
            gotResult.WaitOne();
        }

        if (!finished)
            Console.WriteLine("The command couldn't be processed. Please try again!");
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
                                Console.WriteLine("Withdrawal of " + amount.ToString("C", CultureInfo.CurrentCulture));
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
                cond = finished || responseCount == _bankServers.Count;
            }

            if (cond)
                break;
            gotResult.WaitOne();
        }

        if (!finished)
            Console.WriteLine("The command couldn't be processed. Please try again!");
    }

    private class Sender
    {
        private readonly GrpcChannel _channel;
        private readonly Random _random = new();
        private readonly Mutex _seqLock = new();
        private readonly int _timeout;
        private int _currentSeq = 1;
        private int _senderID;

        public Sender(GrpcChannel channel, int timeout, int senderID)
        {
            _channel = channel;
            _timeout = timeout;
            _senderID = senderID;
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
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.ReadBalance(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
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
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.Deposit(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
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
                req.SenderId = _senderID;
                while (true)
                {
                    try
                    {
                        reply = stub.Withdraw(req);
                    }
                    catch (RpcException)
                    {
                        reply = null;
                        continue;
                    }
                    if (reply.Ack >= req.Seq)
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
}