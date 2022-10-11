using Grpc.Net.Client;
using System.Collections.Generic;

namespace DADProject;

public class BoneyLearner
{
    private List<GrpcChannel> multiPaxosServers = new();
    private List<GrpcChannel> bankClients = new();
    private Dictionary<int, Dictionary<int, int>> acceptedValues = new();

    public BoneyLearner(List<string> servers, List<string> clients)
    {
        foreach (string s in servers)
            AddChannel(multiPaxosServers, s);
        foreach (string s in clients)
            AddChannel(bankClients, s);
    }

    public void AddChannel(List<GrpcChannel> channels, string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        channels.Add(channel);
    }

    public void ReceiveAccepted(int slot, int id, int value)
    {
        if (!acceptedValues.ContainsKey(slot))
            acceptedValues.Add(slot, new Dictionary<int, int>());
        acceptedValues[slot][id] = value;
        Console.WriteLine("ENTERED HERE " + acceptedValues[slot].Count + new List<int>(acceptedValues[slot].Values.Distinct()).Count);
        if (acceptedValues[slot].Count > multiPaxosServers.Count / 2 && new List<int>(acceptedValues[slot].Values.Distinct()).Count == 1)
        {
            Console.WriteLine("ENTERED HERE TOO");
            foreach (GrpcChannel channel in multiPaxosServers)
            {
                Thread thread = new(() =>
                {
                    // CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor).Intercept();
                    var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
                    ResultToProposerRequest request = new() { Slot = slot, Value = value };
                    ResultToProposerReply reply = client.ResultToProposer(request);
                });
                thread.Start();
            }
            foreach (GrpcChannel channel in bankClients)
            {
                Thread thread = new(() =>
                {
                    // CallInvoker interceptingInvoker = channel.Intercept(boneyInterceptor).Intercept();
                    var client = new ProjectBankService.ProjectBankServiceClient(channel);
                    ResultToBankRequest request = new() { Slot = slot, Value = value };
                    // There is a bug here
                    ResultToBankReply reply = client.ResultToBank(request);
                });
                thread.Start();
            }
        }
    }

}
