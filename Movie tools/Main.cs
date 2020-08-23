using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Movie_tools
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        #region agent [ movies copy creator ]

        private CancellationTokenSource agent_tokensource { get; set; }

        private void agent_run(CancellationToken token)
        {
            try
            {
                agentrunbutton.Text = "غیر فعال سازی | Stop";
                Task.Factory.StartNew(() =>
                {
                    agent_core(token);
                });
            }
            catch (Exception E)
            {
                log(E.Message);
            }
        }

        private async void agent_core(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    List<string> disks = agent_getremoveabledisks();
                    richTextBox1.Text = "";
                    if (disks.Count > 0)
                    {
                        List<string> allfiles = new List<string>();
                        foreach (string item in disks)
                        {
                            allfiles.AddRange(agent_getfiles(item));
                        }
                        agent_filterfiles(ref allfiles);
                        if (allfiles.Count > 0)
                        {
                            this.TopMost = true;
                            var oldstate = this.WindowState;
                            this.WindowState = FormWindowState.Normal;
                            info("getting ready for making copy of " + allfiles.Count + " files");
                            info("آماده سازی برای کپی " + allfiles.Count + " فایل");
                            foreach (string item in allfiles)
                            {
                                agent_copyfile(item);
                            }
                            Moviefinder mv = new Moviefinder();
                            await mv.agent_run(false, System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(System.Reflection.Assembly.GetExecutingAssembly().FullName, ""));
                            await mv.agent_run(true);
                            info("success, re searching for new devices");
                            info("عملیات موفقیت آمیز بود ، جستجو مجدد برای دستگاه های جدید");
                            this.TopMost = false;
                            this.WindowState = oldstate;
                        }
                        else
                        {
                            info("no movies, re searching for new devices");
                            info("هیچ فیلم، جستجو مجدد برای دستگاه های جدید");
                        }
                    }
                    await Task.Delay(5000);
                }
                catch (Exception E)
                {
                    log(E.Message);
                }
            }
            agentrunbutton.Text = "فعالسازی | Activate";
            info("stopped");
        }

        private List<string> agent_getremoveabledisks()
        {
            List<string> res = new List<string>();
            var drives = DriveInfo.GetDrives();
            foreach (var item in drives)
            {
                if (item.DriveType == DriveType.Removable)
                {
                    if (item.IsReady)
                    {
                        res.Add(item.Name.ToString());
                    }
                }
            }
            return res;
        }

        private List<string> agent_getfiles(string path)
        {
            List<string> files = new List<string>();
            try
            {
                var fllist = Directory.GetFiles(path);
                files.AddRange(fllist);
                var drlist = Directory.GetDirectories(path);
                foreach (var item in drlist)
                {
                    files.AddRange(agent_getfiles(item));
                }
            }
            catch
            {
            }
            return files;
        }

        private void agent_filterfiles(ref List<string> data)
        {
            List<string> targetformats = new List<string>()
            {
                ".mp4",".mkv",".avi",".flv"
            };
            List<string> res = new List<string>();
            foreach (string item in data)
            {
                try
                {
                    var ext = Path.GetExtension(item);
                    if (targetformats.Contains(ext))
                    {
                        FileInfo fl = new FileInfo(item);
                        if (fl.Length >= 90000000)
                        {
                            res.Add(item);
                        }
                    }
                }
                catch
                {
                }
            }
            data = res;
        }

        private void agent_copyfile(string filename)
        {
            try
            {
                string finalfilename = Path.GetFileNameWithoutExtension(filename) + Path.GetExtension(filename);
                if (!File.Exists(finalfilename))
                {
                    File.Copy(filename, finalfilename);
                }
            }
            catch (Exception E)
            {
                log(E.Message);
            }
        }

        #endregion agent [ movies copy creator ]

        private void log(string data)
        {
            using (StreamWriter st = File.AppendText("log[Copy agent].txt"))
            {
                st.WriteLine($"[{DateTime.Now.ToString("G")}] - {data}");
            }
        }

        private void info(string data)
        {
            richTextBox1.Text += $"[{ DateTime.Now.ToString("G")}] - {data}" + Environment.NewLine;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (agentrunbutton.Text.Contains("Stop"))
            {
                agent_tokensource.Cancel();
            }
            else
            {
                agent_tokensource = new CancellationTokenSource();
                agent_run(agent_tokensource.Token);
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            // adding softwre to startup
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                key.SetValue("MVTools", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Moviefinder mv = new Moviefinder();
            mv.Show();
        }
    }
}