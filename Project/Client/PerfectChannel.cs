using Grpc.Net.Client;

namespace DADProject;

/*
 * 
	rpc ReadBalance(ReadBalanceRequest) returns (ReadBalanceReply);
	rpc Deposit(DepositRequest) returns (DepositReply);
	rpc Withdraw(WithdrawRequest) returns (WithdrawReply);
	rpc AcceptCompareSwapResult (CompareSwapResult) returns (CompareSwapReply);
 */

internal struct Message
{
    public int Seq { get; set; }
    public MessageType Type { get; set; }
}

public enum MessageType
{
    ReadBalance,
    Deposit,
    Withdraw,
    CompareSwapResult
}

//TODO se houver tempo fazer isto de uma forma qualquer bonitinha com genéricos
public class PerfectChannel
{
    // provavelmente há formas mais elegantes mas esta funciona
    private readonly Dictionary<int, CompareSwapResult> _compareSwapResults = new();
    private readonly Dictionary<int, DepositRequest> _depositRequests = new();
    private readonly List<Message> _messages = new();
    private readonly Dictionary<int, ReadBalanceRequest> _readBalanceRequests = new();
    private readonly Dictionary<int, WithdrawRequest> _withdrawRequests = new();
    private int _lastSeqNum;
    public GrpcChannel Channel { get; set; }

    private void UpdateLastSeq(int seq)
    {
        _lastSeqNum = seq > _lastSeqNum ? seq : _lastSeqNum;
    }

    public Task? Close()
    {
        return Channel?.ShutdownAsync();
    }

    public void SafeSend(ReadBalanceRequest request, Action<ReadBalanceReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        AddSent(request);
        handler.Invoke(stub.ReadBalance(request));
    }

    public void SafeSend(DepositRequest request, Action<DepositReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        AddSent(request);
        handler.Invoke(stub.Deposit(request));
    }

    public void SafeSend(WithdrawRequest request, Action<WithdrawReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        AddSent(request);
        handler.Invoke(stub.Withdraw(request));
    }

    public void SafeSend(CompareSwapResult request, Action<CompareSwapReply> handler)
    {
        if (Channel is null) throw new Exception("Channel está a null");
        if (request is null) throw new Exception("request está a null");

        var stub = new ProjectBankServerService.ProjectBankServerServiceClient(Channel);

        request.Seq = _lastSeqNum + 1;
        AddSent(request);
        handler.Invoke(stub.AcceptCompareSwapResult(request));
    }

    public void AddSent(ReadBalanceRequest request)
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

    public void AddSent(WithdrawRequest request)
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

    public void AddSent(CompareSwapResult request)
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

    public void AddSent(DepositRequest request)
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

    public void Retransmit(int lastReceivedAck, ProjectBankServerService.ProjectBankServerServiceClient stub)
    {
        if (lastReceivedAck == _lastSeqNum) return;

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

    public void ClearAcknowledgements(int ack)
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