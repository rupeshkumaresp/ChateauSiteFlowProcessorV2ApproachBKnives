using Microsoft.Exchange.WebServices.Autodiscover;
using System;
using System.Configuration;
using System.IO;
using System.Net;
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

        public static void SendMailWithAttachment(string eto, string subject, string message, string attachmentPath)
        {

            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("info@espautomation.co.uk");
                mailMessage.To.Add(new MailAddress(eto));
                mailMessage.Subject = subject;
                mailMessage.Attachments.Add(new Attachment(attachmentPath));
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = true;
                SmtpClient client = new SmtpClient();
                client.Credentials = new NetworkCredential("info@espautomation.co.uk", "LT4HP8vi#pg@Vb9^$-3R9q+e");
                client.Port = 587;
                client.Host = "smtp.office365.com";
                client.EnableSsl = true;
                client.Send(mailMessage);
            }
            catch (SmtpException exception)
            {
                string msg = "Mail cannot be sent (SmtpException):";
                msg += exception.Message;
                throw new Exception(msg);
            }

            catch (AutodiscoverRemoteException exception)
            {
                string msg = "Mail cannot be sent(AutodiscoverRemoteException):";
                msg += exception.Message;
                throw new Exception(msg);

            }

        }

        public static void SendMail(string eto, string subject, string message)
        {
            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress("info@espautomation.co.uk");
                mailMessage.To.Add(new MailAddress(eto));
                mailMessage.Subject = subject;
                mailMessage.Body = message;
                mailMessage.IsBodyHtml = true;
                SmtpClient client = new SmtpClient();
                client.Credentials = new NetworkCredential("info@espautomation.co.uk", "LT4HP8vi#pg@Vb9^$-3R9q+e");
                client.Port = 587;
                client.Host = "smtp.office365.com";
                client.EnableSsl = true;
                client.Send(mailMessage);
            }
            catch (SmtpException exception)
            {
                string msg = "Mail cannot be sent (SmtpException):";
                msg += exception.Message;
                throw new Exception(msg);
            }

            catch (AutodiscoverRemoteException exception)
            {
                string msg = "Mail cannot be sent(AutodiscoverRemoteException):";
                msg += exception.Message;
                throw new Exception(msg);

            }

        }

    }
}
