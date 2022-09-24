using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string DEPOSIT_CMD = "D";
            const string WITHDRAWAL_CMD = "W";
            const string READ_BALANCE_CMD = "R";
            const string WAIT_CMD = "S";

            while (true)
            {
                Console.Write("Enter command: ");
                string line = Console.ReadLine();
                if (line == null)
                    return;
                string[] tokens = line.Split(' ');
                string cmd = tokens[0];
                switch (cmd)
                {
                    case DEPOSIT_CMD: // Deposit: D name amount
                        if (tokens.Length != 3)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Deposit "amount" to "name"
                        break;
                    case WITHDRAWAL_CMD: // Withdraw: W name amount
                        if (tokens.Length != 3)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Withdraw "amount" from "name"
                        break;
                    case READ_BALANCE_CMD: // Read balance: R name
                        if (tokens.Length != 2)
                        {
                            Console.WriteLine("ERROR: Invalid command");
                            break;
                        }
                        // TODO: Read balance from "name"
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
                        catch (FormatException e)
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
