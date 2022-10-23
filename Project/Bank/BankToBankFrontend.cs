using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    private readonly int id;
    private int seq;
    private readonly ProjectBankTwoPCService.ProjectBankTwoPCServiceClient client;

    public BankToBankFrontend(int id, string serverAddress)
    {
        this.id = id;
        seq = 0;
        client = new(GrpcChannel.ForAddress(serverAddress));
    }

    public void ListPendingTwoPCRequests(int slot, int lastKnownSequenceNumber)
    {
        var request = new ListPendingRequestsRequest
        {
            Slot = slot,
            SenderId = id,
            GlobalSeqNumber = lastKnownSequenceNumber
        };
        client.ListPendingRequests(request);
    }

    public void SendTwoPCTentative(int slot, ClientCommand command, int tentativeSeqNumber)
    {
        var request = new TwoPCTentativeRequest
        {
            Slot = slot,
            SenderId = id,
            Command = new() 
            {
                ClientId = command.ClientID,
                ClientSeqNumber = command.ClientSeqNumber,
                Message = command.Message,
                GlobalSeqNumber = tentativeSeqNumber
            }
        };
        client.TwoPCTentative(request);
    }

    public void SendTwoPCCommit(int slot, ClientCommand command, int committedSeqNumber)
    {
        var request = new TwoPCCommitRequest
        {
            Slot = slot,
            SenderId = id,
            Command = new()
            {
                ClientId = command.ClientID,
                ClientSeqNumber = command.ClientSeqNumber,
                Message = command.Message,
                GlobalSeqNumber = committedSeqNumber
            }
        };
        client.TwoPCCommit(request);
    }

}