﻿using Grpc.Core.Interceptors;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;

namespace DADProject;

public class BoneyProposer
{
    private int id;
    private List<GrpcChannel> multiPaxosServers = new(); // All servers are proposers, learners and acceptors
    private Dictionary<int, int> history = new();

    public BoneyProposer(int id, List<string> servers)
    {
        this.id = id;
        foreach (string s in servers)
            AddServer(s);
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

    public void AddOrSetHistory(int slot, int result)
    {
        history[slot] = result;
    }

    public void CheckMajority(int responses, List<Thread> threads)
    {
        if (responses > multiPaxosServers.Count / 2)
        {
            foreach (Thread thread in threads)
            {
                thread.Abort();
            }
            threads.Clear();
        }
    }

    public async void RunConsensus(int slot, int inValue)
    {
        int valueSentToAccept = inValue;
        int mostRecentReadTimestamp = id;
        history[slot] = -1;
        List<Thread> threads = new();
        if (id > 1) // Assuming the first Boney server has id = 1
        {
            var responses = 0;
            foreach (var channel in multiPaxosServers)
            {
                Thread thread = new(() =>
                {
                    PromiseReply reply = SendPrepare(channel, slot);

                    if (reply.Id > id)
                    {
                        return; // This probably won't kill the whole function though...
                    }

                    if (reply.Id > mostRecentReadTimestamp)
                    {
                        mostRecentReadTimestamp = reply.Id;
                        valueSentToAccept = reply.Value;
                    }

                    CheckMajority(++responses, threads);
                });
                threads.Add(thread);
                thread.Start();
            }
        }

        foreach (var channel in multiPaxosServers)
        {
            Thread thread = new(() =>
            {
                if (!SendAccept(channel, slot, mostRecentReadTimestamp, valueSentToAccept))
                    return;
            });
            thread.Start();
        }
    }

    public PromiseReply SendPrepare(GrpcChannel channel, int slot)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        PrepareRequest request = new() { Slot = slot, Id = id };
        PromiseReply reply = client.Prepare(request);
        return reply;
    }

    public bool SendAccept(GrpcChannel channel, int slot, int readTimestamp, int value)
    {
        var client = new ProjectBoneyAcceptorService.ProjectBoneyAcceptorServiceClient(channel);
        AcceptRequest request = new() { Slot = slot, Id = readTimestamp, Value = value };
        AcceptReply reply = client.Accept(request);
        return reply.Status;
    }
}
