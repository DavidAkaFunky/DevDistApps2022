using Grpc.Net.Client;

namespace DADProject;

public class BoneyToBoneyFrontend
{
    private readonly ProjectBoneyProposerService.ProjectBoneyProposerServiceClient proposerClient;
    private readonly ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient acceptorClient;
    private readonly ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient learnerClient;
    private int seq;

    public BoneyToBoneyFrontend(int clientID, string serverAddress)
    {
        ServerAddress = serverAddress;
        ClientId = clientID;
        seq = 0;
        var channel = GrpcChannel.ForAddress(serverAddress);
        proposerClient = new(channel);
        acceptorClient = new(channel);
        learnerClient = new(channel);
    }

    public int ClientId { get; }

    public string ServerAddress { get; set; }

    // TODO add metadata

    // Proposer
    public PromiseReply Prepare(int slot, int id)
    {
        var request = new PrepareRequest
        {
            Slot = slot,
            TimestampId = id
        };
        return acceptorClient.Prepare(request);
    }

    public bool Accept(int slot, int id, int value)
    {
        var request = new AcceptRequest
        {
            TimestampId = id,
            Slot = slot,
            Value = value
        };
        return acceptorClient.Accept(request).Status;
    }

    // Acceptor 
    public void AcceptedToLearner(int slot, int id, int value)
    {
        var request = new AcceptedToLearnerRequest
        {
            Slot = slot,
            TimestampId = id,
            Value = value
        };
        learnerClient.AcceptedToLearner(request);
    }

    // Learner
    public void ResultToProposer(int slot, int value)
    {
        var request = new ResultToProposerRequest
        {
            Slot = slot,
            Value = value
        };
        proposerClient.ResultToProposer(request);
    }
}