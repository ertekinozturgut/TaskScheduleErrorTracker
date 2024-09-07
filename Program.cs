using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;

namespace TaskScheduleErrorTracker
{
    internal class Program
    {
        static void Main(string[] args)
        {

            try
            {
                string logName = ConfigurationSettings.AppSettings["EventViewSource"];
                EventLogQuery query = new EventLogQuery(logName, PathType.LogName);

                EventLogReader logReader = new EventLogReader(query);
                List<string> errors = new List<string>();
                bool errorFound = false;
                for (EventRecord eventInstance = logReader.ReadEvent();
                     eventInstance != null;
                     eventInstance = logReader.ReadEvent())
                {
                    if (eventInstance.LevelDisplayName == "Error")
                    {
                        errors.Add($"Error Found:{JsonConvert.SerializeObject(eventInstance)}\n");
                        errorFound = true;
                        break;
                    }
                }

                if (errorFound)
                {
                    SendEmail("Task Scheduler Error", "Error at Task Scheduler\n"
                        + errors.Aggregate("", (x, y) => x + "\n" + y));
             
                }
                else
                {

                    Console.WriteLine("Not Found Any Error");
                }
            }
            catch (Exception e)
            {
                SendEmail("Task Scheduler Error Monitor Error", "Error at Task Scheduler Monitor App\n"
                    + JsonConvert.SerializeObject(e));
            }
            Console.ReadKey();
        }
        static void SendEmail(string subject, string body)
        {
            string fromAddress = ConfigurationSettings.AppSettings["FromEmail"];
            string toAddresses = ConfigurationSettings.AppSettings["ToEmails"];
            string smtpServer = ConfigurationSettings.AppSettings["SmtpServer"];
            string trackingServerName = ConfigurationSettings.AppSettings["TrackingServerName"];
            int smtpPort = int.Parse(ConfigurationSettings.AppSettings["SmtpPort"]);

            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(fromAddress);

            foreach (var toAddress in toAddresses.Split(';'))
            {
                mail.To.Add(toAddress);
            }
            mail.IsBodyHtml = true;

            mail.Subject = trackingServerName + " - " + subject;
            mail.Body = body;
            mail.Priority = MailPriority.High;
            
            SmtpClient smtp = new SmtpClient(smtpServer, smtpPort);
            smtp.Credentials = new System.Net.NetworkCredential(ConfigurationSettings.AppSettings["SmtpUser"], ConfigurationSettings.AppSettings["SmtpPassword"]);

            smtp.EnableSsl = Convert.ToBoolean(ConfigurationSettings.AppSettings["EnableSsl"]);
            
            try
            {
                smtp.Send(mail);
                Console.WriteLine("Email sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-mail did not send: {ex.Message}");
            }
        }
    }
}
