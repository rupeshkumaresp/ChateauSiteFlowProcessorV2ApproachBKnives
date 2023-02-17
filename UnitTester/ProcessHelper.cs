using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using PicsMeEntity.MediaClipEntity;
using PicsMeOrderHelper;
using PicsMeOrderHelper.Model;
using Newtonsoft.Json;
using PicsMeSiteFlowApp.Interface;
using SiteFlowHelper;
using iTextSharp.text.pdf;

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

        public SiteflowOrder.RootObject ReadJsonFile(FileInfo jsonFile, ref string json)
        {
            SiteflowOrder.RootObject jsonObject;

            using (StreamReader r = new StreamReader(jsonFile.FullName))
            {
                json = r.ReadToEnd();
                //REMOVE EMPTY ATTRIBUTES
                json = json.Replace("\"" + "error" + "\"" + ":[],", "");
                json = json.Replace("\"" + "stockItems" + "\"" + ":[],", "");
                json = json.Replace("\"" + "attributes" + "\"" + ":[],", "");
                json = json.Replace("\"" + "extraData" + "\"" + ": [],", "");
                json = json.Replace("\"" + "extraData" + "\"" + ":[],", "");
                //DESERIALIZE TO OBJECT
                jsonObject = JsonConvert.DeserializeObject<SiteflowOrder.RootObject>(json);
            }

            return jsonObject;
        }

        /// <summary>
        /// download the pdf from url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool DownloadPdf(string url, string filename)
        {
            bool success = true;

            try
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                WebClient webClient = new WebClient();
                webClient.DownloadFile(url, filename);

            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }
        /// <summary>
        /// Download the orders from SFTP path to local path for processing
        /// </summary>
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

            //MissingJsonNotification(pdfFiles, jsonFiles);

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

        /// <summary>
        /// Order processing summary email generation
        /// </summary>
        /// <param name="messages"></param>
        public static void SendProcessingSummaryEmail(Dictionary<string, string> messages)
        {
            if (messages.Count == 0)
                return;


            ProcessingStatusErrorEmailNotification(messages);
            ProcessingStatusEmailNotification(messages);
        }

        private static void ProcessingStatusErrorEmailNotification(Dictionary<string, string> messages)
        {
            var defaultMessage = EmailHelper.ProcessingStatusSummaryEmailTemplate;

            var orderstatuscontent = "";

            orderstatuscontent +=
                "<table border='1'><tr><td colspan='1'><strong>Order ID</strong></td><td colspan='1'><strong>Status</strong></td></tr>";

            var orderStatusdetails = "";

            if (messages.Keys.Count == 0)
                return;

            bool errorEmail = false;

            foreach (var key in messages.Keys)
            {
                if (messages[key].Contains("Order failed"))
                {
                    errorEmail = true;
                    orderStatusdetails += "<tr>";
                    orderStatusdetails += "<td>" + key + "</td>";

                    orderStatusdetails += "<td>" + messages[key] + "</td>";

                    orderStatusdetails += "</tr>";
                }
            }

            if (!errorEmail)
                return;

            orderstatuscontent += orderStatusdetails;
            orderstatuscontent += "</table>";

            defaultMessage = Regex.Replace(defaultMessage, "\\[ORDERSTATUS\\]", orderstatuscontent);

            var emails = ConfigurationManager.AppSettings["NotificationErrorEmails"].Split(new char[] { ';' });

            foreach (var email in emails)
            {
                if (String.IsNullOrEmpty(email))
                    continue;

                var timeNow = DateTime.Now.ToString("MM/dd/yyyy H:mm:ss");

                EmailHelper.SendMail(email,
                    "PicsMe Order Processing Error Notification - " + timeNow, defaultMessage);
            }
        }


        private static void ProcessingStatusEmailNotification(Dictionary<string, string> messages)
        {
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

        /// <summary>
        /// push the orders to siteflow for processing
        /// </summary>
        /// <param name="processingStatus"></param>
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

        /// <summary>
        /// Order processing steps - process static, photobook , media clip items
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> CreateOrder()
        {

            //GET EACH ORDER JSON PDF FROM local directory
            var jsonFiles = new DirectoryInfo(_localProcessingPath + "\\Input\\").GetFiles("*.json");

            if (!jsonFiles.Any())
                return new Dictionary<string, string>();

            Dictionary<string, string> processingSummary = new Dictionary<string, string>();

            //PROCESS EACH ORDER JSON
            foreach (var jsonFile in jsonFiles)
            {
                #region READ JSON file
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
                        "Error- Json structure issue - Order failed");


                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName)))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                    File.Move(jsonFile.FullName,
                        _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));
                    continue;

                }
                #endregion

                #region Check Duplicate Order
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
                        "Order exists in database and order has already been pushed to siteflow - Order failed");
                    continue;
                }

                #endregion

                #region Check Incomplete Address

                bool incompleteAddress = CheckIncompleteAddress(jsonFile, jsonObject);

                if (incompleteAddress)
                {
                    processingSummary.Add(sourceOrderId, "Error - Incomplete Address - Order failed");
                    continue;
                }

                #endregion

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
                    #region Valdiation
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
                    #endregion

                    #region Media Clip download

                    bool hasMediaClipItem = !string.IsNullOrEmpty(item.supplierPartAuxiliaryId);

                    if (hasMediaClipItem)
                        staticOrder = false;

                    bool success = MediaClipFilesDownload(hasMediaClipItem, jsonObject, pdfCount, item);



                    if (!success)
                    {
                        if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName)))
                            File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                        File.Copy(jsonFile.FullName.ToString(), ConfigurationManager.AppSettings["OriginalOrderJsonInputPath"] + Path.GetFileName(jsonFile.FullName), true);

                        File.Move(jsonFile.FullName.ToString(), _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                        if (processingSummary.ContainsKey(sourceOrderId))
                        {
                            processingSummary[sourceOrderId] += sourceOrderId + "- Media clip download Error! - Order failed";
                            continue;
                        }
                        else
                        {
                            processingSummary.Add(sourceOrderId, sourceOrderId + "- Media clip download Error! - Order failed");
                            continue;
                        }
                    }
                    #endregion

                    var substrate = item.components[0].attributes.Substrate;

                    var pdfPath = _localProcessingPath + "/PDFS/";

                    var staticPdfPath = ConfigurationManager.AppSettings["StaticPDFPath"];

                    if (!sku.ToLower().Contains("photobook"))
                    {
                        #region Static Order

                        if (staticOrder)
                        {
                            if (!File.Exists(staticPdfPath + pdfName))
                            {
                                //send email
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                    staticPdfPath + pdfName + " not found in static folder - Order failed");

                                if (processingSummary.ContainsKey(sourceOrderId))
                                    processingSummary[sourceOrderId] += "Order failed";
                                else
                                    processingSummary.Add(sourceOrderId, "Order failed");

                                continue;
                            }

                            File.Copy(staticPdfPath + pdfName, pdfPath + sourceItemId + ".PDF", true);
                            File.Copy(staticPdfPath + pdfName, _localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) + ".PDF", true);
                        }
                        #endregion

                        #region Non Static Order
                        else
                        {
                            if (!File.Exists(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +
                                             ".PDF"))
                            {
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                    sourceOrderId + "-" + (pdfCount) + ".PDF" + " PDF not found - Order failed");

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
                        #endregion
                    }

                    string orderfileName = pdfPath + sourceItemId + ".PDF";
                    string ordersubstrateName = substrate;
                    string orderbarcode = sourceItemId;
                    string orderorderId = sourceOrderId;
                    string orderQty = Convert.ToString(qty);

                    var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

                    var finalPdfPath = originalOrderInputPath + orderorderId + "_" + orderbarcode + ".PDF";

                    #region PhotoBook Processing
                    if (sku.ToLower().Contains("photobook"))
                    {
                        PhotobookProcessing(sourceOrderId, originalOrderInputPath, orderorderId, orderbarcode, item);
                    }
                    #endregion

                    #region Other products
                    else
                    {
                        File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) + ".PDF", finalPdfPath, true);
                        item.components[0].path =
                            "https://siteflowpdfs.espautomation.co.uk/Picsme/" + orderorderId +
                            "_" + orderbarcode + ".PDF";
                    }
                    #endregion

                    _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                }

                #region Siteflow Json creation & updating database

                var serializedResultJson = JsonConvert.SerializeObject(
                    jsonObject,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });


                var goodOrder = IsGoodOrder(processingSummary, sourceOrderId);

                if (goodOrder)
                    _orderHelper.SubmitModifiedSiteflowJson(orderId, serializedResultJson);

                #endregion

                #region Cleanup
                var fileName = Path.GetFileName(jsonFile.FullName);

                if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + fileName))
                    File.Delete(_localProcessingPath + "\\ProcessedInput\\" + fileName);

                File.Copy(jsonFile.FullName.ToString(), ConfigurationManager.AppSettings["OriginalOrderJsonInputPath"] + fileName, true);

                File.Move(jsonFile.FullName.ToString(), _localProcessingPath + "\\ProcessedInput\\" + fileName);

                if (!processingSummary.ContainsKey(sourceOrderId))
                    processingSummary.Add(sourceOrderId, "OK");
                #endregion

            }

            return processingSummary;
        }
        /// <summary>
        /// Address validation
        /// </summary>
        /// <param name="jsonFile"></param>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
        private bool CheckIncompleteAddress(FileInfo jsonFile, SiteflowOrder.RootObject jsonObject)
        {
            bool incompleteAddress = false;

            if (jsonObject.orderData.shipments.Count > 0 && jsonObject.orderData.shipments[0].shipTo != null)
            {
                if (string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.address1) ||
                    string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.town) ||
                    string.IsNullOrEmpty(jsonObject.orderData.shipments[0].shipTo.postcode))
                {
                    incompleteAddress = true;
                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" +
                                    Path.GetFileName(jsonFile.FullName)))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" +
                                    Path.GetFileName(jsonFile.FullName));

                    File.Move(jsonFile.FullName,
                        _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));


                }
            }

            return incompleteAddress;
        }

        /// <summary>
        /// set customer name as shipment customer name
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <param name="customerName"></param>
        /// <returns></returns>
        public string SetCustomerName(SiteflowOrder.RootObject jsonObject, string customerName)
        {
            if (jsonObject.orderData.shipments.Count > 0)
            {
                customerName = jsonObject.orderData.shipments[0].shipTo.name;
                jsonObject.orderData.customerName = customerName;
            }
            return customerName;
        }
        /// <summary>
        /// Based on supplierPartAuxiliaryId download the files
        /// </summary>
        /// <param name="hasMediaClipItem"></param>
        /// <param name="jsonObject"></param>
        /// <param name="pdfCount"></param>
        public bool MediaClipFilesDownload(bool hasMediaClipItem, SiteflowOrder.RootObject jsonObject, int pdfCount, SiteflowOrder.Item itemInProcess)
        {
            bool sucess = true;

            if (hasMediaClipItem)
            {
                try
                {
                    //foreach (var item in jsonObject.orderData.items)
                    var item = itemInProcess;
                    {
                        if (!string.IsNullOrEmpty(item.supplierPartAuxiliaryId))
                        {
                            var mediaClipNumber = Convert.ToInt32(item.mediaclipLineNumber);
                            var orderDetails = _mediaClipEntities.tMediaClipOrderDetails.FirstOrDefault(m =>
                                m.SupplierPartAuxilliaryId == item.supplierPartAuxiliaryId /*&&
                            m.LineNumber == mediaClipNumber*/);

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
                catch { sucess = false; }

            }

            return sucess;
        }

        /// <summary>
        /// photobook processing - process cover and text
        /// </summary>
        /// <param name="sourceOrderId"></param>
        /// <param name="originalOrderInputPath"></param>
        /// <param name="orderorderId"></param>
        /// <param name="orderbarcode"></param>
        /// <param name="item"></param>
        public void PhotobookProcessing(string sourceOrderId, string originalOrderInputPath, string orderorderId, string orderbarcode, SiteflowOrder.Item item)
        {
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 1 + ".PDF", originalOrderInputPath + "/" + orderorderId + "_" + orderbarcode + "_1.PDF", true);
            File.Copy(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + 2 + ".PDF", originalOrderInputPath + "/" + orderorderId + "_" + orderbarcode + "_2.PDF", true);

            //assign the cover and Text pages
            item.components[0].path =
                "https://siteflowpdfs.espautomation.co.uk/Picsme/" + orderorderId + "_" + orderbarcode + "_2.PDF";

            item.components[1].path =
                "https://siteflowpdfs.espautomation.co.uk/Picsme/" + orderorderId + "_" + orderbarcode + "_1.PDF";

            //Set the Page count for Siteflow
            item.components[0].attributes.Pages = GetPageCount(originalOrderInputPath + "/" + sourceOrderId + "_" + orderbarcode + "_2.PDF");
            item.components[1].attributes.Pages = GetPageCount(originalOrderInputPath + "/" + sourceOrderId + "_" + orderbarcode + "_1.PDF");


        }


        internal int? GetPageCount(string src)
        {
            int pageCount = 1;

            using (PdfReader pdfReader = new PdfReader(src))
            {
                pageCount = pdfReader.NumberOfPages;
            }

            return pageCount;
        }

        /// <summary>
        /// check media clip item exists
        /// </summary>
        /// <param name="jsonObject"></param>
        /// <returns></returns>
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
            }

            return orderDatetime;
        }
    }
}
