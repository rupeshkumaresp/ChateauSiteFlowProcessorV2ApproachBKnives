using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using PicsMeEntity.Entity;
using PicsMeEntity.MediaClipEntity;
using PicsMeOrderHelper;
using PicsMeOrderHelper.Model;
using iTextSharp.text;
using Newtonsoft.Json;
using SiteFlowHelper;
using SpreadsheetReaderLibrary;

namespace PicsMeSiteFlowApp
{

    /// <summary>
    /// HELPER CLASS - DOWNLOAD ORDER, READ JSON AND UPDATE JSON, CREATE ORDER AND SEND TO SITE FLOW
    /// </summary>
    public class ProcessHelper
    {
        readonly OrderHelper _orderHelper = new OrderHelper();
        SiteFlowEngine _siteflowEngine;
        private static readonly string BaseUrlSiteFlow = ConfigurationManager.AppSettings["BaseUrlSiteFlow"];
        private static readonly string SiteflowKey = ConfigurationManager.AppSettings["SiteflowKey"];
        private static readonly string SiteflowSecretKey = ConfigurationManager.AppSettings["SiteflowSecretKey"];
        MediaClipEntities _mediaClipEntities = new MediaClipEntities();


        readonly string _localProcessingPath = ConfigurationManager.AppSettings["WorkingDirectory"] +
                                               ConfigurationManager.AppSettings["ServiceFolderPath"];

        private readonly PdfModificationHelper _pdfModificationHelper;

        public ProcessHelper()
        {
            _pdfModificationHelper = new PdfModificationHelper();
        }

        public static SiteflowOrder.RootObject ReadJsonFile(FileInfo jsonFile, ref string json)
        {
            SiteflowOrder.RootObject jsonObject;

            using (StreamReader r = new StreamReader(jsonFile.FullName))
            {
                json = r.ReadToEnd();
                json = json.Replace("\"" + "error" + "\"" + ":[],", "");
                json = json.Replace("\"" + "stockItems" + "\"" + ":[],", "");
                json = json.Replace("\"" + "attributes" + "\"" + ":[],", "");
                json = json.Replace("\"" + "extraData" + "\"" + ": [],", "");
                json = json.Replace("\"" + "extraData" + "\"" + ":[],", "");
                jsonObject = JsonConvert.DeserializeObject<SiteflowOrder.RootObject>(json);
            }

            return jsonObject;
        }

        public bool DownloadPdf(string url, string filename)
        {
            bool success = true;

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(url, filename);

            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }

        public static void DownloadOrders()
        {
            var inputPathJson = ConfigurationManager.AppSettings["SFTP_path"];
            var inputPathPdf = ConfigurationManager.AppSettings["SFTP_path"];

            var processedFolderPath = ConfigurationManager.AppSettings["SFTP_path_Processed"];


            var localpath = ConfigurationManager.AppSettings["WorkingDirectory"] +
                            ConfigurationManager.AppSettings["ServiceFolderPath"];
            var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

            var pdfFiles = new DirectoryInfo(inputPathPdf).GetFiles("*.pdf");
            var jsonFiles = new DirectoryInfo(inputPathJson).GetFiles("*.json");

            MissingJsonNotification(pdfFiles, jsonFiles);

            foreach (var pdfFile in pdfFiles)
            {
                var fileName = Path.GetFileName(pdfFile.FullName);

                var lastWriteDateTime = File.GetCreationTime(pdfFile.FullName);

                File.Copy(pdfFile.FullName.ToString(), localpath + "\\PDFS\\" + fileName, true);

                if (File.Exists(originalOrderInputPath + fileName))
                    File.Delete(originalOrderInputPath + fileName);

                File.Copy(pdfFile.FullName.ToString(), originalOrderInputPath + fileName, true);

                File.Copy(pdfFile.FullName.ToString(), processedFolderPath + fileName, true);

                try
                {
                    File.Delete(pdfFile.FullName.ToString());
                }
                catch
                {
                }

            }

            foreach (var jsonFile in jsonFiles)
            {
                var fileName = Path.GetFileName(jsonFile.FullName);

                var lastWriteDateTime = File.GetCreationTime(jsonFile.FullName);

                if (DateTime.Now.Subtract(lastWriteDateTime).TotalMinutes > 120)
                {

                    File.Copy(jsonFile.FullName.ToString(), localpath + "\\Input\\" + fileName, true);

                    if (File.Exists(originalOrderInputPath + fileName))
                        File.Delete(originalOrderInputPath + fileName);

                    File.Copy(jsonFile.FullName.ToString(), originalOrderInputPath + fileName, true);
                    File.Copy(jsonFile.FullName.ToString(), processedFolderPath + fileName, true);

                    try
                    {
                        File.Delete(jsonFile.FullName);
                    }
                    catch
                    {
                    }
                }
            }

        }

        private static void MissingJsonNotification(FileInfo[] pdfFiles, FileInfo[] jsonFiles)
        {
            List<string> pdfNameList = new List<string>();
            List<string> jsonNameList = new List<string>();

            foreach (var pdfFile in pdfFiles)
            {
                var fileName = Path.GetFileName(pdfFile.FullName);

                var splitArray = fileName.Split(new char[] { '-' });

                if (!pdfNameList.Contains(splitArray[0]))
                    pdfNameList.Add(splitArray[0]);
            }

            foreach (var jsonFile in jsonFiles)
            {
                var fileName = Path.GetFileName(jsonFile.FullName);

                var splitArray = fileName.Split(new char[] { '-' });

                if (!jsonNameList.Contains(splitArray[0]))
                    jsonNameList.Add(splitArray[0]);
            }

            List<string> missingJsonFiles = new List<string>();

            foreach (var pdfname in pdfNameList)
            {
                if (!jsonNameList.Contains(pdfname))
                {
                    missingJsonFiles.Add(pdfname);
                }
            }

            if (missingJsonFiles.Count > 0)
            {
                var defaultMessage = EmailHelper.MissingJsonEmailTemplate;

                var missingNames = string.Join(",", missingJsonFiles);

                defaultMessage = Regex.Replace(defaultMessage, "\\[FILENAME\\]", missingNames);

                var emails = ConfigurationManager.AppSettings["NotificationEmails"].Split(new char[] { ';' });

                foreach (var email in emails)
                {
                    if (String.IsNullOrEmpty(email))
                        continue;

                    var timeNow = DateTime.Now.ToString("MM/dd/yyyy H:mm:ss");

                    EmailHelper.SendMail(email,
                        "PicsMe - Action Required- Missing order Json" + timeNow, defaultMessage);
                }
            }
        }

       

        public static void SendProcessingSummaryEmail(Dictionary<string, string> messages)
        {
            if (messages.Count == 0)
                return;

            var defaultMessage = EmailHelper.ProcessingStatusSummaryEmailTemplate;

            var orderstatuscontent = "";

            orderstatuscontent +=
                "<table border='1'><tr><td colspan='1'><strong>Order ID</strong></td><td colspan='1'><strong>Status</strong></td></tr>";

            var orderStatusdetails = "";

            if (messages.Keys.Count == 0)
                return;

            foreach (var key in messages.Keys)
            {

                orderStatusdetails += "<tr>";
                orderStatusdetails += "<td>" + key + "</td>";

                orderStatusdetails += "<td>" + messages[key] + "</td>";

                orderStatusdetails += "</tr>";
            }

            orderstatuscontent += orderStatusdetails;
            orderstatuscontent += "</table>";

            defaultMessage = Regex.Replace(defaultMessage, "\\[ORDERSTATUS\\]", orderstatuscontent);

            var emails = ConfigurationManager.AppSettings["NotificationEmails"].Split(new char[] { ';' });

            foreach (var email in emails)
            {
                if (String.IsNullOrEmpty(email))
                    continue;

                var timeNow = DateTime.Now.ToString("MM/dd/yyyy H:mm:ss");

                EmailHelper.SendMail(email,
                    "PicsMe Order Summary - " + timeNow, defaultMessage);
            }
        }

        public static void SendProcessingSummaryWelcomeCardsEmail(Dictionary<string, string> messages)
        {
            if (messages.Count == 0)
                return;

            var defaultMessage = EmailHelper.ProcessingStatusSummaryWelcomeCardsEmailTemplate;

            var orderstatuscontent = "";

            orderstatuscontent +=
                "<table border='1'><tr><td colspan='1'><strong>Order ID</strong></td><td colspan='1'><strong>Status</strong></td></tr>";

            var orderStatusdetails = "";

            if (messages.Keys.Count == 0)
                return;

            foreach (var key in messages.Keys)
            {

                orderStatusdetails += "<tr>";
                orderStatusdetails += "<td>" + key + "</td>";

                orderStatusdetails += "<td>" + messages[key] + "</td>";

                orderStatusdetails += "</tr>";
            }

            orderstatuscontent += orderStatusdetails;
            orderstatuscontent += "</table>";

            defaultMessage = Regex.Replace(defaultMessage, "\\[ORDERSTATUS\\]", orderstatuscontent);

            var emails = ConfigurationManager.AppSettings["NotificationEmails"].Split(new char[] { ';' });

            foreach (var email in emails)
            {
                if (String.IsNullOrEmpty(email))
                    continue;

                var timeNow = DateTime.Now.ToString("MM/dd/yyyy H:mm:ss");
                EmailHelper.SendMail(email,
                    "PicsMe Welcome Cards Order Summary - " + timeNow, defaultMessage);
            }
        }


        public void PushOrdersToSiteFlow(Dictionary<string, string> processingStatus)
        {
            foreach (var orderReference in processingStatus.Keys)
            {
                var status = processingStatus[orderReference];

                if (status == "OK")
                {
                    var orderId = _orderHelper.GetOrderIdFromReference(orderReference);

                    if (orderId > 0)
                    {
                        try
                        {
                            _siteflowEngine = new SiteFlowEngine(BaseUrlSiteFlow, SiteflowKey, SiteflowSecretKey);

                            _siteflowEngine.PushOrderToSiteFlow(orderId);

                            _orderHelper.MarkOrderPushedTositeFlow(orderReference);

                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }

            }
        }

        public void ManualPushOrdersProcessing()
        {
            //var manualPushOrders = _orderHelper.GetOrdersToPushToSiteFlowManual();

            //if (manualPushOrders != null)
            //{
            //    foreach (var manualPushOrder in manualPushOrders)
            //    {
            //        var orderId = _orderHelper.GetOrderIdFromReference(manualPushOrder);

            //        _siteflowEngine = new SiteFlowEngine(BaseUrlSiteFlow, SiteflowKey, SiteflowSecretKey);

            //        _siteflowEngine.PushOrderToSiteFlow(orderId);

            //        _orderHelper.MarkOrderPushedTositeFlow(manualPushOrder);
            //    }

            //    //if (manualPushOrders.Count > 0)
            //    //    _orderHelper.MarkManualSiteFlowProcessingComplete();
            //}
        }

        public Dictionary<string, string> CreateOrder()
        {

            //get each order json pdf from FTP location            

            var jsonFiles = new DirectoryInfo(_localProcessingPath + "\\Input\\").GetFiles("*.json");

            if (!jsonFiles.Any())
                return new Dictionary<string, string>();

            Dictionary<string, string> processingSummary = new Dictionary<string, string>();

            foreach (var jsonFile in jsonFiles)
            {

            }

            
            return processingSummary;
        }

       
       
        private static string SetCustomerName(SiteflowOrder.RootObject jsonObject, string customerName)
        {
            if (jsonObject.orderData.shipments.Count > 0)
            {
                customerName = jsonObject.orderData.shipments[0].shipTo.name;
                jsonObject.orderData.customerName = customerName;
            }

            return customerName;
        }

        private void MediaClipFilesDownload(bool hasMediaClipItem, SiteflowOrder.RootObject jsonObject)
        {
            if (hasMediaClipItem)
            {
                //read from database
                //download and save pdf to local with name
                //_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +".PDF")

                foreach (var item in jsonObject.orderData.items)
                {
                    if (!string.IsNullOrEmpty(item.supplierPartAuxiliaryId))
                    {
                        var orderDetails = _mediaClipEntities.tMediaClipOrderDetails.FirstOrDefault(m =>
                            m.SupplierPartAuxilliaryId == item.supplierPartAuxiliaryId &&
                            m.LineNumber == item.mediaclipLineNumber);

                        var extrinsicDetails = _mediaClipEntities.tMediaClipOrderExtrinsic
                            .Where(e => e.MediaClipOrderDetailsId == orderDetails.OrderDetailsId).ToList();

                        foreach (var component in item.components)
                        {
                            var path = component.path;
                            var coverOrText = component.code;

                            if (coverOrText == "Cover")
                            {
                                var coverExtrinsic = extrinsicDetails.FirstOrDefault(x => x.ExtrinsicName.Contains("cover"));

                                DownloadPdf(coverExtrinsic.ExtrinsicValue,
                                    _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" +
                                    (1) + ".PDF");
                            }
                            else
                            {
                                var pageExtrinsic = extrinsicDetails.FirstOrDefault(x => x.ExtrinsicName.Contains("pages"));

                                DownloadPdf(pageExtrinsic.ExtrinsicValue,
                                    _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" +
                                    (2) + ".PDF");
                            }
                        }
                    }
                }
            }
        }

        private static void SetRushOrderForPicsMeHelp(SiteflowOrder.RootObject jsonObject)
        {
            if (jsonObject.orderData.shipments.Count > 0 && jsonObject.orderData.shipments[0].shipTo != null)
            {
                if (jsonObject.orderData.shipments[0].shipTo.email == "help@thePicsMe.tv")
                {
                    foreach (var item in jsonObject.orderData.items)
                    {
                        foreach (var component in item.components)
                        {
                            component.attributes.RUSH = "rush";
                        }
                    }
                }
            }
        }

        private void PhotobookProcessing(string sourceOrderId, string originalOrderInputPath, string orderorderId, string orderbarcode, SiteflowOrder.Item item)
        {
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 1 + ".PDF", originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_1.PDF", true);
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 2 + ".PDF", originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_2.PDF", true);

            item.components[0].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId + "_" + orderbarcode + "_1.PDF";

            item.components[1].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId + "_" + orderbarcode + "_2.PDF";
        }

        private static bool ContainsMediaClipItem(SiteflowOrder.RootObject jsonObject)
        {
            bool hasMediaClipItem = false;
            foreach (var item in jsonObject.orderData.items)
            {
                if (!string.IsNullOrEmpty(item.supplierPartAuxiliaryId))
                {
                    hasMediaClipItem = true;
                    break;
                }
            }

            return hasMediaClipItem;
        }

       
        private bool IsGoodOrder(Dictionary<string, string> processingSummary, string sourceOrderId)
        {
            var goodOrder = true;

            try
            {
                if (processingSummary.ContainsKey(sourceOrderId))
                {
                    if (processingSummary[sourceOrderId].Contains("Order failed"))
                        goodOrder = false;
                }
            }
            catch (Exception e)
            {
            }

            return goodOrder;
        }
        
        private static DateTime SetOrderDatetime(SiteflowOrder.RootObject jsonObject)
        {
            var orderDatetime = Convert.ToDateTime(jsonObject.orderData.slaTimestamp);

            if (orderDatetime < DateTime.Now.AddMonths(-3))
                orderDatetime = DateTime.Now;

            jsonObject.orderData.slaTimestamp = orderDatetime;

            foreach (var orderDataShipment in jsonObject.orderData.shipments)
            {
                orderDataShipment.canShipEarly = true;
                orderDataShipment.slaDays = 3;
            }

            return orderDatetime;
        }
    }
}
