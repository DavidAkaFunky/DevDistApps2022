using Grpc.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bank
{

    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BankService : ProjectService.ProjectServiceBase
    {

        public BankService()
        {
        }

        public override Task<PerfectChannelReply> Test(
            PerfectChannelRequest request, ServerCallContext context)
        {
            return Task.FromResult(Reg(request));
        }

        public PerfectChannelReply Reg(PerfectChannelRequest request)
        {

            lock (this)
            {
                Console.WriteLine($"Received request with message: {request.Message}");
            }
            return new PerfectChannelReply
            {
                Status = true
            };
        }
    }
}
