using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;
using System.IO;
using RawDiskLib;

namespace tptDiskImager
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        class imageState
        {
            public string progress;
            public string percent;
            public string speed;
            public string error;
            public long disksize;
            public long diskposition;
        }

        public void refreshDisks()
        {
            listView1.Items.Clear();
            WqlObjectQuery q = new WqlObjectQuery("SELECT * FROM Win32_DiskDrive");
            ManagementObjectSearcher res = new ManagementObjectSearcher(q);
            foreach (ManagementObject o in res.Get())
            {
                ListViewItem itm = new ListViewItem((string)o["Model"]);
                itm.SubItems.Add((string)o["DeviceID"]);

                itm.SubItems.Add(fsToHuman((UInt64)o["Size"]));
                listView1.Items.Add(itm);
                string hdstr = "";
                hdstr += "Caption = " + o["Caption"] + Environment.NewLine;
                hdstr += "DeviceID = " + o["DeviceID"] + Environment.NewLine;
                hdstr += "Decsription = " + o["Description"] + Environment.NewLine;
                hdstr += "Manufacturer = " + o["Manufacturer"] + Environment.NewLine;
                hdstr += "MediaType = " + o["MediaType"] + Environment.NewLine;
                hdstr += "Model = " + o["Model"] + Environment.NewLine;
                hdstr += "Name = " + o["Name"] + Environment.NewLine;
                //MessageBox.Show(hdstr);
                // only Vista & 2008: //Console.WriteLine("SerialNumber = "+o["SerialNumber"]);
            }
        }


        private void listView1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null)
            {
                for (var i = 0; i < listView.CheckedItems.Count; i++)
                {
                    listView.CheckedItems[i].Checked = false;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = saveFileDialog1.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            refreshDisks();
        }

        public string fsToHuman(UInt64 fsize)
        {
            Int64 fsz = Convert.ToInt64(fsize);
            return ((fsz / 1024) / 1024).ToString() + "MB";
        }

        public bool readCompleteOK = true;
        public string lastError = "";

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string devid = (string)e.Argument;
            string devnum = new String(devid.Where(Char.IsDigit).ToArray());
            RawDisk disk = new RawDisk(DiskNumberType.PhysicalDisk, Convert.ToInt32(devnum), FileAccess.Read);
            long diskReadLength = disk.SizeBytes;
            long totalRead = 0;
            int increment = (int)(((16f * 1024f * 1024f) / disk.SectorSize) * disk.SectorSize);
            byte[] input = new byte[increment];
            Stopwatch sw = new Stopwatch();

            FileStream fs = new FileStream(textBox1.Text, FileMode.Create);
            
            using (RawDiskStream diskFs = disk.CreateDiskStream())
            {
                sw.Start();
                while (true)
                {
                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        diskFs.Close();
                        fs.Close();
                        break;
                    }
                    int read = (int)Math.Min(increment, disk.SizeBytes - totalRead);
                    int actualRead = diskFs.Read(input, 0, read);

                    if (actualRead == 0)
                    {
                        diskFs.Close();
                        fs.Close();

                        if (totalRead == diskReadLength)
                        {
                            readCompleteOK = true;
                        }
                        else
                        {
                            readCompleteOK = false;
                        }

                        break;
                    }

                    totalRead += actualRead;
                    fs.Write(input, 0, input.Length);
                    imageState imst = new imageState();
                    imst.progress = fsToHuman(Convert.ToUInt64(totalRead)) + " / " + fsToHuman(Convert.ToUInt64(diskReadLength));
                    int perc = (int)(0.5f + ((100f * totalRead) / disk.SizeBytes));
                    //imst.disksize = disk.SizeBytes;
                    //imst.diskposition = totalRead;
                    imst.percent = perc.ToString() + "% complete";
                    imst.speed = Math.Round(((diskFs.Position / 1048576f) / sw.Elapsed.TotalSeconds), 2).ToString() + "MB/s";
                    backgroundWorker1.ReportProgress(perc, imst);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listView1.CheckedItems.Count < 1)
            {
                MessageBox.Show("Please select a disk to image");
            }
            else
            {
                if (textBox1.Text == "")
                {
                    MessageBox.Show("Select an image file to write to");
                }
                else
                {
                    listView1.Enabled = false;
                    button1.Enabled = false;
                    textBox1.Enabled = false;
                    button2.Enabled = false;
                    button3.Enabled = true;
                    backgroundWorker1.RunWorkerAsync(listView1.CheckedItems[0].SubItems[1].Text);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
            listView1.Enabled = true;
            button1.Enabled = true;
            textBox1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            imageState imst = (imageState)e.UserState;
            label2.Text = imst.progress;
            //label5.Text = ((int)Math.Round((double)(100 * imst.diskposition) / imst.disksize)).ToString() + "% completed";
            //progressBar1.Maximum = Convert.ToInt32(imst.disksize);
            //progressBar1.Value = Convert.ToInt32(imst.diskposition);
            label5.Text = imst.percent;
            progressBar1.Value = e.ProgressPercentage;
            label6.Text = imst.speed;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            listView1.Enabled = true;
            button1.Enabled = true;
            textBox1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = false;

            /*if (e.Error != null)
            {
                //MessageBox.Show(e.Error.ToString());
            }
            else
            {*/

                if (!e.Cancelled)
                {
                    if (!readCompleteOK) { MessageBox.Show("Disk Imaging complete" + Environment.NewLine + Environment.NewLine + "However we did not receive the amount of data that we expected. Please mount the image to determine it's integrity.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    else
                    {
                        MessageBox.Show("Disk Imaging completed successfully!", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                }
                else
                {
                    MessageBox.Show("Imaging cancelled!");
                }
            //}
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            refreshDisks();
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            about abt = new about();
            abt.ShowDialog();
        }
    }
}
