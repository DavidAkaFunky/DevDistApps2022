﻿using Grpc.Core;
using Grpc.Net.Client;

namespace DADProject;

public class BoneyToBankFrontend
{
    private readonly int clientID;
    private int seq;
    private readonly GrpcChannel channel;

    public BoneyToBankFrontend(int clientID, string serverAddress)
    {
        this.clientID = clientID;
        seq = 0;
        channel = GrpcChannel.ForAddress(serverAddress);
    }

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
        client.AcceptCompareSwapResult(request);
    }
}