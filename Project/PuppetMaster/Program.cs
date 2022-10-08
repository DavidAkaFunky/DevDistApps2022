namespace DadProject;

public class PuppetMaster
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Filename not passed as argument");
            return;
        }

        Runner runner = OperatingSystem.IsWindows() ? new WindowsRunner() : new UnixRunner();

    }
}

internal interface Runner
{
    public void Run(string executable, string[] args);
}

internal class UnixRunner : Runner
{
    public void Run(string executable, string[] args)
    {
        
    }
}
internal class WindowsRunner : Runner
{
    public void Run(string executable, string[] args)
    {
        
    }
}