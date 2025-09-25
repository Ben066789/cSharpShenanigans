using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace chicanery
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    public class Form1 : Form
    {
        private Button btnEnable;
        private Button btnDisable;
        private PictureBox statusImage;
        private string hostsPath;
        private string[] blockEntries = {
            "127.0.0.1 facebook.com",
            "127.0.0.1 www.facebook.com",
            "::1 facebook.com",
            "::1 www.facebook.com"
        };

        private string enabledImagePath = @"C:\Users\COMLAB-PC\Documents\satono.jpg";
        private string disabledImagePath = @"C:\Users\COMLAB-PC\Documents\yearps.jpg";
        private string defaultImg = @"C:\Users\COMLAB-PC\Documents\deviousAhhEmoji.jpg";

        public Form1()
        {
            this.Text = "chicanerying";
            this.Width = 350;
            this.Height = 250;

            btnEnable = new Button() { Text = "enable", Left = 80, Top = 30, Width = 180 };
            btnDisable = new Button() { Text = "disable", Left = 80, Top = 70, Width = 180 };

            statusImage = new PictureBox()
            {
                Left = 80,
                Top = 120,
                Width = 180,
                Height = 80,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnEnable.Click += BtnEnable_Click;
            btnDisable.Click += BtnDisable_Click;

            this.Controls.Add(btnEnable);
            this.Controls.Add(btnDisable);
            this.Controls.Add(statusImage);

            hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

            SetStatusImage(false);
        }

        private void BtnEnable_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> lines = File.ReadAllLines(hostsPath).ToList();
                foreach (string entry in blockEntries)
                {
                    if (!lines.Contains(entry))
                        lines.Add(entry);
                }
                File.WriteAllLines(hostsPath, lines);
                SetStatusImage(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            FlushDns();
            MessageBox.Show("na block na lods, halongs");
            this.Close();
        }

        private void BtnDisable_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> lines = File.ReadAllLines(hostsPath).ToList();
                foreach (string entry in blockEntries)
                {
                    lines.RemoveAll(l => l.Trim() == entry);
                }
                File.WriteAllLines(hostsPath, lines);
                SetStatusImage(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            FlushDns();
            MessageBox.Show("unblocked");
        }

        private void SetStatusImage(bool enabled)
        {
            try
            {
                if (enabled && File.Exists(enabledImagePath))
                    statusImage.Image = Image.FromFile(enabledImagePath);
                else if (!enabled && File.Exists(disabledImagePath))
                    statusImage.Image = Image.FromFile(disabledImagePath);
                else
                    statusImage.Image = null;
            }
            catch
            {
                statusImage.Image = null;
            }
        }
        private void FlushDns()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = System.Diagnostics.Process.Start(psi);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not flush DNS: " + ex.Message);
            }
        }
    }
}
