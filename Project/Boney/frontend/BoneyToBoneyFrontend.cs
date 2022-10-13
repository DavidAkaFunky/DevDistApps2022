using Grpc.Net.Client;

namespace DADProject;

public class BoneyToBoneyFrontend
{
    private readonly GrpcChannel channel;
    private int seq;

    public BoneyToBoneyFrontend(int clientID, string serverAddress)
    {
        ServerAddress = serverAddress;
        ClientId = clientID;
        seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    public int ClientId { get; }

    public string ServerAddress { get; set; }

    // TODO add metadata

    // Proposer
    public PromiseReply Prepare(int slot, int id)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        var request = new PrepareRequest
        {
            Slot = slot,
            TimestampId = id
        };
        var reply = client.Prepare(request);
        return reply;
    }

    public void Accept(int slot, int id, int value)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        var request = new AcceptRequest
        {
            TimestampId = id,
            Slot = slot,
            Value = value
        };
        var reply = client.Accept(request);
    }

    // Bank
    //public void RequestCompareAndSwap(int slot, int value) 
    //{
    //    var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
    //    CompareAndSwapRequest request = new() 
    //    { 
    //        Slot = slot, 
    //        InValue = value 
    //    };
    //    var reply = client.CompareAndSwap(request);
    //    Console.WriteLine("Request Delivered! Answered: {0}", reply.OutValue);
    //}

    // Acceptor 
    public void AcceptedToLearner(int slot, int id, int value)
    {
        var client = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(channel);
        var request = new AcceptedToLearnerRequest
        {
            Slot = slot,
            TimestampId = id,
            Value = value
        };

        var reply = client.AcceptedToLearner(request);
    }

    // Learner
    public void ResultToProposer(int slot, int value)
    {
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        var request = new ResultToProposerRequest
        {
            Slot = slot,
            Value = value
        };

        var reply = client.ResultToProposer(request);
    }
}