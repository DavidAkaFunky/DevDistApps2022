using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankFrontend
{
    private readonly int _clientId;
    private int _seq;
    private readonly GrpcChannel channel;

    public BankFrontend(int clientId, string serverAddress)
    {
        _clientId = clientId;
        _seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    //Clients
    public async void ReadBalance()
    {
       //Metadata metadata = new();
       // metadata.SenderId = _clientId;
       // metadata.Seq = _seq++;
       // metadata.Ack = -1;

    }

    public void Deposit(double amount)
    {
        //Metadata metadata = new();
        //metadata.SenderId = _clientId;
        //metadata.Seq = _seq++;
        //metadata.Ack = -1;
    }

    public void Withdraw(double amount)
    {
        //Metadata metadata = new();
        //metadata.SenderId = _clientId;
        //metadata.Seq = _seq++;
        //metadata.Ack = -1;
    }


    //Boney
    public void SendCompareSwapResult(int slot, int value)
    {
        //Metadata metadata = new();
        //metadata.SenderId = _clientId;
        //metadata.Seq = _seq++;
        //metadata.Ack = -1;

        var client = new ProjectBankService.ProjectBankServiceClient(channel);
        CompareSwapResult request = new()
        {
            Slot = slot,
            Value = value
        };
        var reply = client.AcceptCompareSwapResult(request);
    }
}