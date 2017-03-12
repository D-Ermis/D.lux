using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace dlux
{
    public partial class Form1 : Form
    {
        Dictionary<int, int> _todLookup; // time of day lookup

        RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        static string _defaultSettings = @"00:00	2700
06:00	4500
18:30	4000
20:00	2700";


        public Form1()
        {
            InitializeComponent();

            // AutoRun
            if (registryKey.GetValue("Dlux") == null)
            {
                chkRun.Checked = false;
            }
            else
            {
                chkRun.Checked = true;
            }

            tb1.Text = LoadSettings();
            try
            {
                _todLookup = Program.BuildTimeOfDayLookup(tb1.Text);
            }
            catch (Exception)
            {
                MessageBox.Show("Your setting file was corrupted. Reverting back to default...");
                tb1.Text = _defaultSettings;
                _todLookup = Program.BuildTimeOfDayLookup(tb1.Text);
                SaveSettings(tb1.Text);
            }
            finally
            {
                btnReload.Enabled = false;
            }


            // Update at the beginning then do it every hour.
            slider1.Value = _todLookup[(int)DateTime.Now.TimeOfDay.TotalSeconds];
        }

        private void ShowWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ShowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                notifyIcon1.Visible = true;
                Hide();
            }
            else if (FormWindowState.Normal == WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void NotifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            ShowWindow();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                slider1.Value = 6500;
                notifyIcon1.Visible = false;
            }
            catch (Exception ex2)
            {
                MessageBox.Show(ex2.Message);
            }
        }

        private void M_timer_Tick(object sender, EventArgs e)
        {
            slider1.Value = _todLookup[(int)DateTime.Now.TimeOfDay.TotalSeconds];
        }

        private void ChkTimer_Click(object sender, EventArgs e)
        {
            m_timer.Enabled = chkTimer.Checked;
            slider1.Value = _todLookup[(int)DateTime.Now.TimeOfDay.TotalSeconds];
        }

        private void ChkRun_Click(object sender, EventArgs e)
        {
            if (chkRun.Checked)
                registryKey.SetValue("Dlux", Application.ExecutablePath);
            else
                registryKey.DeleteValue("Dlux", false);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }

        private void TxtEditor_TextChanged(object sender, EventArgs e)
        {
            var vals = txtEditor.Text.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (vals.Length != 3) return;

            try
            {
                Program.SetGamma(
                    Convert.ToDouble(vals[0]),
                    Convert.ToDouble(vals[1]),
                    Convert.ToDouble(vals[2])
                    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void Slider1_ValueChanged(object sender, EventArgs e)
        {
            if (slider1 == null || this.intensity == null)
            {
                return;
            }

            double intensity = (slider1.Value - slider1.Minimum) / (double)(slider1.Maximum - slider1.Minimum);
            this.intensity.Text = intensity.ToString("N2");

            Program.Method5(intensity, out double red, out double green, out double blue);

            var rrrr = (float)Math.Round(red / 255, 4);
            var gggg = (float)Math.Round(green / 255, 4);
            var bbbb = (float)Math.Round(blue / 255, 4);

            txtEditor.Text = string.Format("{0:N3}\t{1:N3}\t{2:N3}", rrrr, gggg, bbbb);
            label3.Text = string.Format("{0}K", slider1.Value);
            notifyIcon1.Text = string.Format("D.lux - {0}K", slider1.Value);
        }

        private void Tb1_TextChanged(object sender, EventArgs e)
        {
            if (btnReload != null)
            {
                btnReload.Enabled = true;
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettings(tb1.Text);
                _todLookup = Program.BuildTimeOfDayLookup(tb1.Text);
                btnReload.Enabled = false;
            }
            catch (Exception)
            {

                MessageBox.Show("Error : Please respect default format.");
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            tb1.Text = _defaultSettings;
            SaveSettings(tb1.Text);
            _todLookup = Program.BuildTimeOfDayLookup(tb1.Text);
            btnReload.Enabled = false;
        }

        private static string GetSettingsFileName()
        {
            string folderName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dlux");
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
            return Path.Combine(folderName, "SavedSettings.txt"); ;
        }

        private static string LoadSettings()
        {
            var fileName = GetSettingsFileName();
            if (File.Exists(fileName))
            {
                // Load previously-saved settings
                using (var sr = new StreamReader(fileName))
                {
                    return sr.ReadToEnd();
                }
            }
            else
            {
                // Start with default settings
                return _defaultSettings;
            }
        }

        private static void SaveSettings(string program)
        {
            using (var sw = new StreamWriter(GetSettingsFileName()))
            {
                sw.Write(program);
            }
        }
    }
}