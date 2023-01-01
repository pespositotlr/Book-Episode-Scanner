using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;

namespace BookEpisodeScanner.Utilities
{
    public static class EmailHelper
    {
        public static void SendNotificationEmailError(IConfigurationRoot config, string subjectArea, string errorMessage)
        {
            string subject = "Error message in Website Status Checker " + subjectArea;
            string body = "Error message in Website Status Checker " + subjectArea + ": " + errorMessage + " The current time is: " + DateTime.Now.ToString();
            SendNotificationEmail(config, subject, body);
        }

        public static void SendNotificationEmailBookFound(IConfigurationRoot config, string bookId, string currentEpisodeId)
        {
            string subject = "Successfully found " + currentEpisodeId;
            string body = "Successfully found episode " + currentEpisodeId + " of bookId " + bookId + ". It's currently being downloaded. The current time is: " + DateTime.Now.ToString();
            SendNotificationEmail(config, subject, body);
        }

        static void SendNotificationEmail(IConfigurationRoot config, string subject, string body)
        {
            MailAddress _from = new MailAddress(config["emailAddress"]);
            MailAddress _to = new MailAddress(config["emailAddress"]);
            SmtpClient smtp = new SmtpClient(config["smtpClient"]);
            smtp.UseDefaultCredentials = false;
            smtp.EnableSsl = true;
            smtp.Credentials = new NetworkCredential(config["emailAddress"], config["emailAddressPassword"]);
            smtp.Port = 587;
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            MailMessage msgMail = new MailMessage();

            msgMail.From = _from;
            msgMail.To.Add(_to);
            msgMail.Subject = subject;
            msgMail.Body = body;
            msgMail.IsBodyHtml = false;
            try
            {
                smtp.Send(msgMail);
            }
            catch
            {
                throw;
            }

            msgMail.Dispose();
        }
    }
}
