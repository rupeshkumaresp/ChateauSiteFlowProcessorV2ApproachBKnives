using System;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace PicsMeSiteFlowApp
{
    /// <summary>
    /// EMAIL ENGINE - SEND PROCESSING SUMMARY 
    /// </summary>
    public class EmailHelper
    {

        #region email Templates


      
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
