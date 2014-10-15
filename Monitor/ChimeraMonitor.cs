using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Monitor
{
    class ChimeraMonitor : ProcessMonitor
    {
        private ProcessStartInfo chimeraInfo;
        private Process chimeraProcess;
        private List<int> ports = new List<int>();
        private String program;
        private ClientMonitor clientMonitor;
        private string directory;

        public ChimeraMonitor()
        {
            
            
        }

        public ClientMonitor Clients
        {
            set
            {
                clientMonitor = value;
            }
        }

        public void Init(string directory, string program) {
            this.program = program;
            this.directory = directory;
            chimeraInfo = new ProcessStartInfo(directory + program);

            chimeraProcess = new Process();
            
            chimeraProcess.StartInfo = chimeraInfo;
            chimeraProcess.StartInfo.WorkingDirectory = directory;
            chimeraProcess.StartInfo.UseShellExecute = false;
            chimeraProcess.StartInfo.RedirectStandardError = true;
            chimeraProcess.ErrorDataReceived += new DataReceivedEventHandler(OutputDataHandler);
            chimeraProcess.StartInfo.RedirectStandardOutput = true;
            chimeraProcess.OutputDataReceived += new DataReceivedEventHandler(ErrorDataHandler);
        }

        public void start()
        {
            chimeraProcess.Start();
            Reporter.ReportStart(program);
            chimeraProcess.BeginOutputReadLine();
            chimeraProcess.BeginErrorReadLine();
        }

        public int Wait()
        {
            chimeraProcess.WaitForExit();
            int exitCode = chimeraProcess.ExitCode;
            Reporter.ReportExit(program, exitCode);
            return exitCode;
        }

        public void Stop()
        {
            chimeraProcess.CloseMainWindow();
        }

        private void OutputDataHandler(object sendingProcess,
           DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                Console.WriteLine(outLine.Data);
            }
        }

        private void ErrorDataHandler(object sendingProcess,
            DataReceivedEventArgs errLine)
        {

            if (!String.IsNullOrEmpty(errLine.Data))
            {
                Console.Error.WriteLine(errLine.Data);
            }
        }

        private void PrcoessLine(string line)
        {
            /*if (line.Contains("Launching"))
            {
                Client client = clients.Find(
                            delegate(Client cl)
                            {
                                return theprocess.MainWindowTitle.Contains(cl.Name);
                            }
                        );
                if (client != null && !client.Running)
                    client.Process = theprocess;
            }*/
        }
    }
}
