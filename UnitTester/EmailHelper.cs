using System;
using System.Configuration;
using System.Net.Mail;

namespace ChateauSiteFlowApp
{
    /// <summary>
    /// EMAIL ENGINE - SEND PROCESSING SUMMARY 
    /// </summary>
    public class EmailHelper
    {

        #region email Templates

        public static string ReportEmailTemplate =

            @"<p>
    	Hi,</p>
        <p>
	        Please find attached today's Chateau Knives order spreadsheet. </p>       
        
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

        public static void SendReportEmail(string path)
        {
            var defaultMessage = ReportEmailTemplate;

            var emailTo = ConfigurationManager.AppSettings["NotificationEmailKnives"];

            var emails = emailTo.Split(new char[] { ';' });

            for (int i = 0; i < emails.Length; i++)
            {
                if (string.IsNullOrEmpty(emails[i]))
                    continue;

                SendMailWithAttachment(emails[i], "Chateau Knives order Report - " + DateTime.Now.ToShortDateString(), defaultMessage, path);
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
