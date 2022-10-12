using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BankToBankFrontend
{
    private readonly int _clientId;
    private int _seq;
    private readonly GrpcChannel channel;

    public BankToBankFrontend(int clientId, string serverAddress)
    {
        _clientId = clientId;
        _seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

    // TODO: 2PC

}