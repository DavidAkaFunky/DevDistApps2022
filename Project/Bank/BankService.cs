using Grpc.Core;

namespace DADProject
{

    // ChatServerService is the namespace defined in the protobuf
    // ChatServerServiceBase is the generated base implementation of the service
    public class BankService : ProjectBankService.ProjectBankServiceBase
    {
        private BankAccount account = new();

        public BankService() { }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            ReadBalanceReply reply = new() { Balance = account.Balance };
            return Task.FromResult(reply);
        }

        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            lock (this)
            {
                account.Deposit(request.Amount);
                DepositReply reply = new();
                return Task.FromResult(reply);
            }
        }

        public override Task<WithdrawReply> Withdraw(WithdrawRequest request, ServerCallContext context)
        {
            lock (this)
            {
                WithdrawReply reply = new() { Status = account.Withdraw(request.Amount) };
                return Task.FromResult(reply);
            }
        }
    }
}
