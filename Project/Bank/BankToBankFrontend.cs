using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    private readonly int id;
    private int seq;
    private readonly GrpcChannel channel;

    public BankToBankFrontend(int id, string serverAddress)
    {
        this.id = id;
        seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    public void SendTwoPCTentative(int slot, ClientCommand command, int tentativeSeqNumber)
    {
        var client = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(channel);
        var request = new TwoPCTentativeRequest
        {
            Slot = slot,
            ClientId = command.ClientID,
            ClientSeqNumber = command.ClientSeqNumber,
            GlobalSeqNumber = tentativeSeqNumber
        };
        client.TwoPCTentative(request);
    }

    public void SendTwoPCCommit(int slot, ClientCommand command, int committedSeqNumber)
    {
        var client = new ProjectBankTwoPCService.ProjectBankTwoPCServiceClient(channel);
        var request = new TwoPCCommitRequest
        {
            Slot = slot,
            ClientId = command.ClientID,
            ClientSeqNumber = command.ClientSeqNumber,
            Message = command.Message,
            GlobalSeqNumber = committedSeqNumber
        };
        client.TwoPCCommit(request);
    }

}