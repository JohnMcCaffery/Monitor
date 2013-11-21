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

namespace Monitor {
    class Monitor {
        private string opensim_directory = "C:\\Users\\openvirtualworlds\\Desktop\\Opensim-Timespan\\bin\\";

        OpenSimMonitor opensim;
        ChimeraMonitor chimera;
        ClientMonitor clients;

        Thread opensimThread;

        static void Main(string[] args) {
            Monitor monitor = new Monitor();
            monitor.StartServer();

            monitor.StartChimera();
            monitor.WaitChimera();

            monitor.StopServer();
        }

        public Monitor()
        {
            opensim = new OpenSimMonitor(opensim_directory);
            chimera = new ChimeraMonitor("C:\\Users\\openvirtualworlds\\vierwer\\Chimera\\Bin\\", "Timespan.exe");
            clients = new ClientMonitor();
            clients.RegisterClient("Master");
            clients.RegisterClient("Slave1");
            clients.RegisterClient("Slave2");
        }

        public void StartServer()
        {
            opensim.start();
            opensimThread = new Thread(new ThreadStart(opensim.Monitor));
            opensimThread.Start();
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
    }
}
