using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using ChateauOrderHelper;
using ChateauOrderHelper.Model;
using Newtonsoft.Json;
using SiteFlowHelper;

namespace ChateauSiteFlowApp
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
        readonly string _localProcessingPath = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"];
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


            var localpath = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"];
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
                catch { }

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
                    catch { }
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
                        "Chateau - Action Required- Missing order Json" + timeNow, defaultMessage);
                }
            }
        }

        public void ProcessPostBacks()
        {
            var siteFlowSentOrderIds = _orderHelper.GetSiteFlowPushedOrders();

            foreach (var orderId in siteFlowSentOrderIds)
            {
                _orderHelper.ProcessPostBacks(orderId);
            }
        }

        public static void SendProcessingSummaryEmail(Dictionary<string, string> messages)
        {
            if (messages.Count == 0)
                return;

            var defaultMessage = EmailHelper.ProcessingStatusSummaryEmailTemplate;

            var orderstatuscontent = "";

            orderstatuscontent += "<table border='1'><tr><td colspan='1'><strong>Order ID</strong></td><td colspan='1'><strong>Status</strong></td></tr>";

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
                    "Chateau Order Summary - " + timeNow, defaultMessage);
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
                            _orderHelper.WriteLog("Siteflow submission error: " + ex.Message, Convert.ToInt64(orderId));
                        }
                    }
                }

            }
        }

        public void ManualPushOrdersProcessing()
        {
            var manualPushOrders = _orderHelper.GetOrdersToPushToSiteFlowManual();

            if (manualPushOrders != null)
            {
                foreach (var manualPushOrder in manualPushOrders)
                {
                    var orderId = _orderHelper.GetOrderIdFromReference(manualPushOrder);

                    _siteflowEngine = new SiteFlowEngine(BaseUrlSiteFlow, SiteflowKey, SiteflowSecretKey);

                    _siteflowEngine.PushOrderToSiteFlow(orderId);

                    _orderHelper.MarkOrderPushedTositeFlow(manualPushOrder);
                }

                if (manualPushOrders.Count > 0)
                    _orderHelper.MarkManualSiteFlowProcessingComplete();
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

                SiteflowOrder.RootObject jsonObject = ProcessHelper.ReadJsonFile(jsonFile, ref json);

                var sourceOrderId = "";
                try
                {
                    sourceOrderId = jsonObject.orderData.sourceOrderId;

                }
                catch
                {
                    processingSummary.Add(Path.GetFileNameWithoutExtension(jsonFile.FullName), "Error- Json structure issue");


                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName)))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                    File.Move(jsonFile.FullName,
                        _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));
                    continue;

                }

                try
                {  //check already in database then don't create again

                    var itemFound = _orderHelper.DoesOrderExists(sourceOrderId);

                    if (itemFound)
                    {

                        if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName)))
                            File.Delete(_localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                        File.Move(jsonFile.FullName,
                            _localProcessingPath + "\\ProcessedInput\\" + Path.GetFileName(jsonFile.FullName));

                        processingSummary.Add(sourceOrderId, "Order exists in database and order has already been pushed to siteflow");
                        continue;
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

                    bool onlyKnives = OrderContainsOnlyKnives(jsonObject);

                    bool orderContainsKnivesAndOtherProducts = OrderContainsMixProductsWithKnives(jsonObject);

                    List<SiteflowOrder.Item> knifeJsonItems = new List<SiteflowOrder.Item>();

                    foreach (var item in jsonObject.orderData.items)
                    {
                        var sourceItemId = item.sourceItemId;
                        var sku = item.sku;

                        if (string.IsNullOrEmpty(sku))
                        {
                            if (processingSummary.ContainsKey(sourceOrderId))
                            {
                                processingSummary[sourceOrderId] += "NULL SKU - Order failed";
                            }
                            else
                            {
                                processingSummary.Add(sourceOrderId, "NULL SKU - Order failed");
                            }

                            break;
                        }

                        ReviseChateauQuantityCalculations(sku, item);

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
                                pdfCount = Convert.ToInt32(partArray[1]);
                            }
                        }

                        var substrate = item.components[0].attributes.Substrate;

                        var pdfPath = _localProcessingPath + "/PDFS/Original/";

                        var staticPdfPath = ConfigurationManager.AppSettings["StaticPDFPath"];


                        if (staticOrder)
                        {
                            if (!File.Exists(staticPdfPath + pdfName))
                            {
                                //send email
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                    staticPdfPath + pdfName + " not found in static folder");

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

                            File.Copy(staticPdfPath + pdfName, pdfPath + sourceItemId + ".PDF", true);
                        }
                        else
                        {
                            if (!File.Exists(_localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +
                                             ".PDF"))
                            {
                                processingSummary.Add(sourceOrderId + "-" + sourceItemId, sourceOrderId + "-" + (pdfCount) + ".PDF" + " PDF not found");

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

                        //PDF modifications & update the json with new PDF path to database
                        string orderfileName = pdfPath + sourceItemId + ".PDF";
                        string ordersubstrateName = substrate;
                        string orderbarcode = sourceItemId;
                        string orderorderId = sourceOrderId;
                        string orderQty = Convert.ToString(qty);

                        var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

                        var finalPdfPath = originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode +
                                           ".PDF";

                        bool isDyeSub = sku == "Dye-Sub-Chateau" || sku == "Bag-Chateau" || sku == "Apron-Chateau";

                        if (ordersubstrateName == "Tote")
                            isDyeSub = false;

                        bool etchedProductCandle = sku == "EtchedProduct-Chateau";

                        if (etchedProductCandle)
                        {
                            //generate the PDF and save to  \\192.168.0.84\TheChateauTV\Candle_Labels\barcode.pdf
                            //Scent = “The Orangery” and "Walled Garden"
                            // 54x25mm

                            string orderfileNameUnRotated = pdfPath + sourceItemId + "_NO_ROT.PDF";

                            File.Copy(orderfileName, orderfileNameUnRotated, true);

                            File.Delete(orderfileName);

                            _pdfModificationHelper.RotatePDF(orderfileNameUnRotated, orderfileName, 270);

                            if (!Directory.Exists(@"\\192.168.0.84\TheChateauTV\Candles\Artwork\"))
                                Directory.CreateDirectory(@"\\192.168.0.84\TheChateauTV\Candles\Artwork\");

                            if (!Directory.Exists(@"\\192.168.0.84\TheChateauTV\Candles\Label\"))
                                Directory.CreateDirectory(@"\\192.168.0.84\TheChateauTV\Candles\Label\");


                            //artwork
                            for (int i = 1; i <= qty; i++)
                            {
                                var artworkFileName = @"\\192.168.0.84\TheChateauTV\Candles\Artwork\" + orderbarcode + "_Artwork_" + i.ToString() + ".pdf";
                                File.Copy(orderfileName, artworkFileName, true);
                            }

                            File.Copy(orderfileName, finalPdfPath, true);

                            //label
                            for (int i = 1; i <= qty; i++)
                            {
                                var labelFileName = @"\\192.168.0.84\TheChateauTV\Candles\Label\" + orderbarcode + "_Label_" + i.ToString() + ".pdf";

                                var qtyString = i.ToString() + " of " + qty.ToString();

                                _pdfModificationHelper.ChateauCandleLabelGeneration(labelFileName, substrate, orderbarcode, orderorderId, qtyString);
                            }
                            _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                            item.components[0].path = "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId + "_" + orderbarcode + ".PDF";
                        }
                        else
                        {
                            if (isDyeSub)
                            {
                                _pdfModificationHelper.PdfModifications(orderfileName, ordersubstrateName, orderbarcode, orderorderId, orderQty);

                                if (!File.Exists(_localProcessingPath + "//PDFS//Modified//" + orderorderId + "_" + orderbarcode + ".PDF"))
                                {
                                    processingSummary.Add(sourceOrderId + "-" + sourceItemId, "Flatten PDF not found");

                                    if (processingSummary.ContainsKey(sourceOrderId))
                                        processingSummary[sourceOrderId] += "Order failed";
                                    else
                                        processingSummary.Add(sourceOrderId, "Order failed");

                                    continue;
                                }

                                File.Copy(_localProcessingPath + "//PDFS//Modified//" + orderorderId + "_" + orderbarcode + ".PDF",
                                    finalPdfPath, true);
                            }
                            else
                            {
                                if (ordersubstrateName == "Tote" || sku == "Cushion-Chateau" || sku == "StaticBag-Chateau" || sku == "Tour-Chateau" || sku == "Belfield-Chateau" || sku == "BelfieldFabric-Chateau")
                                {
                                    File.Copy(orderfileName, originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + ".PDF",
                                        true);
                                }
                                else
                                {
                                    if (sku == "Chateau-Stationery")
                                    {

                                        var code = item.components[0].code;
                                        var StationeryStyle = item.components[0].attributes.StationeryStyle;
                                        var StationeryType = item.components[0].attributes.StationeryType;

                                        if (code == "Stationery")
                                        {
                                            var newChateauStationeryPDFPath =
                                                _pdfModificationHelper.ChateauStationeryPDFModifications(
                                                    _localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +
                                                    ".PDF", code, StationeryStyle, StationeryType);

                                            File.Copy(newChateauStationeryPDFPath, finalPdfPath, true);

                                            item.components[0].path =
                                                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                                "_" + orderbarcode + ".PDF";
                                        }
                                    }
                                    else
                                    {

                                        if (sku == "Chateau-StationerySet")
                                        {
                                            int componentCount = 1;
                                            foreach (var itemComponent in item.components)
                                            {
                                                var code = itemComponent.code;
                                                var stationeryStyle = itemComponent.attributes.StationeryStyle;
                                                var stationeryType = itemComponent.attributes.StationeryType;

                                                if (code == "Stationery")
                                                {
                                                    var newChateauStationeryPDFPath =
                                                        _pdfModificationHelper.ChateauStationeryPDFModifications(
                                                            _localProcessingPath + "/PDFS/" + sourceOrderId + "-" +
                                                            (pdfCount) +
                                                            ".PDF", code, stationeryStyle, stationeryType);

                                                    finalPdfPath = finalPdfPath.Replace(".pdf", "_" + componentCount + ".pdf");
                                                    File.Copy(newChateauStationeryPDFPath, finalPdfPath, true);

                                                    itemComponent.path =
                                                        "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                                        "_" + orderbarcode +"_" + componentCount + ".PDF";
                                                }

                                                if (code == "StationerySet")
                                                {
                                                    var newChateauStationerySetPDFPath =
                                                        _pdfModificationHelper.ChateauStationerySetPDFModifications(
                                                            _localProcessingPath + "/PDFS/" + sourceOrderId + "-" +
                                                            (pdfCount) +
                                                            ".PDF", code, stationeryStyle, stationeryType);
                                                    finalPdfPath = finalPdfPath.Replace(".pdf", "_" + componentCount + ".pdf");
                                                    File.Copy(newChateauStationerySetPDFPath, finalPdfPath, true);

                                                    itemComponent.path =
                                                        "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                                        "_" + orderbarcode + "_" + componentCount + ".PDF";
                                                }

                                                componentCount++;
                                            }

                                        }
                                        else
                                        {
                                            File.Copy(
                                                _localProcessingPath + "/PDFS/" + sourceOrderId + "-" + (pdfCount) +
                                                ".PDF",
                                                finalPdfPath, true);
                                        }
                                    }
                                }
                            }
                        }
                        _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                        bool chateauStationery = sku == "Chateau-Stationery" || sku == "Chateau-StationerySet";

                        if (!chateauStationery)
                            item.components[0].path = "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                                      "_" + orderbarcode + ".PDF";

                        //If item is knife then add this to database Knife table
                        DumpKnivesToDatabase(sku, orderContainsKnivesAndOtherProducts, knifeJsonItems, item, orderId, sourceOrderId, sourceItemId, orderbarcode, jsonObject);
                    }

                    RemoveKnivesOrderItem(orderContainsKnivesAndOtherProducts, jsonObject, knifeJsonItems);

                    var serializedResultJson = JsonConvert.SerializeObject(
                        jsonObject,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (!onlyKnives)
                    {
                        var goodOrder = IsGoodOrder(processingSummary, sourceOrderId);

                        if (goodOrder)
                            _orderHelper.SubmitModifiedSiteflowJson(orderId, serializedResultJson);
                    }

                    var fileName = Path.GetFileName(jsonFile.FullName);

                    if (File.Exists(_localProcessingPath + "\\ProcessedInput\\" + fileName))
                        File.Delete(_localProcessingPath + "\\ProcessedInput\\" + fileName);

                    File.Move(jsonFile.FullName.ToString(), _localProcessingPath + "\\ProcessedInput\\" + fileName);

                    if (!processingSummary.ContainsKey(sourceOrderId))
                        processingSummary.Add(sourceOrderId, "OK");
                }
                catch (Exception exception)
                {
                    if (processingSummary.ContainsKey(sourceOrderId))
                        processingSummary[sourceOrderId] += "Order failed, Error: " + exception.Message;
                    else
                        processingSummary.Add(sourceOrderId, "Order failed, Error: " + exception.Message);
                }
            }

            return processingSummary;
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

        private void DumpKnivesToDatabase(string sku, bool orderContainsKnivesAndOtherProducts, List<SiteflowOrder.Item> knifeJsonItems, SiteflowOrder.Item item,
            long orderId, string sourceOrderId, string sourceItemId, string orderbarcode, SiteflowOrder.RootObject jsonObject)
        {
            if (sku == "ShenKnives-Chateau")
            {
                if (orderContainsKnivesAndOtherProducts)
                {
                    knifeJsonItems.Add(item);
                }

                ReportData model = new ReportData
                {
                    OrderId = Convert.ToString(orderId),
                    OrderReference = sourceOrderId,
                    OrderDetailsReference = sourceItemId,
                    BarCode = orderbarcode,
                    Attribute = item.components[0].attributes.ProductCode + " " +
                                item.components[0].attributes.ProductFinishedPageSize,
                    Quantity = Convert.ToString(item.quantity),
                    ArtworkUrl = item.components[0].path,
                    CustomerName = jsonObject.orderData.shipments[0].shipTo.name,
                    CustomerAddress1 = jsonObject.orderData.shipments[0].shipTo.address1,
                    CustomerAddress2 = jsonObject.orderData.shipments[0].shipTo.address2,
                    CustomerAddress3 = jsonObject.orderData.shipments[0].shipTo.address3,
                    CustomerTown = jsonObject.orderData.shipments[0].shipTo.town,
                    CustomerState = jsonObject.orderData.shipments[0].shipTo.state,
                    CustomerPostcode = jsonObject.orderData.shipments[0].shipTo.postcode,
                    CustomerCountry = jsonObject.orderData.shipments[0].shipTo.country,
                    CustomerEmail = jsonObject.orderData.shipments[0].shipTo.email,
                    CustomerCompanyName = jsonObject.orderData.shipments[0].shipTo.companyName,
                    CustomerPhone = jsonObject.orderData.shipments[0].shipTo.phone
                };

                _orderHelper.AddKnife(model);
            }
        }

        private static void RemoveKnivesOrderItem(bool orderContainsKnivesAndOtherProducts, SiteflowOrder.RootObject jsonObject,
            List<SiteflowOrder.Item> knifeJsonItems)
        {
            //REMOVE THE KNIFE json order item so it doesn't get duplicate in siteflow
            if (orderContainsKnivesAndOtherProducts)
            {
                List<SiteflowOrder.Item> modifiedItems = new List<SiteflowOrder.Item>();

                foreach (var item in jsonObject.orderData.items)
                {
                    if (!knifeJsonItems.Contains(item))
                        modifiedItems.Add(item);
                }
                jsonObject.orderData.items = modifiedItems;
            }
        }

        private static bool OrderContainsOnlyKnives(SiteflowOrder.RootObject jsonObject)
        {
            bool onlyKnives = true;

            foreach (var item in jsonObject.orderData.items)
            {
                var sku = item.sku;

                if (sku != "ShenKnives-Chateau")
                {
                    onlyKnives = false;
                    break;
                }
            }

            return onlyKnives;
        }

        private static bool OrderContainsMixProductsWithKnives(SiteflowOrder.RootObject jsonObject)
        {
            bool knifeFound = false;
            bool otherProductFound = false;

            foreach (var item in jsonObject.orderData.items)
            {
                var sku = item.sku;

                if (sku == "ShenKnives-Chateau")
                {
                    knifeFound = true;
                }

                if (sku != "ShenKnives-Chateau")
                {
                    otherProductFound = true;
                }

            }

            if (knifeFound && otherProductFound)
                return true;

            return false;
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

        private static void ReviseChateauQuantityCalculations(string sku, SiteflowOrder.Item item)
        {
            var count = Convert.ToInt32(item.quantity);

            if (sku == "Cards-Chateau")
            {
                if (item.components[0].attributes.CardDesign == "Painting" ||
                    item.components[0].attributes.CardDesign == "Christmas Potagerie")
                {
                    if (count > 1)
                    {
                        item.quantity = count * 24;
                    }
                    else
                        item.quantity = 24;
                }

                if (item.components[0].attributes.CardDesign == "Painting-Potagerie")
                {
                    if (count > 1)
                    {
                        item.quantity = count * 12;
                    }
                    else
                        item.quantity = 12;
                }
            }

            try
            {
                if (item.components[0].attributes.Substrate == "Tour Coaster")
                    item.quantity = 4 * Convert.ToInt32(item.quantity);
            }
            catch (Exception e)
            {
            }

        }
    }
}
