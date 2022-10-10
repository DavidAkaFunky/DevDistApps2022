namespace DADProject;

public class Client
{
    public static void Main(string[] args)
    {
        const string DEPOSIT_CMD = "D";
        const string WITHDRAWAL_CMD = "W";
        const string READ_BALANCE_CMD = "R";
        const string WAIT_CMD = "S";

        if (args.Length < 2)
        {
            Console.Error.WriteLine("Too few arguments: [id] [configPath]");
            return;
        }
        else if (args.Length > 2)
        {
            Console.Error.WriteLine("Too many arguments: [id] [configPath]");
            return;
        }

        int id;
        try
        {
            id = int.Parse(args[0]);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        List<string> bankServers = new();
        int numberOfSlots = -1; // Is this needed for Clients? Hmmm

        foreach (string line in File.ReadAllLines(args[1]))
        {
            var tokens = line.Split(' ');

            if (tokens[0] == "P")
            {
                if (tokens[2] == "bank")
                {
                    if (tokens.Length != 4)
                        throw new Exception("Exactly 4 arguments needed for 'P bank' lines");
                    bankServers.Add(tokens[3]);
                }
            }
            if (tokens[0] == "S")
            {
                if (tokens.Length != 2)
                    throw new Exception("Exactly 2 arguments needed for 'S' lines");
                try
                {
                    numberOfSlots = int.Parse(tokens[1]);
                }
                catch (FormatException)
                {
                    Console.Error.WriteLine("Invalid value for number of slots");
                    return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
                }
            }
            else if (tokens[0] == "T" || tokens[0] == "D" || tokens[0] == "F")
                continue;
            else
            {
                Console.Error.WriteLine("Invalid line");
                return;
            }
        }


        if (numberOfSlots < 0)
            throw new Exception("No number of slots given.");

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        ClientFrontend frontend = new(bankServers);

        while (true)
        {
            Console.Write("Enter command: ");
            var line = Console.ReadLine();
            if (line is null)
                return;
            var tokens = line.Split(' ');
            var cmd = tokens[0];
            int amount;
            switch (cmd)
            {
                case DEPOSIT_CMD: // Deposit: D amount
                    if (tokens.Length != 2)
                    {
                        Console.Error.WriteLine("ERROR: Invalid command");
                        break;
                    }
                    
                    try
                    {
                        amount = int.Parse(tokens[1]);
                        if (amount <= 0)
                        {
                            Console.Error.WriteLine("ERROR: Invalid amount");
                            break;
                        }
                        frontend.Deposit(amount);
                    }
                    catch (FormatException)
                    {
                        Console.Error.WriteLine("ERROR: Invalid timespan");
                    }

                    break;
                case WITHDRAWAL_CMD: // Withdraw: W amount
                    if (tokens.Length != 2)
                    {
                        Console.Error.WriteLine("ERROR: Invalid command");
                        break;
                    }
                    try
                    {
                        amount = int.Parse(tokens[1]);
                        if (amount <= 0)
                        {
                            Console.Error.WriteLine("ERROR: Invalid amount");
                            break;
                        }

                        frontend.Withdraw(amount);
                    }
                    catch (FormatException)
                    {
                        Console.Error.WriteLine("ERROR: Invalid timespan");
                    }
                    
                    break;
                case READ_BALANCE_CMD: // Read balance: R
                    if (tokens.Length != 1)
                    {
                        Console.Error.WriteLine("ERROR: Invalid command");
                        break;
                    }

                    // TODO: Read balance
                    frontend.ReadBalance();
                    break;
                case WAIT_CMD: // Wait: S milliseconds
                    if (tokens.Length != 2)
                    {
                        Console.Error.WriteLine("ERROR: Invalid command");
                        break;
                    }

                    try
                    {
                        var milliseconds = int.Parse(tokens[1]);
                        if (milliseconds < 0)
                        {
                            Console.Error.WriteLine("ERROR: Invalid timespan");
                            break;
                        }

                        Thread.Sleep(milliseconds);
                        Console.WriteLine("hello");
                    }
                    catch (FormatException)
                    {
                        Console.Error.WriteLine("ERROR: Invalid timespan");
                    }

                    break;
                default:
                    Console.Error.WriteLine("ERROR: Unknown command");
                    break;
            }
        }
    }
}