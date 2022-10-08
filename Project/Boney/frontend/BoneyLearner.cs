using Grpc.Net.Client;
using System.Collections.Generic;

namespace DADProject;

public class BoneyLearner
{
    private List<GrpcChannel> multiPaxosServers = new();
    private Dictionary<int, Dictionary<string, int>> acceptedValues = new();

    public BoneyLearner( string[] servers)
    {
        foreach (string s in servers)
            AddServer(s);
    }

    public void AddServer(string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }

    public void ReceiveAccepted(int slot, string acceptorAddress, int id, int value)
    {
        if (!acceptedValues.ContainsKey(slot))
            acceptedValues.Add(slot, new Dictionary<string, int>());
        acceptedValues[slot][acceptorAddress] = value;
        if (acceptedValues[slot].Count > multiPaxosServers.Count / 2 && new List<int>(acceptedValues[slot].Values.Distinct()).Count == 1)
        {
            // TODO: Send result to clients AND to proposers (to add to history)
        }

    }

}
