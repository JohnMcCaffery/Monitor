using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Configuration;
using System.Web;
using System.Threading;

namespace Monitor
{
    class Reporter
    {
        private static BlockingCollection<Report> MessageQueue = new BlockingCollection<Report>();
        private const string FOLDER = "Screenshots";
        private static string serverStr = ConfigurationManager.AppSettings["server"];
        private static string id = ConfigurationManager.AppSettings["id"];
        private bool shutingdown = false;
        private static Reporter instance;
        private Thread sender;
        private Thread heartbeat;
        private ManualResetEvent waitHandle = new ManualResetEvent(false);

        public static void start()
        {
            instance = new Reporter();
            instance.startSender();
        }

        public static void Stop()
        {
            instance.shutingdown = true;
            instance.waitHandle.Set();
        }

        private Reporter()
        {
            sender = new Thread(new ThreadStart(SendQueue));
            heartbeat = new Thread(new ThreadStart(HeartBeat));
        }

        private void startSender()
        {
            sender.Start();
            heartbeat.Start();
        }

        public static void ReportExit(string name)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramExit";
            args["program"] = name;
            EnquePost(serverStr, null, args);
        }

        public static void ReportExit(string name, int exitcode)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramExit";
            args["program"] = name;
            args["exitCode"] = "" + exitcode;
            List<FileParameter> images = null;
            images = Reporter.TakeScreenShot();
            EnquePost(serverStr, images, args);
        }

        public static void ReportStart(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStart";
            args["program"] = program;
            EnquePost(serverStr, null, args);
        }

        public static void ReportStartBegin(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStartBegin";
            args["program"] = program;
            EnquePost(serverStr, null, args);
        }

        public static void ReportStartEnd(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStartEnd";
            args["program"] = program;
            EnquePost(serverStr, null, args);
        }

        public static List<FileParameter> TakeScreenShot()
        {
            DateTime n = DateTime.Now;
            string t = n.ToString("yyyy.MM.dd-HH.mm");

            if (!Directory.Exists(FOLDER))
                Directory.CreateDirectory(FOLDER);

            List<FileParameter> images = new List<FileParameter>();
            foreach (var s in Screen.AllScreens)
            {
                try {
                    using (Bitmap mScreenshot = new Bitmap(s.Bounds.Width, s.Bounds.Height)) {
                        using (Graphics g = Graphics.FromImage(mScreenshot)) {
                            g.CopyFromScreen(s.Bounds.Location, Point.Empty, s.Bounds.Size);
                            string name = t + "-" + s.DeviceName.Replace("\\", "").Trim('.') + ".png";
                            String f = FOLDER + "/" + name;
                            mScreenshot.Save(f);
                            mScreenshot.Dispose();
                            FileInfo info = new FileInfo(f);
                            FileParameter file = new FileParameter(info, name, "image/png");
                            images.Add(file);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                }
            }
            return images;
        }

        public static void UpdateClientStatus(List<Client> clients)
        {

                NameValueCollection args = new NameValueCollection();
                args["id"] = id;
                args["command"] = "ClientStatus";
                foreach (Client client in clients)
                {
                    args.Add("clients", client.Name);
                    args.Add(client.Name, client.Running.ToString());
                }
                List<FileParameter> images = null;
                images = Reporter.TakeScreenShot();
                EnquePost(serverStr, images, args);
        }

        private static void EnquePost(string requestUriString, List<FileParameter> files, NameValueCollection formData)
        {
            Report report = new Report(requestUriString, files, formData);
            MessageQueue.Add(report);
        }

        private void SendQueue()
        {
            Report report;
            while (!shutingdown)
            {
                report = MessageQueue.Take();
                try
                {
                    SendPost(report);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send report");
                }
            }
            while (MessageQueue.TryTake(out report))
            {
                try
                {
                    SendPost(report);
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to send report");
                }
            }
        }

        private void HeartBeat()
        {
            while (!shutingdown)
            {
                EnqueuHeartBeat();
                waitHandle.WaitOne(60000);
            }
        }

        private void EnqueuHeartBeat()
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "HeartBeat";
            EnquePost(serverStr, null, args);
        }

        private static WebResponse SendPost(Report report)
        {
            return SendPost(report.RequestUriString, report.Files, report.FormData);
        }

        private static WebResponse SendPost(string requestUriString, List<FileParameter> files, NameValueCollection formData)
        {
            WebRequest req = HttpWebRequest.Create(requestUriString);
            string boundary = DateTime.Now.Ticks.ToString("x");
            byte[] data = PreparePostData(files, formData, boundary);
            req.Method = "POST";
            req.ContentType = "multipart/form-data; boundary=" + boundary;
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Flush();
            }
            return req.GetResponse();
        }

        private static byte[] PreparePostData(List<FileParameter> files, NameValueCollection formData, string boundary)
        {
            MemoryStream postDataStream = new System.IO.MemoryStream();

            //adding form data
            string formDataHeaderTemplate = "\r\n--" + boundary + "\r\n" +
            "Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

            foreach (string key in formData.Keys)
            {
                byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(string.Format(formDataHeaderTemplate,
                key, formData[key]));
                postDataStream.Write(formItemBytes, 0, formItemBytes.Length);
            }

            if (files != null)
            {
                foreach (FileParameter fileParameter in files)
                {
                    FileInfo fileInfo = fileParameter.fileInfo;
                    string fileHeaderTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"" +
                    "\r\nContent-Type: {2}\r\n\r\n";

                    byte[] fileHeaderBytes = System.Text.Encoding.UTF8.GetBytes(string.Format(fileHeaderTemplate,
                    fileParameter.name, fileInfo.FullName, fileParameter.ContentType));

                    postDataStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);

                    FileStream fileStream = fileInfo.OpenRead();

                    byte[] buffer = new byte[1024];

                    int bytesRead = 0;

                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        postDataStream.Write(buffer, 0, bytesRead);
                    }

                    fileStream.Close();
                }
            }

            byte[] endBoundaryBytes = System.Text.Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
            postDataStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);

            return postDataStream.ToArray();
        }

        public class FileParameter
        {
            public FileInfo fileInfo;
            public string name;
            public string ContentType;

            public FileParameter(FileInfo fileInfo, string name, string contenttype)
            {
                this.fileInfo = fileInfo;
                this.name = name;
                ContentType = contenttype;
            }
        }

        public class Report
        {
            public string RequestUriString { get; private set; }
            public List<FileParameter> Files { get; private set; }
            public NameValueCollection FormData { get; private set; }

            public Report(string requestUriString, List<FileParameter> files, NameValueCollection formData)
            {
                this.RequestUriString = requestUriString;
                this.Files = files;
                this.FormData = formData;
            }
        }
    }
}
