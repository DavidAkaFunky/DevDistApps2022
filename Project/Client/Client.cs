using System.Globalization;
using System.Text;

namespace DADProject;

public class Client
{
    private static int _initialSleepTime;

    private static int Id { set; get; }

    public static void Main(string[] args)
    {
        const string DEPOSIT_CMD = "D";
        const string WITHDRAWAL_CMD = "W";
        const string READ_BALANCE_CMD = "R";
        const string WAIT_CMD = "S";

        Console.OutputEncoding = Encoding.Unicode;
        if (args.Length != 4)
        {
            Console.Error.WriteLine(
                "Wrong number of arguments: [id] [generalConfigPath] [clientConfigPath] [startTime]");
            return;
        }

        try
        {
            Id = int.Parse(args[0]);
            var currentTime = DateTime.Now;
            var startTime = Convert.ToDateTime(args[3]);
            if (startTime < currentTime)
                startTime = startTime.AddDays(1);
            _initialSleepTime = (int)(startTime - currentTime).TotalMilliseconds;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("Invalid arguments");
            return; // TODO: throw new DADException(ErrorCode.MissingConfigFile) does not work 
        }

        List<string> bankServers = new();

        File.ReadLines(args[1]).ToList().ForEach(line =>
        {
            var tokens = line.Split(' ');
            if (tokens[0] == "P")
                if (tokens[2] == "bank")
                    bankServers.Add(tokens[3]);
        });

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        ClientFrontend frontend = new(bankServers, Id);

        PrintHeader();

        Console.WriteLine("Waiting for " + _initialSleepTime + " milliseconds");
        Thread.Sleep(_initialSleepTime);

        File.ReadLines(args[2]).ToList().ForEach(line =>
        {
            Console.WriteLine(line);
            var tokens = line.Split(' ');
            var cmd = tokens[0];
            double amount;
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
                        amount = double.Parse(tokens[1], CultureInfo.InvariantCulture);
                        if (amount <= 0)
                        {
                            Console.Error.WriteLine("ERROR: Invalid deposit amount");
                            break;
                        }

                        frontend.Deposit(amount);
                    }
                    catch (FormatException)
                    {
                        try
                        {
                            amount = int.Parse(tokens[1]);
                            if (amount <= 0)
                            {
                                Console.Error.WriteLine("ERROR: Invalid deposit amount");
                                break;
                            }

                            frontend.Deposit(amount);
                        }
                        catch (FormatException)
                        {
                            Console.Error.WriteLine("ERROR: Invalid deposit amount");
                        }
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
                        amount = double.Parse(tokens[1], CultureInfo.InvariantCulture);
                        if (amount <= 0)
                        {
                            Console.Error.WriteLine("ERROR: Invalid withdrawal amount");
                            break;
                        }

                        frontend.Withdraw(amount);
                    }
                    catch (FormatException)
                    {
                        try
                        {
                            amount = int.Parse(tokens[1]);
                            if (amount <= 0)
                            {
                                Console.Error.WriteLine("ERROR: Invalid withdrawal amount");
                                break;
                            }

                            frontend.Withdraw(amount);
                        }
                        catch (FormatException)
                        {
                            Console.Error.WriteLine("ERROR: Invalid withdrawal amount");
                        }
                    }

                    break;
                case READ_BALANCE_CMD: // Read balance: R
                    if (tokens.Length != 1)
                    {
                        Console.Error.WriteLine("ERROR: Invalid command");
                        break;
                    }

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
        });
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