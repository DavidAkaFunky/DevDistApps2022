using Grpc.Net.Client;

namespace DADProject.Client.PerfectChannel;

/*
 * 
	rpc ReadBalance(ReadBalanceRequest) returns (ReadBalanceReply);
	rpc Deposit(DepositRequest) returns (DepositReply);
	rpc Withdraw(WithdrawRequest) returns (WithdrawReply);
	rpc AcceptCompareSwapResult (CompareSwapResult) returns (CompareSwapReply);
 */

internal struct Message
{
    public int Seq { get; init; }
    public MessageType Type { get; init; }
}

internal enum MessageType
{
    ReadBalance,
    Deposit,
    Withdraw,
    CompareSwapResult
}

internal class Lock
{
}

//TODO se houver tempo fazer isto de uma forma qualquer bonitinha com genéricos
public class Client
{
    private const int Timeout = 10; // in ms

    // provavelmente há formas mais elegantes mas esta funciona
    private readonly Dictionary<int, CompareSwapResult> _compareSwapResults = new();
    private readonly Dictionary<int, DepositRequest> _depositRequests = new();
    private readonly List<Message> _messages = new();
    private readonly Dictionary<int, ReadBalanceRequest> _readBalanceRequests = new();
    private readonly Lock _seqLock = new();
    private readonly Dictionary<int, WithdrawRequest> _withdrawRequests = new();
    private int _lastSeqNum = 1;
    public GrpcChannel Channel { get; init; } = null!;
    public int ClientId { get; init; }

    public Task? Close()
    {
        return Channel.ShutdownAsync();
    }

    private int GetNewSeq()
    {
        var seq = 0;
        lock (_seqLock)
        {
            seq = _lastSeqNum++;
        }

        return seq;
    }

    private Task<ReadBalanceReply> ReliableSend(ReadBalanceRequest request)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        request.Seq = GetNewSeq();
        request.SenderId = ClientId;
        request.Ack = 0;
        AddSent(request);

        return new Task<ReadBalanceReply>(() =>
        {
            ReadBalanceReply reply = null!;
            var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);
            do
            {
                reply = stub.ReadBalance(request, null,
                    DateTime.Now.AddMilliseconds(Timeout + new Random().Next(0, Timeout)));
            } while (reply is null);

            return reply;
        });
    }

    public async void Send(ReadBalanceRequest request)
    {
        var reply = await ReliableSend(request);

        // TODO para amanha
        // a reply é uma reply qualquer, nao implica que seja a reply a esta mensagem
        // ou seja, tem de se verificar ack para ver o que se faz
        // podemos forcar ou ter uma especie de timer que é reiniciado sempre que um ack é feito
        // (e que não corre se ack == ultimo seq enviado)
        // se timer expirar mandamos tudo desde (ACK -> SEQ) para lá
        // diria para nestes reenvios mandar tudo com o Reliable Send (se calhar mandar sequencial, facilita
        // mas com ciclo infinito, mensagens futuras podem estar em cache no servidor mas ele não avanca sem receber as
        // que faltam, penso que o ciclo infinito nao fara mal
    }


    public void SafeSend(DepositRequest request, Action<DepositReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        request.SenderId = ClientId;
        request.Ack = 0;
        AddSent(request);
        handler.Invoke(stub.Deposit(request));
    }

    public void SafeSend(WithdrawRequest request, Action<WithdrawReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        request.SenderId = ClientId;
        request.Ack = 0;
        AddSent(request);
        handler.Invoke(stub.Withdraw(request));
    }

    public void SafeSend(CompareSwapResult request, Action<CompareSwapReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        request.SenderId = ClientId;
        request.Ack = 0;
        AddSent(request);
        handler.Invoke(stub.AcceptCompareSwapResult(request));
    }

    private void AddSent(ReadBalanceRequest request)
    {
        if (_messages.Count == 0 || request.Seq == _messages.Last().Seq - 1)
        {
            UpdateLastSeq(request.Seq);
            _messages.Add(new Message
            {
                Seq = request.Seq,
                Type = MessageType.ReadBalance
            });
            _readBalanceRequests.Add(request.Seq, request);
        }
        else
        {
            throw new Exception("isto não é suposto acontecer");
        }
    }

    private void AddSent(WithdrawRequest request)
    {
        if (_messages.Count == 0 || request.Seq == _messages.Last().Seq - 1)
        {
            UpdateLastSeq(request.Seq);
            _messages.Add(new Message
            {
                Seq = request.Seq,
                Type = MessageType.Withdraw
            });
            _withdrawRequests.Add(request.Seq, request);
        }
        else
        {
            throw new Exception("isto não é suposto acontecer");
        }
    }

    private void AddSent(CompareSwapResult request)
    {
        if (_messages.Count == 0 || request.Seq == _messages.Last().Seq - 1)
        {
            UpdateLastSeq(request.Seq);
            _messages.Add(new Message
            {
                Seq = request.Seq,
                Type = MessageType.CompareSwapResult
            });
            _compareSwapResults.Add(request.Seq, request);
        }
        else
        {
            throw new Exception("isto não é suposto acontecer");
        }
    }

    private void AddSent(DepositRequest request)
    {
        if (_messages.Count == 0 || request.Seq == _messages.Last().Seq - 1)
        {
            UpdateLastSeq(request.Seq);
            _messages.Add(new Message
            {
                Seq = request.Seq,
                Type = MessageType.Deposit
            });
            _depositRequests.Add(request.Seq, request);
        }
        else
        {
            throw new Exception("isto não é suposto acontecer");
        }
    }

    public void Retransmit(int lastReceivedAck)
    {
        if (lastReceivedAck == _lastSeqNum) return;

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        var taskList = new List<Task>();
        _messages.ForEach(msg =>
        {
            // already acked, this shouldn't happen as well but just to be sure
            if (msg.Seq <= lastReceivedAck) return;

            // em principio nao precisa de locks, assumindo que os acks do outro lado sao cumulativos,
            // mesmo que hajam race conditions, no máximo há acumulação de mensagens deste lado que já foram acked
            taskList.Add(Task.Run(() =>
            {
                switch (msg.Type)
                {
                    case MessageType.ReadBalance:
                    {
                        var response = stub.ReadBalance(_readBalanceRequests.GetValueOrDefault(msg.Seq));
                        ClearAcknowledgements(response.Ack);
                    }
                        break;
                    case MessageType.Withdraw:
                    {
                        var response = stub.Withdraw(_withdrawRequests.GetValueOrDefault(msg.Seq));
                        ClearAcknowledgements(response.Ack);
                    }
                        break;
                    case MessageType.Deposit:
                    {
                        var response = stub.Deposit(_depositRequests.GetValueOrDefault(msg.Seq));
                        ClearAcknowledgements(response.Ack);
                    }
                        break;
                    case MessageType.CompareSwapResult:
                    {
                        var response =
                            stub.AcceptCompareSwapResult(_compareSwapResults.GetValueOrDefault(msg.Seq));
                        ClearAcknowledgements(response.Ack);
                    }
                        break;
                }
            }));
        });

        //TODO fazer qualquer coisa com a lista de tarefas?
    }

    private void ClearAcknowledgements(int ack)
    {
        // should only remove from the beginning of the list
        // podem se fazer umas pequenas otimizacoes a brincar com indices (assumindo tambem que a lista e sempre contigua)
        // mas assim fica mais legivel
        // aqui sim, provavelmente uma lock
        lock (_messages)
        {
            _messages.ForEach(msg =>
            {
                if (msg.Seq > ack) return;
                switch (msg.Type)
                {
                    case MessageType.ReadBalance:
                        _readBalanceRequests.Remove(msg.Seq);
                        break;
                    case MessageType.Withdraw:
                        _withdrawRequests.Remove(msg.Seq);
                        break;
                    case MessageType.Deposit:
                        _depositRequests.Remove(msg.Seq);
                        break;
                    case MessageType.CompareSwapResult:
                        _compareSwapResults.Remove(msg.Seq);
                        break;
                }
            });
            _messages.RemoveAll(msg => msg.Seq <= ack);
        }
    }
}