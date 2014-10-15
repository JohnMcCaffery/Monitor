using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Monitor {
    public partial class Selection : Form {
        private string[] installations;
        public string Installation {get; private set; }

        public Selection(string[] installations) {
            InitializeComponent();
            this.WindowState = FormWindowState.Normal;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Bounds = Screen.PrimaryScreen.Bounds;
            this.installations = installations;
            this.listBox1.DataSource = installations;
        }

        private void selection_KeyPress(object sender, KeyPressEventArgs e) {
            int listIndex = -1;
            for (int i = 0; i < installations.Length; i++) {
                if (installations[i].StartsWith(e.KeyChar.ToString())) {
                    Console.WriteLine(string.Format("Changed to {0}", installations[i]));
                    Installation = installations[i];
                    listIndex = i;
                }
            }
            if (listIndex != -1) {
                this.listBox1.SelectedIndex = listIndex;
            }
        }

        private void listbox_SelectedIndexChanged(object sender, EventArgs e) {
            Installation = installations[listBox1.SelectedIndex];
        }

    }
}
