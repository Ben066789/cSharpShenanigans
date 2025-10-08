using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

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
        private Button btnAdd;
        private TextBox txtDomain;
        private PictureBox statusImage;
        private string hostsPath;

        private string enabledImagePath = @"C:\Users\COMLAB-PC\Documents\satono.jpg";
        private string disabledImagePath = @"C:\Users\COMLAB-PC\Documents\yearps.jpg";

        private readonly string appDataDir;
        private readonly string domainsFile;
        private List<string> savedDomains = new List<string>();
        private List<string> blockEntries = new List<string>();

        public Form1()
        {
            this.Text = "chicanerying";
            this.Width = 420;
            this.Height = 320;

            btnEnable = new Button() { Text = "Enable (Block)", Left = 110, Top = 30, Width = 180 };
            btnDisable = new Button() { Text = "Disable (Unblock)", Left = 110, Top = 70, Width = 180 };
            btnAdd = new Button() { Text = "Add Domain", Left = 300, Top = 110, Width = 90 };

            txtDomain = new TextBox()
            {
                Left = 30,
                Top = 110,
                Width = 260,
#if NET6_0_OR_GREATER
                PlaceholderText = "link must be: example.com"
#endif
            };

            statusImage = new PictureBox()
            {
                Left = 110,
                Top = 160,
                Width = 180,
                Height = 80,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnEnable.Click += BtnEnable_Click;
            btnDisable.Click += BtnDisable_Click;
            btnAdd.Click += BtnAdd_Click;

            this.Controls.Add(btnEnable);
            this.Controls.Add(btnDisable);
            this.Controls.Add(btnAdd);
            this.Controls.Add(txtDomain);
            this.Controls.Add(statusImage);

            hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

            appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chicanery");
            domainsFile = Path.Combine(appDataDir, "blocked_domains.txt");
            Directory.CreateDirectory(appDataDir);

            LoadSavedDomains();
            BuildBlockEntries();
            SetStatusImage(false);
        }

        private void LoadSavedDomains()
        {
            if (File.Exists(domainsFile))
            {
                var lines = File.ReadAllLines(domainsFile)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .Select(l => NormalizeDomain(l))
                                .Where(d => !string.IsNullOrEmpty(d))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                savedDomains = lines;
            }
            else
            {
                savedDomains = new List<string>();
            }

            if (!savedDomains.Contains("facebook.com", StringComparer.OrdinalIgnoreCase))
                savedDomains.Add("facebook.com");
        }

        private void BuildBlockEntries()
        {
            blockEntries.Clear();
            foreach (var d in savedDomains)
            {
                blockEntries.Add($"127.0.0.1 {d}");
                blockEntries.Add($"127.0.0.1 www.{d}");
                blockEntries.Add($"::1 {d}");
                blockEntries.Add($"::1 www.{d}");
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var raw = txtDomain.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                MessageBox.Show("link must be: example.com");
                return;
            }

            var domain = NormalizeDomain(raw);
            if (string.IsNullOrEmpty(domain))
            {
                MessageBox.Show("invalid input");
                return;
            }

            if (savedDomains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show($"{domain} already exist");
                txtDomain.Clear();
                return;
            }

            savedDomains.Add(domain);
            File.AppendAllLines(domainsFile, new[] { domain });
            blockEntries.Add($"127.0.0.1 {domain}");
            blockEntries.Add($"127.0.0.1 www.{domain}");
            blockEntries.Add($"::1 {domain}");
            blockEntries.Add($"::1 www.{domain}");

            MessageBox.Show($"added {domain} to blocklist");
            txtDomain.Clear();
        }

        private void BtnEnable_Click(object sender, EventArgs e)
        {
            try
            {
                var lines = File.ReadAllLines(hostsPath).ToList();
                var normalizedExisting = new HashSet<string>(lines.Select(NormalizeLine), StringComparer.OrdinalIgnoreCase);

                foreach (var entry in blockEntries)
                {
                    var n = NormalizeLine(entry);
                    if (!normalizedExisting.Contains(n))
                    {
                        lines.Add(entry);
                        normalizedExisting.Add(n);
                    }
                }

                File.WriteAllLines(hostsPath, lines);
                SetStatusImage(true);
                FlushDns();
                MessageBox.Show("added to hostfile");
            }
            catch (Exception ex)
            {
                MessageBox.Show("error: " + ex.Message);
            }
        }

        private void BtnDisable_Click(object sender, EventArgs e)
        {
            try
            {
                var lines = File.ReadAllLines(hostsPath).ToList();
                var toRemove = new HashSet<string>(blockEntries.Select(NormalizeLine), StringComparer.OrdinalIgnoreCase);
                var newLines = lines.Where(l => !toRemove.Contains(NormalizeLine(l))).ToList();
                File.WriteAllLines(hostsPath, newLines);
                SetStatusImage(false);
                FlushDns();
                MessageBox.Show("unblocked");
            }
            catch (Exception ex)
            {
                MessageBox.Show("error: " + ex.Message);
            }
        }

        private string NormalizeDomain(string input)
        {
            var d = input.Trim().ToLower();
            d = d.Replace("http://", "").Replace("https://", "");
            d = d.Split(new[] { '/', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
            if (d.StartsWith("www.")) d = d.Substring(4);
            if (!d.Contains('.')) return "";
            return d;
        }

        private string NormalizeLine(string line)
        {
            return Regex.Replace(line ?? "", @"\s+", " ").Trim();
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
