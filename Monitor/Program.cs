using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Monitor {
    class Program {
        private const string FOLDER = "Screenshots";

        static void Main(string[] args) {
            DateTime n = DateTime.Now;
            string t = n.ToString("yyyy.MM.dd-HH.mm");

            if (!Directory.Exists(FOLDER))
                Directory.CreateDirectory(FOLDER);

            foreach (var s in Screen.AllScreens) {
                using (Bitmap mScreenshot = new Bitmap(s.Bounds.Width, s.Bounds.Height)) {
                    using (Graphics g = Graphics.FromImage(mScreenshot)) {
                        g.CopyFromScreen(s.Bounds.Location, Point.Empty, s.Bounds.Size);
                        String f = FOLDER + "/" + t + "-" + s.DeviceName.Replace("\\", "").Trim('.') + ".bmp";
                        mScreenshot.Save(f);
                    }
                }
            }
        }
    }
}
