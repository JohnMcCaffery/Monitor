using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;

namespace Monitor
{
    class OpenSimMonitor : ProcessMonitor
    {
        ProcessStartInfo opensimInfo;
        Process opensimProcess;
        bool shutingdown = false;
        List<int> ports = new List<int>();
        Thread opensimStart;

        public OpenSimMonitor(string directory)
        {
            opensimInfo = new ProcessStartInfo(directory + "OpenSim.exe");
            opensimInfo.WorkingDirectory = directory;
            opensimInfo.UseShellExecute = true;

            using (StreamReader sr = new StreamReader(directory + "Regions\\Regions.ini"))
            {
                while (sr.EndOfStream)
                {
                    String line = sr.ReadLine();
                    if (line != null && line.StartsWith("InternalPort = "))
                    {
                        string[] l = line.Split(' ');
                        ports.Add(Int32.Parse(l[2]));
                    }
                }
            }
        }

        public void start()
        {
            opensimProcess = Process.Start(opensimInfo);
            Reporter.ReportStartBegin("OpenSim");
            opensimStart = new Thread(new ThreadStart(MonitorStart));
            opensimStart.Start();
        }

        public void MonitorStart()
        {
             int found = 0;
             do
             {
                 found = 0;
                 IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                 IPEndPoint[] endPoints = ipProperties.GetActiveUdpListeners();
                 foreach (IPEndPoint endpoint in endPoints)
                 {
                     if (ports.Contains(endpoint.Port))
                         found++;
                 }
                 Thread.Sleep(1000);
             } while (found < ports.Count);
             Reporter.ReportStartEnd("OpenSim");
        }

        public void Monitor()
        {
            opensimProcess.WaitForExit();
            int exitCode = opensimProcess.ExitCode;
            Reporter.ReportExit("OpenSim", exitCode);
            if (exitCode != 0)
            {
                Reporter.TakeScreenShot();
            }
            if (!shutingdown)
            {
                start();
            }
        }

        public void Stop()
        {
            shutingdown = true;
            if (opensimProcess.HasExited)
                return;
            SetForegroundWindow(opensimProcess.MainWindowHandle);
            Thread.Sleep(500);
            SendKeys.SendWait("shutdown\n");
        }
    }
}
