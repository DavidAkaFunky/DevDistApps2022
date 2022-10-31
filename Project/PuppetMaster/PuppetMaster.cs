using System.Diagnostics;

namespace DADProject;

// refactor the interface to receive an identifier and a path
internal interface IRunner
{
    public Process Run(string executable, string args);

    public Process Run(string cwd, string executable, string args);
}

internal class UnixRunner : IRunner
{
    public Process Run(string cwd, string executable, string args)
    {
        try
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd);
            var p = new Process();
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "alacritty",
                Arguments = $"-e sh -c \"{executable} {args} ; sleep 3600 \""
            });

            if (process == null)
                throw new DADException(ErrorCode.FailedStartingProcess);

            Directory.SetCurrentDirectory(pwd);
            return process;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw new DADException(ErrorCode.FailedStartingProcess);
        }
    }

    public Process Run(string executable, string args)
    {
        return Run(Directory.GetCurrentDirectory(), executable, args);
    }
}

internal class WindowsRunner : IRunner
{
    public Process Run(string cwd, string executable, string args)
    {
        try
        {
            var pwd = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(cwd);
            var p = new Process();
            var process = Process.Start(new ProcessStartInfo
            {
                //FileName = "cmd.exe",
                //Arguments = $"/c {executable} {args}",
                FileName = executable,
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Normal
            });

            if (process == null)
                throw new DADException(ErrorCode.FailedStartingProcess);

            Directory.SetCurrentDirectory(pwd);
            return process;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw new DADException(ErrorCode.FailedStartingProcess);
        }
    }

    public Process Run(string executable, string args)
    {
        return Run(Directory.GetCurrentDirectory(), executable, args);
    }
}

public class DADException : Exception
{
    public DADException(ErrorCode code) : base(ErrorMessage.Get(code))
    {
    }
}

public enum ErrorCode
{
    FailedStartingProcess,
    MissingConfigFile
}

public class ErrorMessage
{
    public static string Get(ErrorCode code)
    {
        switch (code)
        {
            case ErrorCode.FailedStartingProcess:
                return "Couldn't start new process";
            case ErrorCode.MissingConfigFile:
                return "The configuration file is missing (probably wrong path)";
            default:
                return "Unknown Exception";
        }
    }
}
// internal class WindowsRunner : Runner
// {
//     public Process? Run(string executable, string args)
//     {
//         
//     }
// }

public class PuppetMaster
{
    public static void Main(string[] args)
    {
        // Only one argument and it is the file name
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Filename not passed as argument");
            return;
        }

        if (args.Length > 1)
        {
            Console.Error.WriteLine("Too many arguments");
            return;
        }

        // TODO eu assumi que a syntax dos scripts está sempre correta, caso não se verifique, muito rapidamente se mete um regex aqui
        //IRunner runner = new UnixRunner();

        IRunner runner = OperatingSystem.IsWindows() ? new WindowsRunner() : new UnixRunner();

        // processos vao receber como argumentos um id e o caminho para o ficheiro de config
        // depois vao à procura da sua linha e configuram-se
        // assim o acesso à informação comum a todos é facilitado

        StreamReader inputFile;
        try
        {
            inputFile = new StreamReader(args[0]);
        }
        catch (Exception)
        {
            throw new DADException(ErrorCode.MissingConfigFile);
        }

        List<string> boneys = new();
        List<string> banks = new();
        List<Tuple<string, string>> clients = new();
        List<Process> processes = new();

        while (inputFile.ReadLine() is { } line)
        {
            var tokens = line.Split(' ');

            if (tokens[0] == "P")
            {
                var processId = tokens[1];
                switch (tokens.Length)
                {
                    // banks and boney
                    case 4 when tokens[2] == "boney":
                        boneys.Add(processId);
                        break;
                    case 4 when tokens[2] == "bank":
                    {
                        banks.Add(processId);
                        break;
                    }
                    // clients
                    case 4:
                        clients.Add(new(processId, tokens[3]));
                        break;
                }
            }
        }
        //PARA TESTE
        //processes.AddRange(boneys.Select(id => runner.Run("../Boney", "dotnet", $"run {id} {args[0]} > boney{id}.txt")));
        //processes.AddRange(banks.Select(id => runner.Run("../Bank", "dotnet", $"run {id} {args[0]} > bank{id}.txt")));

        processes.AddRange(boneys.Select(id => runner.Run("../Boney", "dotnet", $"run {id} {args[0]}")));
        processes.AddRange(banks.Select(id => runner.Run("../Bank", "dotnet", $"run {id} {args[0]}")));
        processes.AddRange(clients.Select(client => runner.Run("../Client", "dotnet", $"run {client.Item1} {args[0]} {client.Item2}")));

        Console.WriteLine("Type anything to kill all processes");
        Console.ReadLine();

        processes.ForEach(process => process.Kill());
    }
}