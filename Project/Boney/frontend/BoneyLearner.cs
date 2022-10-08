using System;
using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Security.Principal;
using Google.Protobuf.WellKnownTypes;
using System.Threading.Channels;


namespace DADProject;

public class BoneyLearner
{
    private int id;
    private List<GrpcChannel> multiPaxosServers = new();
    private Dictionary<int, int> history = new();

    public BoneyLearner(int id, string[] servers)
    {
        this.id = id;
        foreach (string s in servers)
        {
            AddServer(s);
        }
    }

    public int Id
    {
        get { return id; }
    }

    public Dictionary<int, int> History
    {
        get { return history; }
    }

    public void AddServer(string server)
    {
        GrpcChannel channel = GrpcChannel.ForAddress(server);
        multiPaxosServers.Add(channel);
    }

}
