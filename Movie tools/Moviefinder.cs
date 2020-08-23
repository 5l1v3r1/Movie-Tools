using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Movie_tools
{
    public partial class Moviefinder : Form
    {
        public Moviefinder()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            movieslist = new List<string>();
        }

        private List<string> movieslist { get; set; }

        #region agent [ movie finder ]

        public async Task agent_run(bool sorter = false, string custompath = "")
        {
            try
            {
                if (sorter)
                {
                    await Task.Factory.StartNew(() =>
                     {
                         button2.Enabled = false;
                         agent_core_sort();
                     });
                }
                else
                {
                    await Task.Factory.StartNew(() =>
                     {
                         button1.Enabled = false;
                         agent_core(custompath);
                     });
                }
            }
            catch (Exception E)
            {
                log(E.Message);
            }
        }

        private void agent_core(string custompath = "")
        {
            try
            {
                label4.Text = "0";
                List<string> disks = new List<string>();
                if (custompath.Length > 5)
                {
                    disks = new List<string>() { custompath };
                }
                else
                {
                    disks = agent_getalldrives();
                }

                if (disks.Count > 0)
                {
                    info($"getting ready for {disks.Count} drives");
                    info($"آماده سازی برای {disks.Count} درایو");
                    List<string> fileslist = new List<string>(disks);
                    foreach (string item in disks)
                    {
                        fileslist.AddRange(agent_getfiles(item));
                        label4.Text = fileslist.Count.ToString();
                    }
                    info("file gathering finished");
                    info("پیدا کردن فایل ها به پایان رسید");
                    info("filtering movies");
                    info("فیلتر کردن فیلم ها");
                    agent_filterfiles(ref fileslist);
                    List<string[]> ordered = new List<string[]>();
                    foreach (string item in fileslist)
                    {
                        try
                        {
                            FileInfo fl = new FileInfo(item);
                            if (fl.Length < 100000000) continue;
                            string quality = "";
                            if (Regex.IsMatch(fl.Name, @"\d{3}[Pp]|\d{4}[Pp]"))
                            {
                                quality = Regex.Match(fl.Name, @"\d{3}[Pp]|\d{4}[Pp]").Value.ToString();
                            }
                            else
                            {
                                quality = "unset";
                            }
                            string[] data =
                            {
                                fl.Name,
                               ( fl.Length/1000000)+" MB",
                                quality,
                                item
                            };
                            ordered.Add(data);
                        }
                        catch
                        {
                        }
                    }
                    label5.Text = fileslist.Count.ToString();
                    info("movies filtering finished");
                    info("فیلتر کردن فیلم ها به پایان رسید");
                    ordered = ordered.OrderByDescending(func => long.Parse(func[1].Replace(" MB", ""))).ToList();
                    ordered.ForEach(func =>
                    {
                        movieslist.Add(func[func.Length - 1]);
                        listView1.Items.Add(new ListViewItem(func));
                    });
                }
                else
                {
                    info("we found no useable drives");
                    info("ما درایو قابل استفاده ای پیدا نکردیم");
                }
                button1.Enabled = true;
            }
            catch (Exception E)
            {
                log(E.Message);
            }
        }

        private void agent_core_sort()
        {
            try
            {
                Dictionary<string, string[]> series = new Dictionary<string, string[]>();
                Dictionary<string, string> othermovies = new Dictionary<string, string>();
                foreach (string item in movieslist)
                {
                    Regex seriesepisod = new Regex(@"([Ss](\d{2})[Ee](\d{2}))");
                    string flname = Path.GetFileNameWithoutExtension(item);
                    if (seriesepisod.IsMatch(item))
                    {
                        var regexed = seriesepisod.Match(flname);
                        string filename = Regex.Match(flname, @"(.*?)([Ss](\d{2})[Ee](\d{2}))").Groups[1].Value
                             .ToString()
                             .Replace(".", " ")
                             .Replace(@"\", "")
                             .Replace("/", "")
                             .Replace("-", " ");
                        filename = Regex.Replace(filename, @"\s{2,1000}", "").ToString();
                        if (char.IsWhiteSpace(filename[filename.Length - 1]))
                        {
                            filename = filename.Substring(0, filename.Length - 1);
                        }
                        string quality = "";
                        if (Regex.IsMatch(flname, @"\d{3}[Pp]|\d{4}[Pp]"))
                        {
                            quality = Regex.Match(flname, @"\d{3}[Pp]|\d{4}[Pp]").Value.ToString();
                        }
                        else
                        {
                            quality = "unset";
                        }
                        string[] infos =
                        {
                            quality,
                            regexed.Groups[2].Value,
                            regexed.Groups[3].Value,
                            filename
                        };
                        series.Add(item, infos);
                    }
                    else
                    {
                        var fl = new FileInfo(item);
                        string quality = "";
                        if (Regex.IsMatch(flname, @"\d{3}[Pp]|\d{4}[Pp]"))
                        {
                            quality = Regex.Match(flname, @"\d{3}[Pp]|\d{4}[Pp]").Value.ToString();
                        }
                        else
                        {
                            quality = "unset";
                        }
                        othermovies.Add(item, quality);
                    }
                }

                label7.Text = series.Count.ToString();
                foreach (var item in series.OrderByDescending(func => func.Value[3]))
                {
                    try
                    {
                        string finalfilename = agent_createdirectorytree(new string[]
                        {
                            "Series",
                            item.Value[3],
                            item.Value[1],
                            $"{item.Value[3]}-{item.Value[2]}-{item.Value[0]}" + Path.GetExtension(item.Key)
                        });
                        if (!File.Exists(finalfilename))
                        {
                            File.Move(item.Key, finalfilename);
                        }
                    }
                    catch (Exception E)
                    {
                        log(E.Message);
                    }
                }
                foreach (var item in othermovies.OrderByDescending(func => func.Value))
                {
                    try
                    {
                        string finalfilename = agent_createdirectorytree(new string[]
                          {
                            "Other",
                            item.Value,
                            Path.GetFileName(item.Key)
                          });
                        if (!File.Exists(finalfilename))
                        {
                            File.Move(item.Key, finalfilename);
                        }
                    }
                    catch (Exception E)
                    {
                        log(E.Message);
                    }
                }
                info("sorting finished");
                info("مرتب کردن به پایان رسید");
                button2.Enabled = true;
            }
            catch (Exception E)
            {
                log(E.Message);
            }
        }

        private string agent_createdirectorytree(string[] paths)
        {
            string res = paths.Aggregate((a, b) =>
            {
                var dir = a + @"\" + b;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            });
            return res;
        }

        private List<string> agent_getalldrives()
        {
            List<string> res = new List<string>();
            var drives = DriveInfo.GetDrives();
            foreach (var item in drives)
            {
                if (item.IsReady)
                {
                    res.Add(item.Name.ToString());
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
                    foreach (string items in targetformats)
                    {
                        if (item.ToLower().IndexOf(items) >= 0)
                        {
                            res.Add(item);
                            break;
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
                string path = "Movies [ MVTool ]";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                string finalfilename = path + @"\" + Path.GetFileNameWithoutExtension(filename) + Path.GetExtension(filename);
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

        #endregion agent [ movie finder ]

        private void log(string data)
        {
            using (StreamWriter st = File.AppendText("log[Movie finder].txt"))
            {
                st.WriteLine($"[{DateTime.Now.ToString("G")}] - {data}");
            }
        }

        private void info(string data)
        {
            richTextBox1.Text += $"[{DateTime.Now.ToString("G")}] - {data}" + Environment.NewLine;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await agent_run();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            if (movieslist.Count > 0)
            {
                await agent_run(true);
            }
            else
            {
                MessageBox.Show("Find movie files first | ابتدا فیلم هارا پیدا کنید");
            }
        }

        private void Moviefinder_Load(object sender, EventArgs e)
        {
        }
    }
}