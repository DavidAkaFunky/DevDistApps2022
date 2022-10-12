using Grpc.Core;
using Grpc.Net.Client;
using System;

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
        var request = new AcceptRequest()
        {
            TimestampId = id,
            Slot = slot,
            Value = value
        };
        client.Accept(request);
    }
    
    // Bank
    public void RequestCompareAndSwap(int slot, int value) 
    {
        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        CompareAndSwapRequest request = new() 
        { 
            Slot = slot, 
            InValue = value 
        };
        var reply = client.CompareAndSwap(request);
        Console.WriteLine("Request Delivered! Answered: {0}", reply.OutValue);
    }
    
    // Acceptor 
    public void AcceptedToLearner(int slot, int id, int value) 
    {
        var client = new ProjectBoneyLearnerService.ProjectBoneyLearnerServiceClient(channel);
        var request = new AcceptedToLearnerRequest()
        {
            Slot = slot,
            TimestampId = id,
            Value = value
        };

        client.AcceptedToLearner(request);
    }
    
    // Learner
    public void ResultToProposer(int slot, int value) {

        var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
        var request = new ResultToProposerRequest()
        {
            Slot = slot,
            Value = value
        };

        client.ResultToProposer(request);
    }
}