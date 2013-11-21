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
        ProcessStartInfo chimeraInfo;
        Process chimeraProcess;
        List<int> ports = new List<int>();
        String program;

        public ChimeraMonitor(string directory, string program)
        {
            this.program = program;
            chimeraInfo = new ProcessStartInfo(directory + program);
            chimeraInfo.WorkingDirectory = directory;
            chimeraInfo.UseShellExecute = true;
        }

        public void start()
        {
            chimeraProcess = Process.Start(chimeraInfo);
            Reporter.ReportStart(program);
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
    }
}
