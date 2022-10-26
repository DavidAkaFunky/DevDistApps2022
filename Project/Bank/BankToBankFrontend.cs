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

    public ListPendingRequestsReply ListPendingTwoPCRequests(int slot, int lastKnownSequenceNumber)
    {
        var request = new ListPendingRequestsRequest
        {
            Slot = slot,
            SenderId = id,
            GlobalSeqNumber = lastKnownSequenceNumber
        };
        return client.ListPendingRequests(request);
    }

    public void SendTwoPCTentative(int slot, ClientCommand command, int tentativeSeqNumber)
    {
        var request = new TwoPCTentativeRequest
        {
            Slot = slot,
            SenderId = id,
            Command = command.CreateCommandGRPC(tentativeSeqNumber)
        };
        client.TwoPCTentative(request);
    }

    public void SendTwoPCCommit(int slot, ClientCommand command, int committedSeqNumber)
    {
        var request = new TwoPCCommitRequest
        {
            Slot = slot,
            SenderId = id,
            Command = command.CreateCommandGRPC(committedSeqNumber)
        };
        client.TwoPCCommit(request);
    }

}