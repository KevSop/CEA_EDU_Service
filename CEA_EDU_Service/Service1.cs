using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Net;

namespace CEA_EDU_Service
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer timer;
        private System.Timers.Timer timer2;
        private Object lockObj = new object();

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Thread.Sleep(10000);

            double interval = 60000;
            string time = ConfigurationManager.AppSettings["time"];
            if (!string.IsNullOrWhiteSpace(time))
            {
                double.TryParse(time, out interval);
            }

            this.timer = new System.Timers.Timer();
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            this.timer.Interval = interval;
            this.timer.AutoReset = true;
            this.timer.Enabled = true;

            this.timer.Start();

            //点火web应用
            //this.timer2 = new System.Timers.Timer();
            //this.timer2.Elapsed += new System.Timers.ElapsedEventHandler(timer2_Elapsed);
            //this.timer2.Interval = 1000 * 60 * 10;
            //this.timer2.AutoReset = true;
            //this.timer2.Enabled = true;

            //this.timer2.Start();
        }

        protected override void OnStop()
        {
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string appPath = ConfigurationManager.AppSettings["AppPath"];
                if(!string.IsNullOrWhiteSpace(appPath))
                {
                    string fileName = Path.GetFileNameWithoutExtension(appPath);
                    Process[] proc = Process.GetProcessesByName(fileName); 
                    if (proc.Length == 0) 
                    {
                        lock (lockObj)
                        {
                            ProcessAsUser.Launch(appPath);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                //log 邮件
            }
        }

        void timer2_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                //Thread.Sleep(10000);

                string webAppUrl = ConfigurationManager.AppSettings["WebAppUrl"];
                if (!string.IsNullOrWhiteSpace(webAppUrl))
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(webAppUrl);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.KeepAlive = false;
                    request.Method = "get";

                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Stream stream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(stream);

                    sr.Close();
                    stream.Close();
                    response.Close();
                }

                TimeSpan ts = DateTime.Now - DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " 05:00:00");
                int totalSeconds = (int)ts.TotalSeconds * 1000;
                if (totalSeconds < timer2.Interval / 2 && totalSeconds > -timer2.Interval / 2)
                {
                    string appPath = ConfigurationManager.AppSettings["AppPath"];
                    if (!string.IsNullOrWhiteSpace(appPath))
                    {
                        string fileName = Path.GetFileNameWithoutExtension(appPath);
                        Process[] proc = Process.GetProcessesByName(fileName);
                        if (proc.Length > 0)
                        {
                            lock (lockObj)
                            {
                                ProcessAsUser.Launch("taskkill /f /t /im " + Path.GetFileName(appPath));
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //log 邮件
            }
        }
    }
}
