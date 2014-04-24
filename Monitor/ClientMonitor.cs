using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Monitor
{
    class ClientMonitor
    {
        List<Client> clients;
        int notFound;
        bool shutingdown;
        Thread findThread;

        public ClientMonitor()
        {
            this.clients = new List<Client>();
            notFound = 0;
            shutingdown = false;
        }

        public void RegisterClient(string name)
        {
            Client client = new Client(name, this);
            clients.Add(client);
            notFound++;
        }

        public void findClients()
        {
            while (!shutingdown)
            {
                if (notFound != 0)
                {
                    Process[] processlist = Process.GetProcessesByName("Firestorm-private-shutle01");

                    foreach (Process theprocess in processlist)
                    {
                        Client client = clients.Find(
                            delegate(Client cl)
                            {
                                return theprocess.MainWindowTitle.Contains(cl.Name);
                            }
                        );
                        if (client != null && !client.Running)
                        {
                            client.Process = theprocess;
                        }
                    }
                }
                Thread.Sleep(2000);
            }
        }

        public void Start()
        {
            findThread = new Thread(new ThreadStart(findClients));
            findThread.Start();
        }

        public void Stop()
        {
            shutingdown = true;
        }

        public void ClientStatusChange(bool running)
        {
            if (running)
                notFound--;
            else
                notFound++;
            Reporter.UpdateClientStatus(clients);
        }


        public List<Client> Clinets
        {
            get
            {
                return clients;
            }
        }
    }

    class Client
    {
        string name;
        Process process;
        bool running;
        ClientMonitor monitor;

        public string Name { get { return name; } }
        public bool Running { get { return running; } }

        public Process Process
        {
            set
            {
                process = value;
                running = true;
                process.EnableRaisingEvents = true;
                process.Exited += new EventHandler(clientExited);
                monitor.ClientStatusChange(true);
                ProcessMonitor.ClickToFocus(process.MainWindowHandle);
            }
        }

        public Client(string name, ClientMonitor monitor)
        {
            this.name = name;
            this.monitor = monitor;
            running = false;
        }

        private void clientExited(object sender, System.EventArgs e)
        {
            running = false;
            monitor.ClientStatusChange(false);
        }
    }
}
