using System.Diagnostics;

//namespace DADProject ???

public class PuppetMaster {

    public static void Main(string[] args) {
        
        List<Process> processes = new List<Process>();

        String boneyExe;
        String bankExe;
        String clientExe;

        int numSlots = -1;
        int slotDuration = -1;

        try {

            //-----------------------------------set executables name according to OS

            ReadInput(args[0], processes, numSlots, slotDuration);

            StartProcesses(processes, numSlots, slotDuration);
            
            CloseProcesses(processes);
        
        } catch(Exception e) {
            Console.WriteLine("Exception: " + e.Message);
        }
    }

    public void ReadInput( String fileName, List<Process> processes, int numSlots, int slotDuration) {
        StreamReader inputFile = new StreamReader(fileName);
        String line = inputFile.ReadLine();

        while (line != null)
        {
            string[] tokens = line.Split(' ');
            string cmd = tokens[0];

            switch(cmd){
                case 'P':
                    if(tokens.Length < 3 || tokens.Length > 4) {
                        Console.WriteLine("ERROR: Invalid command");
                        break;
                    }

                    int processID = int.Parse(tokens[1]);
                    if (processID <= 0) {
                        Console.WriteLine("ERROR: Invalid amount");
                        break;
                    }

                    if(tokens.Length == 4) {
                        if ( tokens[2] == "boney"){
                            Process newProcess = new Process();

                            newProcess.StartInfo.FileName = boneyExe;
                            newProcess.StartInfo.Arguments = token[4];

                            processes.add(newProcess);

                        } else if ( tokens[2] == "bank") {
                            Process newProcess = new Process();

                            newProcess.StartInfo.FileName = bankExe;
                            newProcess.StartInfo.Arguments = token[4];

                            processes.add(newProcess);

                        } else {
                            Console.WriteLine("ERROR: Invalid amount");
                            break;
                        }

                    } else {
                        if ( tokens[2] != "client"){
                            Console.WriteLine("ERROR: Invalid amount");
                            break;
                        }
                        
                        Process newProcess = new Process();

                        newProcess.StartInfo.FileName = clientExe;

                        processes.add(newProcess);
                    }
                    break;

                case 'S':
                    if (token.Length != 2) {
                        Console.WriteLine("ERROR: Invalid amount");
                        break;
                    }

                    numSlots = int.Parse(tokens[1]);
                    break;

                case 'T':
                    if (token.Length != 2) {
                        Console.WriteLine("ERROR: Invalid amount");
                        break;
                    }

                    numSlots = token[1];//---------------------check if formats right
                    break;

                case 'D':
                    if (token.Length != 2) {
                        Console.WriteLine("ERROR: Invalid amount");
                        break;
                    }

                    slotDuration = int.Parse(tokens[1]);
                    break;

                case 'F':
                    //------------------------------------complete
                    break;

                default:
                    Console.WriteLine("ERROR: Unknown command");
                    break;
            }

            line = inputFile.ReadLine();
        }
        inputFile.Close();
    }

    public void StartProcesses(List<Process> processes, int numSlots, int slotDuration) {
        foreach (Process p in processes){
            //---------------------------------set args accordingly
            if (p.StartInfo.FileName == "boney.exe"){
                //newProcess.StartInfo.Arguments = add remaing info;
            } else if (p.StartInfo.FileName == "bank.exe"){
                //newProcess.StartInfo.Arguments = add remaing info;
            } else if (p.StartInfo.FileName == "client.exe"){
                //newProcess.StartInfo.Arguments = add necessary info;
            }

            p.Start();
        }

    }

    public void CloseProcesses(List<Process> proc) {
        int count = proc.Count - 1;
        String quit = "";

        while (quit != "quit") 
        { 
            Console.WriteLine("Type 'quit' to terminate all processes.");
            quit = Console.ReadLine();
        }

        while ( count >= 0 )
        {
            proc[count].CloseMainWindow();
            proc[count].Close();
            count--;
        }
    }
}


//  OSInfo.Name
//Environment.OSVersion()