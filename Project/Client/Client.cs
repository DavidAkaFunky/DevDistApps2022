namespace DADProject
{
    public class Client
    {
        public static void Main(string[] args)
        {
            const string DEPOSIT_CMD = "D";
            const string WITHDRAWAL_CMD = "W";
            const string READ_BALANCE_CMD = "R";
            const string WAIT_CMD = "S";

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            ClientFrontend frontend = new();
            //for (string server: args)
            string server = "http://localhost:5000";
            frontend.AddServer(server);

            while (true)
            {
                Console.Write("Enter command: ");
                string line = Console.ReadLine();
                if (line == null)
                    return;
                string[] tokens = line.Split(' ');
                string cmd = tokens[0];
                int amount;
                switch (cmd)
                {
                    case DEPOSIT_CMD: // Deposit: D amount
                        if (tokens.Length != 2)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Deposit "amount"
                        amount = int.Parse(tokens[1]);
                        if (amount <= 0)
                        {
                            Console.WriteLine("ERROR: Invalid amount");
                            break;
                        }
                        frontend.Deposit(amount);
                        break;
                    case WITHDRAWAL_CMD: // Withdraw: W amount
                        if (tokens.Length != 2)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Withdraw "amount"
                        amount = int.Parse(tokens[1]);
                        if (amount <= 0)
                        {
                            Console.WriteLine("ERROR: Invalid amount");
                            break;
                        }
                        frontend.Withdraw(amount);
                        break;
                    case READ_BALANCE_CMD: // Read balance: R
                        if (tokens.Length != 1)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Read balance
                        frontend.ReadBalance();
                        break;
                    case WAIT_CMD: // Wait: S milliseconds
                        if (tokens.Length != 2)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        try
                        {
                            int milliseconds = int.Parse(tokens[1]);
                            if (milliseconds < 0)
                            {
                                Console.WriteLine("ERROR: Invalid timespan");
                                break;
                            }
                            Thread.Sleep(milliseconds);
                            Console.WriteLine("hello");
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("ERROR: Invalid timespan");
                        }
                        break;
                    default:
                        Console.WriteLine("ERROR: Unknown command");
                        break;
                }
            }
        }
    }

}
