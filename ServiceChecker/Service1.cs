using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Timers;

namespace ServiceChecker
{
    public partial class Service1 : ServiceBase
    {
        Timer timer = new Timer();
        string[] services, receivers;
        string senderMail, senderPass;
        public Service1()
        {
            try
            {

                InitializeComponent();
                string interval = ConfigurationSettings.AppSettings.Get("interval");
                timer.Interval = Convert.ToDouble(interval) * 1000;

                string[] serviceKeys = ConfigurationSettings.AppSettings.AllKeys.Where(key => key.Contains("service")).ToArray();
                services = new string[serviceKeys.Length];

                for (int i = 0; i < services.Length; i++)
                {
                    services[i] = ConfigurationSettings.AppSettings.Get(serviceKeys[i]);
                }

                senderMail = ConfigurationSettings.AppSettings.Get("senderMail");
                senderPass = ConfigurationSettings.AppSettings.Get("senderMailPass");

                string[] receiverKeys = ConfigurationSettings.AppSettings.AllKeys.Where(key => key.Contains("receiverMail")).ToArray();
                receivers = new string[receiverKeys.Length];

                for (int i = 0; i < receivers.Length; i++)
                {
                    receivers[i] = ConfigurationSettings.AppSettings.Get(receiverKeys[i]);
                }

            }
            catch (Exception ex)
            {
                Log(DateTime.Now.ToString() + " *** " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        protected override void OnStart(string[] args)
        {
            Log("Service Checker is started at " + DateTime.Now.ToString());
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (services.Length > 0)
                {
                    for (int i = 0; i < services.Length; i++)
                    {
                        ServiceController sc = new ServiceController(services[i]);
                        if (sc.Status == ServiceControllerStatus.Stopped)
                        {
                            SendEmail(services[i] + " is stopped at " + DateTime.Now.ToString());
                        }
                        else if (sc.Status == ServiceControllerStatus.Paused)
                        {
                            SendEmail(services[i] + " is paused at " + DateTime.Now.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(DateTime.Now.ToString() + " *** " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        protected override void OnStop()
        {
            Log("Service Checker is stopped at " + DateTime.Now.ToString());
        }

        private void SendEmail(string mailBody)
        {
            using (var message = new MailMessage())
            {
                var fromAddress = new MailAddress(senderMail, "Service Checker");
                string fromPassword = senderPass;
                message.Subject = "Some of the services have stopped or paused!";
                message.Body = mailBody;
                message.From = fromAddress;

                for (int i = 0; i < receivers.Length; i++)
                {
                    message.To.Add(receivers[i]);
                }

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                smtp.Send(message);
            }
        }

        private void Log(string message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceCheckerLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(message);
                }
            }

        }
    }
}
