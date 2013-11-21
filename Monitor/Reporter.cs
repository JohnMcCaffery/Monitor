﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Configuration;
using System.Web;

namespace Monitor
{
    class Reporter
    {
        private const string FOLDER = "Screenshots";
        private static string serverStr = ConfigurationManager.AppSettings["server"];
        private static string id = ConfigurationManager.AppSettings["id"];

        public static void ReportExit(string name)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramExit";
            args["program"] = name;
            SendPost(serverStr, null, args);
        }

        public static void ReportExit(string name, int exitcode)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramExit";
            args["program"] = name;
            args["exitCode"] = "" + exitcode;
            List<FileParameter> images = null;
            if (exitcode != 0)
                images = Reporter.TakeScreenShot();
            SendPost(serverStr, images, args);
        }

        public static void ReportStart(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStart";
            args["program"] = program;
            SendPost(serverStr, null, args);
        }

        public static void ReportStartBegin(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStartBegin";
            args["program"] = program;
            SendPost(serverStr, null, args);
        }

        public static void ReportStartEnd(string program)
        {
            NameValueCollection args = new NameValueCollection();
            args["id"] = id;
            args["command"] = "ProgramStartEnd";
            args["program"] = program;
            SendPost(serverStr, null, args);
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
                using (Bitmap mScreenshot = new Bitmap(s.Bounds.Width, s.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(mScreenshot))
                    {
                        g.CopyFromScreen(s.Bounds.Location, Point.Empty, s.Bounds.Size);
                        string name = t + "-" + s.DeviceName.Replace("\\", "").Trim('.') + ".png";
                        String f = FOLDER + "/" + name;
                        mScreenshot.Save(f);
                        FileInfo info = new FileInfo(f);
                        FileParameter file = new FileParameter(info, name, "image/png");
                        images.Add(file);
                    }
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

                SendPost(serverStr, null, args);
        }

        private static WebResponse SendPost(string reqiestUriString, List<FileParameter> files, NameValueCollection formData)
        {
            WebRequest req = HttpWebRequest.Create(reqiestUriString);
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
    }
}
