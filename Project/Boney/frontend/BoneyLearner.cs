using Grpc.Net.Client;
using System.Collections.Generic;

namespace DADProject;

public class BoneyLearner
{
    private List<GrpcChannel> multiPaxosServers = new();
    private List<GrpcChannel> bankClients = new();
    private Dictionary<int, Dictionary<string, int>> acceptedValues = new();

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

    public void ReceiveAccepted(int slot, string acceptorAddress, int id, int value)
    {
        if (!acceptedValues.ContainsKey(slot))
            acceptedValues.Add(slot, new Dictionary<string, int>());
        acceptedValues[slot][acceptorAddress] = value;
        if (acceptedValues[slot].Count > multiPaxosServers.Count / 2 && new List<int>(acceptedValues[slot].Values.Distinct()).Count == 1)
        {
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
                    var client = new ProjectBoneyProposerService.ProjectBoneyProposerServiceClient(channel);
                    ResultToBankRequest request = new() { Slot = slot, Value = value };
                    ResultToBankReply reply = client.ResultToBank(request);
                });
                thread.Start();
            }
        }
    }

}
