using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Drawing;

namespace Get_Info_to_mail
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private bool checkStop = true;
        private int time = 0;
        private int n = 0;
        private int delayMin;
        private int delayMax;
        private int delayNghi;
        private int countNghi;
        private int demSuccess = 0;
        private int demMiss = 0;
        private int maxThread = 1;
        private int countMail = 0;
        private string Token = "";
        private string dtsg = "";
        private string idUser = "";
        private string idPg = "";
        private List<string> lsMail = new List<string>();
        private List<User> lsUser = new List<User>();
        private WebClient setHttp(string cookie)
        {
            WebClient _WC = new WebClient();
            _WC.Headers.Add(HttpRequestHeader.Cookie, cookie);
            _WC.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.135 Safari/537.36");
            return _WC;
        }
        private string getToken()
        {
            WebClient _http = setHttp(txtCookie.Text);
            string data = _http.DownloadString("https://m.facebook.com/composer/ocelot/async_loader/?publisher=feed");
            var reg = Regex.Matches(data, @"(?<=(accessToken\\"":\\"")).*?(?=\\)", RegexOptions.Multiline);
            if (reg != null && reg.Count > 0)
            {
                data = reg[0].ToString();
            }
            return data;
        }
        private string getIDUser(string data)
        {
            string id = "";
            var reg = Regex.Matches(data, @"(?<=(c_user=)).*?(?=;)", RegexOptions.Multiline);
            if (reg != null && reg.Count > 0)
            {
                id = reg[0].ToString();
            }
            return id;
        }
        static string getDtsg(string data)
        {
            var reg = Regex.Matches(data, @"(?<=(""token"":"")).*?(?="")", RegexOptions.Multiline);
            string token = "";
            if (reg != null && reg.Count > 0)
            {
                token = reg[0].ToString();
            }
            return token;
        }
        static string getIdEmail(string data)
        {
            var reg = Regex.Matches(data, @"(?<=(""uid"":"")).*?(?="")", RegexOptions.Multiline);
            string token = "";
            if (reg != null && reg.Count > 0)
            {
                token = reg[0].ToString();

            }
            return token;
        }
        static string getNameEmail(string data)
        {
            var reg = Regex.Matches(data, @"(?<=(""text"":"")).*?(?="")", RegexOptions.Multiline);
            string token = "";
            if (reg != null && reg.Count > 0)
            {
                token = reg[0].ToString();
            }
            return token;
        }
        private User getUser(string id, string mail, string data)
        {
            User user = new User();
            JObject rss = JObject.Parse(data);
            try
            {
                user.uid = id;
                user.mail = mail;
                user.sex = (string)rss["gender"];
                user.name = (string)rss["name"];
                user.location = (string)rss["location"]["name"];
                user.date = (string)rss["birthday"];
            }
            catch (Exception)
            {
            }
            return user;
        }
        private string selectIDPage(string data)
        {
            var reg = Regex.Matches(data, @"(?<=page_id=).*?(?=&amp)", RegexOptions.Multiline);
            string token = "";
            if (reg != null && reg.Count > 0)
            {
                token = reg[0].ToString();
            }
            return token;
        }
        private void getIdPage()
        {
            if (this.txtCookie.Text != "")
            {
                Thread t = new Thread((ThreadStart)(() =>
                {
                    try
                    {
                        idUser = getIDUser(txtCookie.Text);
                        WebClient _http = setHttp(txtCookie.Text);
                        string data = _http.DownloadString("https://mbasic.facebook.com/pages/pin/setting/");
                        idPg = selectIDPage(data);
                        if (string.IsNullOrEmpty(idPg))
                        {
                            txtStatus.Invoke(new MethodInvoker(delegate ()
                            {
                                this.txtStatus.Text = "Cookie die, hoặc tài khoản chưa có page";
                            }));
                        }
                        else
                        {
                            data = _http.DownloadString("https://m.facebook.com/pages/edit/admins/" + idPg);
                            dtsg = getDtsg(data);
                            Token = getToken();
                            if (string.IsNullOrEmpty(dtsg))
                                getIdPage();
                            txtStatus.Invoke(new MethodInvoker(delegate ()
                            {
                                this.btnStart.Enabled = true;
                                this.txtStatus.Text = "Cookie done";
                            }));
                        }
                    }
                    catch (Exception)
                    {
                        txtStatus.Invoke(new MethodInvoker(delegate ()
                        {
                            this.txtStatus.Text = "Hãy nhập lại cookie";
                        }));
                    }

                }));
                t.Start();
                t.Join();
            }
        }
        private void txtCookie_TextChanged(object sender, EventArgs e)
        {
            Thread d = new Thread(new ThreadStart(getIdPage));
            d.Start();
            d.IsBackground = true;
        }
        private void addMail()
        {
            Hashtable myHash = new Hashtable();
            string[] line = { };
            Thread a = new Thread((ThreadStart)(() =>
            {
                OpenFileDialog open = new OpenFileDialog();
                open.Filter = "|*.txt";
                if (open.ShowDialog() == DialogResult.OK)
                {
                    line = File.ReadAllLines(open.FileName);
                    if (line.Length == 0)
                        myHash.Add(0, null);
                    else
                    {
                        for (int i = 0; i < line.Length; i++)
                        {
                            if (myHash.ContainsValue(line[i]) == false)
                                myHash.Add(i, line[i]);
                        }
                        countMail += myHash.Count;
                    }
                }
            }));
            a.SetApartmentState(ApartmentState.STA);
            a.Start();
            a.Join();
            foreach (DictionaryEntry item in myHash)
            {
                try
                {
                    lsMail.Add(item.Value.ToString());
                    dgv.Invoke(new MethodInvoker(delegate ()
                    {
                        this.dgv.Rows.Add(item.Value.ToString());
                    }));
                }
                catch (Exception)
                {
                }
            }
        }
        private bool checkMailLive(string data)
        {
            JObject rss = JObject.Parse(data);
            if (rss["smtp_check"].ToString() == "True")
                return true;
            else
                return false;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Thread d = new Thread(new ThreadStart(addMail));
            d.Start();
            d.IsBackground = true;
        }
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            maxThread = trackBar1.Value;
            lbMaxThread.Text = "Max Thread: " + maxThread;
        }
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                this.btnStart.Enabled = false;
                checkStop = false;
                delayMin = (int)NUD_DelayMin.Value*1000;
                delayMax = (int)NUD_DelayMax.Value*1000;
                delayNghi = (int)NUD_delayNghi.Value * 1000;
                countNghi = (int)NUD_rowNghi.Value;
                if (delayMin > delayMax)
                {
                    this.txtStatus.Text = "Yêu cầu chọn lại delay!";
                    this.btnStart.Enabled = true;
                    return;
                }
                backgroundGetInfo.RunWorkerAsync();                
            }
            catch (Exception)
            {
            }
        }

        public void backgroundGetInfo_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {            
            List<Thread> lThreads = new List<Thread>();
            int autodem = n;
            int demSleep=0;
            for (; n < lsMail.Count; n++)
            {    
                Invoke(new MethodInvoker(delegate ()
                {
                    maxThread = trackBar1.Value;
                    this.txtStatus.Text = "Đang thực hiện.";
                }));
                for (int i = 0; i < maxThread; i++)
                {
                    if (demSleep == countNghi)
                    {
                        Invoke(new MethodInvoker(delegate ()
                        {
                            this.txtStatus.Text = "Đang thực hiện nghỉ " + delayNghi / 1000 + " giây sau " + countNghi + " lần check";
                        }));
                        Thread.Sleep(delayNghi);
                        demSleep = 0;
                    }
                    if (checkStop == true)
                        return;
                    n = autodem;
                    Random rd = new Random();
                    if (autodem == lsMail.Count)
                    {
                        break;
                    }
                    string mail = lsMail[autodem];                    
                    Invoke(new MethodInvoker(delegate ()
                    {
                        this.dgv.Rows[n].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#5c9572");
                    }));
                    Thread sub = new Thread(this.checkMail(mail,rd.Next(delayMin,delayMax)));
                    demSleep++;
                    sub.IsBackground = true;
                    sub.Start();
                    lThreads.Add(sub);
                    autodem++;
                    Thread.Sleep(100);
                }
                foreach (Thread machineThread in lThreads)
                {
                    machineThread.Join();
                }
                lThreads.Clear();
            }
            n = 0;
            Invoke(new MethodInvoker(delegate ()
            {
                this.txtStatus.Text = "Đã Xong";
                this.btnStart.Enabled = true;
            }));            
        }
        private ThreadStart checkMail(string mail,int sleep)
        {
            return  delegate
            {
                WebClient _WC = new WebClient();
                _WC = setHttp(txtCookie.Text);
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                bool check = true;                
                try
                {
                    string h = "https://m.facebook.com/presma/user_search_typeahead/?search_mode=ANYONE_EXCEPT_VERIFIED_ACCOUNT&q=" + mail.ToString() + "&fb_dtsg_ag=" + dtsg + "&__user=" + idUser;
                    Thread.Sleep(sleep);                    
                    string b = _WC.DownloadString(h);
                    if (checkApi(b))
                        check = false;
                    User user = new User();
                    string id = getIdEmail(b);
                    string l = _WC.DownloadString("https://graph.facebook.com/v7.0/" + id + "?fields=name,gender,birthday,location&access_token=" + Token);
                    user = getUser(getIdEmail(b), mail.ToString(), l);
                    user.mailLive= checkMailLive(_WC.DownloadString("https://thakkaha.dev.fast.sheridanc.on.ca/pme/email-status/status.php?address=" + mail));
                    Invoke(new MethodInvoker(delegate ()
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.Cells[0].Value.Equals(mail))
                            {
                                row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#7db6e1");
                                demSuccess++;
                                lsUser.Add(user);
                                row.Cells[1].Value = user.mailLive.ToString();
                                row.Cells[2].Value = user.uid;                                
                                row.Cells[3].Value = user.name;
                                row.Cells[4].Value = user.date;
                                row.Cells[5].Value = user.sex;
                                row.Cells[6].Value = user.location;
                                row.Cells[7].Value = "Done";
                                break;
                            }
                        }
                    }));
                }
                catch (Exception)
                {
                    Invoke(new MethodInvoker(delegate ()
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.Cells[0].Value.Equals(mail))
                            {
                                row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#fbda72");
                                if (check == true)
                                    row.Cells[6].Value = "Mail chưa đăng ký facebook";
                                else
                                {
                                    row.Cells[6].Value = "Cookie bị block tính năng";
                                }
                                demMiss++;
                                break;
                            }
                        }
                    }));
                }
            };
        }
        private bool checkApi(string data)
        {
            var reg = Regex.Matches(data, @"!DOCTYPE html", RegexOptions.Multiline);
            string token = "";
            if (reg != null && reg.Count > 0)
            {
                token = reg[0].ToString();
            }
            return token == "!DOCTYPE html";
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            this.time++;
            this.Text = (this.Text = string.Concat(new object[]
            {
                "[CHECK FB REG EMAIL_By Vũ Senpai]    Thời gian :",
                TimeSpan.FromSeconds((double)this.time).ToString(),
                "  Tổng mail ",
                this.lsMail.Count,
                "  God ",
                this.demSuccess,
                "  Bad ",
                this.demMiss
            }));
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            DialogResult Result = MessageBox.Show("Bạn muốn lưu lại list mail done?", "Mail List", MessageBoxButtons.YesNo);
            if (Result == DialogResult.Yes)
            {
                SaveMail();
            }
            lsUser.Clear();
            dgv.Rows.Clear();
            demMiss = 0;
            n = 0;
            demSuccess = 0;
            lsMail.Clear();

        }

        private void btnGetMail_Click(object sender, EventArgs e)
        {
            SaveMail();
        }
        private void SaveMail()
        {
            if (this.lsUser.Count > 0)
            {
                SaveFileDialog _SFD = new SaveFileDialog();
                _SFD.FileName = "Check Mail Done" + demSuccess + ".txt";
                _SFD.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                if (_SFD.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter _SW = new StreamWriter(_SFD.FileName))
                        foreach (User Item in lsUser)
                        {
                            string a = Item.mail + "|" + Item.uid + "|" + Item.name + "|" + Item.sex + "|" + Item.date + "|" + Item.location;
                            _SW.WriteLine(a);
                        }
                }
                this.txtStatus.Text = " Mail have been saved!";
            }
            else
            {
                this.txtStatus.Text = "Cannot save zero Mail!";
            }
        }
        private void button2_Click_1(object sender, EventArgs e)
        {
            checkStop = true;
            this.txtStatus.Text = "Đã dừng chương trình";
            this.btnStart.Enabled = true;
        }
    }
}
