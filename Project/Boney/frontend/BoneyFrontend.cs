using Grpc.Net.Client;

namespace DADProject;

public class BoneyFrontend
{
    private readonly int _clientId;
    private int _seq;
    private readonly GrpcChannel channel;

    public BoneyFrontend(int clientId, string serverAddress)
    {
        _clientId = clientId;
        _seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    // Proposer
    // TODO add metadata
    public Tuple<int, int> Prepare(int slot, int id)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        var request = new PrepareRequest
        {
            Id = id,
            Slot = slot
        };
        var reply = client.Prepare(request);
        return new Tuple<int, int>(reply.Id, reply.Value);
    }

    public void Accept(int slot, int id, int value)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        var request = new AcceptRequest()
        {
            Id = id,
            Slot = slot,
            Value = value
        };
        client.Accept(request);
    }
    
    // Bank
    public void CompareAndSwap(int slot, int value) {}
    
    // Acceptor 
    public void AcceptedToLearner(int slot, int id, int value) {}
    
    // Learner
    public void ResultToProposer(int slot, int value) {}
    
}