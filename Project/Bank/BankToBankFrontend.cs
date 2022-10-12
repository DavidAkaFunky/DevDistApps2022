using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    private readonly int clientID;
    private int seq;
    private readonly GrpcChannel channel;

    public BankToBankFrontend(int clientID, string serverAddress)
    {
        this.clientID = clientID;
        seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    // TODO: 2PC

}