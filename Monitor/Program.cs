using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Configuration;

namespace Monitor {
    class Monitor {
        private string opensim_directory = ConfigurationManager.AppSettings["opensim_directory"];
        private string chimera_directory = ConfigurationManager.AppSettings["chimera_directory"];
        private string chimera_exec = ConfigurationManager.AppSettings["chimera_exec"];
        private string[] client_names = ConfigurationManager.AppSettings["clients"].Split(',');
        private string client_name = ConfigurationManager.AppSettings["client_name"];

        OpenSimMonitor opensim;
        ChimeraMonitor chimera;
        ClientMonitor clients;

        Thread opensimThread;

        static void Main(string[] args) {
            Monitor monitor = new Monitor();
            monitor.StartServer();

            monitor.StartChimera();
            monitor.WaitChimera();

            monitor.StopReporter();

            monitor.StopServer();
        }

        public Monitor()
        {
            opensim = new OpenSimMonitor(opensim_directory);
            chimera = new ChimeraMonitor(chimera_directory, chimera_exec);
            clients = new ClientMonitor(client_name);
            foreach (string client in client_names)
            {
                clients.RegisterClient(client);
            }
            //clients.RegisterClient("Master");
            //clients.RegisterClient("Slave1");
            //clients.RegisterClient("Slave2");
            chimera.Clients = clients;
            Reporter.start();
        }

        public void StartServer()
        {
            opensim.start();
            opensimThread = new Thread(new ThreadStart(opensim.Monitor));
            opensimThread.Start();
            opensim.WaitForStarted();
        }

        public void StopServer()
        {
            opensim.Stop();
            opensimThread.Join();
        }

        public void StartChimera()
        {
            chimera.start();
            clients.Start();
        }

        public int WaitChimera()
        {
            int ret = chimera.Wait();
            clients.Stop();
            return ret;
        }

        public void StopReporter()
        {
            Reporter.Stop();
        }
    }
}
