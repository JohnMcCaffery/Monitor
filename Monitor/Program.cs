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
using System.ComponentModel;

namespace Monitor {
    class Monitor {
        private string opensim_directory_default = ConfigurationManager.AppSettings["opensim_directory"];
        private string chimera_directory_default = ConfigurationManager.AppSettings["chimera_directory"];
        private string chimera_exec_default = ConfigurationManager.AppSettings["chimera_exec"];
        private string[] client_names_default = ConfigurationManager.AppSettings["clients"].Split(',');
        private string client_name_default = ConfigurationManager.AppSettings["client_name"];

        private string[] installations = ConfigurationManager.AppSettings["installations"].Split(',');
        private string default_installation = ConfigurationManager.AppSettings["default_installation"];

        private string opensim_directory;
        private string chimera_directory;
        private string chimera_exec;
        private string[] client_names;
        private string client_name;

        private string installation;

        OpenSimMonitor opensim;
        ChimeraMonitor chimera;
        ClientMonitor clients;

        Thread opensimThread;
        private Selection selection;
        private bool restarting = false;
        private Hotkey hotkey;

        static void Main(string[] args) {
            Monitor monitor = new Monitor();
            monitor.start();
        }

        public Monitor()
        {
            opensim = new OpenSimMonitor();
            chimera = new ChimeraMonitor();
            clients = new ClientMonitor();

            installation = default_installation;

            chimera.Clients = clients;
            Reporter.start();

            hotkey = new Hotkey();
            hotkey.KeyCode = Keys.F5;
            hotkey.Pressed += new HandledEventHandler(hostkey_keypressed);

        }

        public void start() {
            selection = new Selection(installations);
            //s.Show();
            new Thread(run).Start();
           
            Application.Run(selection);
        }

        public void SelectInstelation() {
            selection.Visible = true;
            selection.Focus();
            ProcessMonitor.ClickToFocus(selection.Handle);
            Thread.Sleep(60000);
            installation = selection.Installation;
            string s = ConfigurationManager.AppSettings[string.Format("{0}_opensim_directory", installation)];
            if (s != null)
                opensim_directory = s;
            else
                opensim_directory = opensim_directory_default;

            s = ConfigurationManager.AppSettings[string.Format("{0}_chimera_directory", installation)];
            if (s != null)
                chimera_directory = s;
            else
                chimera_directory = chimera_directory_default;

            s = ConfigurationManager.AppSettings[string.Format("{0}_chimera_exec", installation)];
            Console.WriteLine(s);
            if (s != null)
                chimera_exec = s;
            else
                chimera_exec = chimera_exec_default;

            s = ConfigurationManager.AppSettings[string.Format("{0}_clients", installation)];
            if (s != null)
                client_names = s.Split(',');
            else
                client_names = client_names_default;

            s = ConfigurationManager.AppSettings[string.Format("{0}_client_name", installation)];
            if (s != null)
                client_name = s;
            else
                client_name = client_name_default;

            Init();
            selection.Visible = false;
        }

        public void run() {
            Thread.Sleep(100);
            selection.Invoke(new Action(() => {
                //if (hotkey.GetCanRegister(selection)) {
                hotkey.Register(selection);
                //}
            }));

            do {
                restarting = false;
                SelectInstelation();
                StartServer();

                StartChimera();
                WaitChimera();

                StopReporter();

                StopServer();
            } while (restarting);

            selection.Close();
        }

        public void Init() {
            opensim.Init(opensim_directory);
            chimera.Init(chimera_directory, chimera_exec);
            clients.Init(client_name);
            foreach (string client in client_names) {
                clients.RegisterClient(client);
            }

            
        }

        private void hostkey_keypressed(object sender, HandledEventArgs args) {
            Console.WriteLine("Key Pressed");
            restarting = true;
            SendKeys.SendWait("{F4}");
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
