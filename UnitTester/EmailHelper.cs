using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace ChateauSiteFlowApp
{
    /// <summary>
    /// EMAIL ENGINE - SEND PROCESSING SUMMARY 
    /// </summary>
    public class EmailHelper
    {

        #region email Templates


        public static string ReportEmailTemplatePreOrder =

            @"<p>
    	Hi,</p>
        <p>
	        Please find attached today's Pre-Orders spreadsheet. </p>       
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";

        public static string ReportEmailTemplateBelfield =

            @"<p>
    	Hi,</p>
        <p>
	        Please find attached today's Chateau Belfield order spreadsheet. </p>       
        
           <p>
	        Please connect to SFTP to access the imposed PDF file. </p>       
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";

        public static string ErrorEmailTemplateBelfieldNoImpositions =

            @"<p>
    	Hi,</p>
        <p>
	        Please note we have not got output from Prinergy for Belfield orders. The service waited around an hour and still no impostions got generated.</p>       
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";

        public static string ErrorEmailTemplateBelfield =

            @"<p>
    	Hi,</p>
        <p>
	        An error occured processing the Belfield order input:</p>       
        <p>
	        [ERRORSTATUS]</p>
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";


        public static string ReportEmailTemplateKnives =

            @"<p>
    	Hi,</p>
        <p>
	        Chateau Knives report has been copied to FTP. Please login and access the report from FTP. </p>       
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";

        public static string ProcessingStatusSummaryEmailTemplate =

        @"<p>
    	Hi,</p>
        <p>
	        Please find below processing summary for input orders:</p>       
        <p>
	        [ORDERSTATUS]</p>
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";


        public static string ProcessingStatusSummaryWelcomeCardsEmailTemplate =

            @"<p>
    	Hi,</p>
        <p>
	        Please find below processing summary for Chateau Welcome Card orders:</p>       
        <p>
	        [ORDERSTATUS]</p>
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";


        public static string MissingJsonEmailTemplate =

            @"<p>
    	Hi,</p>
        <p>
	        Action Needed: We have only received PDFs for below order, No Json file found :</p>       
        <p>
	        [FILENAME]</p>
        
        <p>
	        &nbsp;</p>
        <p>
	        Kind Regards,</p>
        <p>
	        ESP Team</p>
        <p>
	        <span style='color:#696969;'><em><span style='font-size: 12px;'>Please note this is an automated response email, please do not reply to this email address.</span></em></span><br /></em></span></p>";



        #endregion

        public static void SendMail(string eto, string subject, string message)
        {
            try
            {

                var priority = MailPriority.Normal;

                MailMessage mailer = new MailMessage("info@espweb2print.co.uk", eto, subject, message);

                SmtpClient smtp = new SmtpClient("espcolour-co-uk.mail.protection.outlook.com");
                mailer.IsBodyHtml = true;
                mailer.Priority = priority;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = null;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(mailer);

            }
            catch (Exception)
            {
                //LOG IT TO A LOG FILE
            }

        }

        public static void SendPreOrderReportEmail(string path)
        {
            var defaultMessage = ReportEmailTemplatePreOrder;

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailPreOrder"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Pre-Order Report - " + DateTime.Now.ToShortDateString(), defaultMessage, path);
            }

        }


        public static void SendKnivesReportEmail(string path)
        {
            var defaultMessage = ReportEmailTemplateKnives;

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailKnives"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Knives order Report - " + DateTime.Now.ToShortDateString(), defaultMessage, "");
            }

        }

        public static void SendBelfieldReportEmail(string path, string mergedLabel)
        {
            var defaultMessage = ReportEmailTemplateBelfield;

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailBelfield"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Belfield order Report - " + DateTime.Now.ToShortDateString(), defaultMessage, path, mergedLabel);
            }

        }

        public static void SendBelfieldErrorEmail(string path, string innerException)
        {
            var defaultMessage = ErrorEmailTemplateBelfield;

            defaultMessage = Regex.Replace(defaultMessage, "\\[ERRORSTATUS\\]", innerException);

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailBelfield"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Belfield Error - Action needed " + DateTime.Now.ToShortDateString(), defaultMessage, path);
            }
        }

        public static void SendBelfieldNoImpositionsErrorEmail(string path)
        {
            var defaultMessage = ErrorEmailTemplateBelfieldNoImpositions;

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailBelfield"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Belfield Error - No Prinergy Impostions- " + DateTime.Now.ToShortDateString(), defaultMessage, path);
            }

        }

        public static void SendMailWithAttachment(string eto, string subject, string message, string attachmentPath1, string attachmentPath2)
        {
            try
            {
                if (string.IsNullOrEmpty(eto))
                    return;
                var priority = MailPriority.Normal;

                MailMessage mailer = new MailMessage("info@espweb2print.co.uk", eto, subject, message);

                if (!string.IsNullOrEmpty(attachmentPath1))
                    mailer.Attachments.Add(new Attachment(attachmentPath1));

                if (!string.IsNullOrEmpty(attachmentPath2))
                {
                    if (File.Exists(attachmentPath2))
                        mailer.Attachments.Add(new Attachment(attachmentPath2));
                }


                SmtpClient smtp = new SmtpClient("espcolour-co-uk.mail.protection.outlook.com");
                mailer.IsBodyHtml = true;
                mailer.Priority = priority;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = null;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(mailer);

            }
            catch (Exception)
            {
                //LOG IT TO A LOG FILE
            }

        }

        public static void SendMailWithAttachment(string eto, string subject, string message, string attachmentPath)
        {
            try
            {
                if (string.IsNullOrEmpty(eto))
                    return;
                var priority = MailPriority.Normal;

                MailMessage mailer = new MailMessage("info@espweb2print.co.uk", eto, subject, message);

                if (!string.IsNullOrEmpty(attachmentPath))
                    mailer.Attachments.Add(new Attachment(attachmentPath));

                SmtpClient smtp = new SmtpClient("espcolour-co-uk.mail.protection.outlook.com");
                mailer.IsBodyHtml = true;
                mailer.Priority = priority;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = null;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(mailer);

            }
            catch (Exception)
            {
                //LOG IT TO A LOG FILE
            }

        }

    }
}
