﻿using Grpc.Core.Interceptors;
using Grpc.Core;

namespace DADProject
{
    class Boney
    {
        static void Main()
        {
            string serverHostname = "localhost";
            int serverPort = 1000;
            MultiPaxos multiPaxos = new();
            string x = "http://localhost:5000";
            multiPaxos.AddServer(x);
            Server server = new()
            {
                Services = { ProjectBoneyService.BindService(new BoneyService(multiPaxos)).Intercept(new ServerInterceptor()) },
                Ports = { new ServerPort(serverHostname, serverPort, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.WriteLine("ChatServer server listening on port " + serverPort);
            Console.WriteLine("Press any key to stop the server...");
            Console.ReadKey();

            server.ShutdownAsync().Wait();

        }
    }

    public class ServerInterceptor : Interceptor
    {

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            string callId = context.RequestHeaders.GetValue("dad");
            Console.WriteLine("DAD header: " + callId);
            return base.UnaryServerHandler(request, context, continuation);
        }

    }
}