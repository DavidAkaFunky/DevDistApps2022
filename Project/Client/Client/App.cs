namespace DADProject.Client;

public class App
{
    private static int Id { set; get; }

    public static void Main(string[] args)
    {
        const string DEPOSIT_CMD = "D";
        const string WITHDRAWAL_CMD = "W";
        const string READ_BALANCE_CMD = "R";
        const string WAIT_CMD = "S";

        if (args.Length != 2)
        {
            Console.Error.WriteLine("Wrong number of arguments: [id] [configPath]");
            return;
        }

        try
        {
            Id = int.Parse(args[0]);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        List<string> bankServers = new();

        foreach (var line in File.ReadAllLines(args[1]))
        {
            var tokens = line.Split(' ');
            if (tokens[0] == "P")
                if (tokens[2] == "bank")
                    bankServers.Add(tokens[3]);
        }

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        Frontend frontend = new(bankServers);

        PrintHeader();

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

    public static void PrintHeader()
    {
        Console.WriteLine("==========================================================");
        Console.WriteLine(" $$$$$$\\  $$\\       $$$$$$\\ $$$$$$$$\\ $$\\   $$\\ $$$$$$$$\\ ");
        Console.WriteLine("$$  __$$\\ $$ |      \\_$$  _|$$  _____|$$$\\  $$ |\\__$$  __|");
        Console.WriteLine("$$ /  \\__|$$ |        $$ |  $$ |      $$$$\\ $$ |   $$ |   ");
        Console.WriteLine("$$ |      $$ |        $$ |  $$$$$\\    $$ $$\\$$ |   $$ |   ");
        Console.WriteLine("$$ |      $$ |        $$ |  $$  __|   $$ \\$$$$ |   $$ |   ");
        Console.WriteLine("$$ |  $$\\ $$ |        $$ |  $$ |      $$ |\\$$$ |   $$ |   ");
        Console.WriteLine("\\$$$$$$  |$$$$$$$$\\ $$$$$$\\ $$$$$$$$\\ $$ | \\$$ |   $$ |   ");
        Console.WriteLine(" \\______/ \\________|\\______|\\________|\\__|  \\__|   \\__|   ");
        Console.WriteLine("==========================================================");
    }
}