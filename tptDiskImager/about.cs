using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace tptDiskImager
{
    public partial class about : Form
    {
        public about()
        {
            InitializeComponent();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process proc = new Process();
            ProcessStartInfo si = new ProcessStartInfo("http://www.tptech.com.au");
            proc.StartInfo = si;
            proc.Start();
        }
    }
}
