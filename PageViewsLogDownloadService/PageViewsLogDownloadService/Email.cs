// -----------------------------------------------------------------------
// <copyright file="Email.cs" company="MSIT">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PageViewsLogDownloadService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Mail;
    using System.Globalization;
    using System.Net;
    using System.Configuration;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class MailSender
    {
        private static string SmtpHost = ConfigurationManager.AppSettings["SmtpHost"];
        public static int SendTimeOut = 100000;
        public static int SendRetryTimes = 3;

        static public void GenerateAndSendMail(string errorMessage)
        {
            string mailContent = errorMessage;

            List<string> summaryMailToList = new List<string>();
            List<string> summaryMailCcList = new List<string>();
            string adminList = ConfigurationManager.AppSettings["AdminList"];
            string userName = ConfigurationManager.AppSettings["UserName"];
            string password = ConfigurationManager.AppSettings["Password"];
            string domain = ConfigurationManager.AppSettings["Domain"];
            summaryMailToList.AddRange(adminList.Split(';').ToList());

            MailSender.SendMail(
                    summaryMailToList,
                    summaryMailCcList,
                    String.Format("DashBoard error message"),
                    mailContent,
                    userName,
                    password,
                    domain);
        }

        static public void GenerateAndSendMail(string errorMessage, string title)
        {
            string mailContent = errorMessage;

            List<string> summaryMailToList = new List<string>();
            List<string> summaryMailCcList = new List<string>();
            string adminList = ConfigurationManager.AppSettings["AdminList"];
            string userName = ConfigurationManager.AppSettings["UserName"];
            string password = ConfigurationManager.AppSettings["Password"];
            string domain = ConfigurationManager.AppSettings["Domain"];
            summaryMailToList.AddRange(adminList.Split(';').ToList());

            MailSender.SendMail(
                    summaryMailToList,
                    summaryMailCcList,
                    String.Format(title),
                    mailContent,
                    userName,
                    password,
                    domain);
        }

        static public void SendMail(List<string> to, List<string> cc
                    , string subject, string bodyHtml, string MailUserName, string MailPassword, string MailDomain)
        {
            SendMail(to, cc, subject, bodyHtml, null, MailUserName, MailPassword, MailDomain);
        }

        static public void SendMail(List<string> to, List<string> cc
            , string subject, string bodyHtml, string messageAttachPath, string MailUserName, string MailPassword, string MailDomain)
        {
            SendMail(to, cc, subject, bodyHtml, null, null, MailUserName, MailPassword, MailDomain);
        }

        static public void SendMail(List<string> to, List<string> cc
            , string subject, string bodyHtml, string messageAttachPath, AlternateView htmlBody, string MailUserName, string MailPassword, string MailDomain)
        {
            MailMessage mail = new MailMessage();
            string mailPassword = MailPassword;
            string mailDomain = MailDomain;

            if (!string.IsNullOrEmpty(MailUserName))
            {
                //full email address or alias
                if (!MailUserName.Contains('@'))
                    mail.From = new MailAddress(string.Format(CultureInfo.InvariantCulture, "{0}@microsoft.com", MailUserName));
                else
                    mail.From = new MailAddress(MailUserName);
            }
            if ((to != null) && (to.Count != 0))
            {
                IEnumerable<string> uto = to.Distinct<string>();
                foreach (string toStr in uto)
                {
                    if (!string.IsNullOrEmpty(toStr))
                    {
                        //full email address or alias
                        if (!toStr.Contains('@'))
                            mail.To.Add(new MailAddress(string.Format(CultureInfo.InvariantCulture, "{0}@microsoft.com", toStr)));
                        else
                            mail.To.Add(new MailAddress(toStr));
                    }
                }
            }
            else
            {
                //if To is empty, return w/o sending the email.
                return;
            }
            if ((cc != null) && (cc.Count != 0))
            {
                IEnumerable<string> ucc = cc.Distinct<string>();
                foreach (string ccStr in ucc)
                {
                    if (!string.IsNullOrEmpty(ccStr))
                    {
                        //full email address or alias
                        if (!ccStr.Contains('@'))
                            mail.CC.Add(new MailAddress(string.Format(CultureInfo.InvariantCulture, "{0}@microsoft.com", ccStr)));
                        else
                            mail.CC.Add(new MailAddress(ccStr));
                    }
                }
            }
            mail.Subject = subject;
            mail.Body = bodyHtml;
            mail.IsBodyHtml = true;

            if (!string.IsNullOrEmpty(messageAttachPath))
                mail.Attachments.Add(new Attachment(messageAttachPath));
            if (htmlBody != null)
            {
                mail.AlternateViews.Add(htmlBody);
            }
            SmtpClient smtpClient = new SmtpClient(SmtpHost);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.EnableSsl = true;
            if (string.IsNullOrEmpty(mailDomain))
            {
                smtpClient.Credentials = new NetworkCredential(MailUserName, mailPassword);
            }
            else
            {
                smtpClient.Credentials = new NetworkCredential(MailUserName, mailPassword, mailDomain);
            }
            smtpClient.Timeout = SendTimeOut;

            for (int i = 1; i <= SendRetryTimes; i++)
            {
                try
                {
                    bool sendRet;
                    if (i == SendRetryTimes)
                        sendRet = SendMailRequest(smtpClient, mail, true);
                    else
                        sendRet = SendMailRequest(smtpClient, mail, false);

                    if (sendRet)
                        break;
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Send log mail failed.", e);
                }
            }
        }

        static private bool SendMailRequest(SmtpClient smtpClient, MailMessage mail, bool throwException)
        {
            try
            {
                smtpClient.Send(mail);
                return true;
            }
            catch (Exception e)
            {
                if (throwException)
                    throw new InvalidOperationException("Send log mail failed.", e);
                else
                    return false;
            }
        }
    }
}