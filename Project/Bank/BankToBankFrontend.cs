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

    public int Id
    {
        get { return id; }
    }

    public ListPendingRequestsReply ListPendingTwoPCRequests(int lastKnownSequenceNumber)
    {
        var request = new ListPendingRequestsRequest
        {
            SenderId = id,
            GlobalSeqNumber = lastKnownSequenceNumber
        };
        return client.ListPendingRequests(request);
    }

    public void SendTwoPCTentative(ClientCommand command, int tentativeSeqNumber)
    {
        var request = new TwoPCTentativeRequest
        {
            SenderId = id,
            Command = command.CreateCommandGRPC(tentativeSeqNumber)
        };
        client.TwoPCTentative(request);
    }

    public void SendTwoPCCommit(ClientCommand command, int committedSeqNumber)
    {
        var request = new TwoPCCommitRequest
        {
            SenderId = id,
            Command = command.CreateCommandGRPC(committedSeqNumber)
        };
        client.TwoPCCommit(request);
    }

}