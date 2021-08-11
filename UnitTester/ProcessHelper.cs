using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Eventing.Reader;
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
using PicsMeSiteFlowApp.Interface;
using SiteFlowHelper;

namespace PicsMeSiteFlowApp
{
    /// <summary>
    /// HELPER CLASS - DOWNLOAD ORDER, READ JSON AND UPDATE JSON, CREATE ORDER AND SEND TO SITE FLOW
    /// </summary>
    public class ProcessHelper : IProcessHelper
    {
        readonly OrderHelper _orderHelper = new OrderHelper();
        SiteFlowEngine _siteflowEngine;
        private static readonly string BaseUrlSiteFlow = ConfigurationManager.AppSettings["BaseUrlSiteFlow"];
        private static readonly string SiteflowKey = ConfigurationManager.AppSettings["SiteflowKey"];
        private static readonly string SiteflowSecretKey = ConfigurationManager.AppSettings["SiteflowSecretKey"];
        MediaClipEntities _mediaClipEntities = new MediaClipEntities();

        readonly string _localProcessingPath = ConfigurationManager.AppSettings["WorkingDirectory"] +
                                               ConfigurationManager.AppSettings["ServiceFolderPath"];


        public ProcessHelper()
        {
        }

        public  SiteflowOrder.RootObject ReadJsonFile(FileInfo jsonFile, ref string json)
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



        public Dictionary<string, string> CreateOrder()
        {

            //get each order json pdf from FTP location            

            var jsonFiles = new DirectoryInfo(_localProcessingPath + "\\Input\\").GetFiles("*.json");

            if (!jsonFiles.Any())
                return new Dictionary<string, string>();

            Dictionary<string, string> processingSummary = new Dictionary<string, string>();

            foreach (var jsonFile in jsonFiles)
            {
                string json = "";
                SiteflowOrder.RootObject jsonObject = new SiteflowOrder.RootObject();
                bool exceptionJsonRead = false;
                try
                {
                    jsonObject = ReadJsonFile(jsonFile, ref json);
                }
                catch (Exception e)
                {
                    exceptionJsonRead = true;
                }

                if (exceptionJsonRead)
                {
                    processingSummary.Add(Path.GetFileName(jsonFile.FullName), "JSON structure issue- Order failed");
                    File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));
                    continue;
                }

                var customerName = "";

                customerName = SetCustomerName(jsonObject, customerName);

                var sourceOrderId = "";
                try
                {
                    sourceOrderId = jsonObject.orderData.sourceOrderId;

                }
                catch
                {
                    processingSummary.Add(Path.GetFileNameWithoutExtension(jsonFile.FullName),
                        "Error- Json structure issue");


                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName)))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                    File.Move(jsonFile.FullName,
                        _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));
                    continue;

                }


                var itemFound = _orderHelper.DoesOrderExists(sourceOrderId);

                if (itemFound)
                {

                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" +
                                    Path.GetFileName(jsonFile.FullName)))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" +
                                    Path.GetFileName(jsonFile.FullName));

                    File.Move(jsonFile.FullName,
                        _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                    processingSummary.Add(sourceOrderId,
                        "Order exists in database and order has already been pushed to siteflow");
                    continue;
                }

                if (jsonObject.orderData.shipments.Count > 0 && jsonObject.orderData.shipments[0].shipTo != null)
                {
                    if (string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.address1) ||
                        string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.town) ||
                        string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.postcode))
                    {
                        if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" +
                                        Path.GetFileName(jsonFile.FullName)))
                            File.Delete(_localProcessingPath + "\\ProcessedInput\\" +
                                        Path.GetFileName(jsonFile.FullName));

                        File.Move(jsonFile.FullName,
                            _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                        processingSummary.Add(sourceOrderId, "Error - Incomplete Address");
                        continue;
                    }
                }

                var orderDatetime = SetOrderDatetime(jsonObject);

                decimal orderTotal = 0M;
                decimal deliveryCost = 0M;
                string email = "";
                string telephone = "";
                string originalJson = json;

                ////create order and order details, address entry in database
                var orderId = _orderHelper.CreateOrder(sourceOrderId, orderDatetime, orderTotal, deliveryCost,
                    email, telephone, originalJson);

                var itemCount = jsonObject.orderData.items.Count;

                foreach (var item in jsonObject.orderData.items)
                {
                    var sourceItemId = item.sourceItemId;
                    var sku = item.sku;

                    if (string.IsNullOrEmpty(sku))
                    {
                        if (processingSummary.ContainsKey(sourceOrderId))
                            processingSummary[sourceOrderId] += "NULL SKU - Order failed";
                        else
                            processingSummary.Add(sourceOrderId, "NULL SKU - Order failed");

                        break;

                    }


                    var qty = item.quantity;
                    var pdfUri = item.components[0].path;

                    bool staticOrder = pdfUri != null && pdfUri.ToLower().Contains("static");

                    var pdfName = "";
                    if (staticOrder)
                        pdfName = pdfUri.Split('/').Last();

                    var partArray = pdfUri.Split(new char[] { '-' });

                    var pdfCount = 1;

                    if (itemCount > 1)
                    {
                        if (partArray.Length == 2)
                        {
                            partArray[1] = partArray[1].Replace(".pdf", "");

                            try
                            {
                                pdfCount = Convert.ToInt32(partArray[1]);
                            }
                            catch (Exception e)
                            {
                                pdfCount = 1;
                            }

                        }
                    }

                    bool hasMediaClipItem = !string.IsNullOrEmpty(item.supplierPartAuxiliaryId);

                    if (hasMediaClipItem)
                        staticOrder = false;

                    MediaClipFilesDownload(hasMediaClipItem, jsonObject, pdfCount);

                    var substrate = item.components[0].attributes.Substrate;

                    var pdfPath = _localProcessingPath + "/PDFS/";

                    var staticPdfPath = ConfigurationManager.AppSettings["StaticPDFPath"];

                    if (!sku.ToLower().Contains("photobook"))
                    {
                        if (staticOrder)
                        {
                            if (!File.Exists(staticPdfPath + pdfName))
                            {
                                //send email
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                    staticPdfPath + pdfName + " not found in static folder");

                                if (processingSummary.ContainsKey(sourceOrderId))
                                    processingSummary[sourceOrderId] += "Order failed";
                                else
                                    processingSummary.Add(sourceOrderId, "Order failed");

                                continue;
                            }

                            File.Copy(staticPdfPath + pdfName, pdfPath + sourceItemId + ".PDF", true);
                        }
                        else
                        {
                            if (!File.Exists(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +
                                             ".PDF"))
                            {
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                    sourceOrderId + "-" + (pdfCount) + ".PDF" + " PDF not found");

                                if (processingSummary.ContainsKey(sourceOrderId))
                                {
                                    processingSummary[sourceOrderId] += "Order failed";
                                }
                                else
                                {
                                    processingSummary.Add(sourceOrderId, "Order failed");
                                }

                                continue;
                            }

                            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) + ".PDF",
                                pdfPath + sourceItemId + ".PDF", true);
                        }
                    }

                    string orderfileName = pdfPath + sourceItemId + ".PDF";
                    string ordersubstrateName = substrate;
                    string orderbarcode = sourceItemId;
                    string orderorderId = sourceOrderId;
                    string orderQty = Convert.ToString(qty);

                    var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

                    var finalPdfPath = originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode +
                                       ".PDF";

                    if (sku.ToLower().Contains("photobook"))
                    {
                        PhotobookProcessing(sourceOrderId, originalOrderInputPath, orderorderId, orderbarcode, item);
                    }
                    else
                    {
                        File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) + ".PDF", finalPdfPath, true);
                        item.components[0].path =
                            "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                            "_" + orderbarcode + ".PDF";
                    }

                    _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                }

                var serializedResultJson = JsonConvert.SerializeObject(
                    jsonObject,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });


                var goodOrder = IsGoodOrder(processingSummary, sourceOrderId);

                if (goodOrder)
                    _orderHelper.SubmitModifiedSiteflowJson(orderId, serializedResultJson);


                var fileName = Path.GetFileName(jsonFile.FullName);

                if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + fileName))
                    File.Delete(_localProcessingPath + "\\ProcessedInput\\" + fileName);

                File.Move(jsonFile.FullName.ToString(), _localProcessingPath + "\\ProcessedInput\\" + fileName);

                if (!processingSummary.ContainsKey(sourceOrderId))
                    processingSummary.Add(sourceOrderId, "OK");

            }



            return processingSummary;
        }


        public string SetCustomerName(SiteflowOrder.RootObject jsonObject, string customerName)
        {
            if (jsonObject.orderData.shipments.Count > 0)
            {
                customerName = jsonObject.orderData.shipments[0].shipTo.name;
                jsonObject.orderData.customerName = customerName;
            }

            return customerName;
        }

        public void MediaClipFilesDownload(bool hasMediaClipItem, SiteflowOrder.RootObject jsonObject, int pdfCount)
        {
            if (hasMediaClipItem)
            {
                foreach (var item in jsonObject.orderData.items)
                {
                    if (!string.IsNullOrEmpty(item.supplierPartAuxiliaryId))
                    {
                        var orderDetails = _mediaClipEntities.tMediaClipOrderDetails.FirstOrDefault(m =>
                            m.SupplierPartAuxilliaryId == item.supplierPartAuxiliaryId &&
                            m.LineNumber == item.mediaclipLineNumber);

                        var extrinsicDetails = _mediaClipEntities.tMediaClipOrderExtrinsic
                            .Where(e => e.MediaClipOrderDetailsId == orderDetails.OrderDetailsId).ToList();


                        if (item.components.Count == 1)
                        {
                            var extrinsic = extrinsicDetails.FirstOrDefault();

                            DownloadPdf(extrinsic.ExtrinsicValue,
                                _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" + (pdfCount) + ".PDF");

                        }
                        else
                        {
                            foreach (var component in item.components)
                            {
                                var path = component.path;
                                var coverOrText = component.code;

                                if (coverOrText == "Cover")
                                {
                                    var coverExtrinsic =
                                        extrinsicDetails.FirstOrDefault(x => x.ExtrinsicName.Contains("cover"));

                                    DownloadPdf(coverExtrinsic.ExtrinsicValue,
                                        _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" +
                                        (1) + ".PDF");
                                }
                                else
                                {
                                    if (coverOrText == "Text")
                                    {
                                        var pageExtrinsic =
                                            extrinsicDetails.FirstOrDefault(x => x.ExtrinsicName.Contains("pages"));

                                        DownloadPdf(pageExtrinsic.ExtrinsicValue,
                                            _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" +
                                            (2) + ".PDF");
                                    }
                                    else
                                    {
                                        var extrinsic = extrinsicDetails.FirstOrDefault();

                                        DownloadPdf(extrinsic.ExtrinsicValue,
                                            _localProcessingPath + "/PDFS/" + jsonObject.orderData.sourceOrderId + "-" +
                                            (pdfCount) + ".PDF");

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SetRushOrderForPicsMeHelp(SiteflowOrder.RootObject jsonObject)
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

        public void PhotobookProcessing(string sourceOrderId, string originalOrderInputPath, string orderorderId, string orderbarcode, SiteflowOrder.Item item)
        {
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 1 + ".PDF", originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_1.PDF", true);
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 2 + ".PDF", originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_2.PDF", true);

            item.components[0].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId + "_" + orderbarcode + "_1.PDF";

            item.components[1].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId + "_" + orderbarcode + "_2.PDF";
        }

        public bool ContainsMediaClipItem(SiteflowOrder.RootObject jsonObject)
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


        public bool IsGoodOrder(Dictionary<string, string> processingSummary, string sourceOrderId)
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

        public DateTime SetOrderDatetime(SiteflowOrder.RootObject jsonObject)
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
