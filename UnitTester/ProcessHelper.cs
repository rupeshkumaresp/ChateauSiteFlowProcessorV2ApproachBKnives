using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using ChateauEntity.Entity;
using ChateauOrderHelper;
using ChateauOrderHelper.Model;
using iTextSharp.text;
using Newtonsoft.Json;
using SiteFlowHelper;
using SpreadsheetReaderLibrary;

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
                    "Chateau Order Summary - " + timeNow, defaultMessage);
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
                    "Chateau Welcome Cards Order Summary - " + timeNow, defaultMessage);
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
                SiteflowOrder.RootObject jsonObject = new SiteflowOrder.RootObject();
                bool exceptionJsonRead = false;
                try
                {
                    jsonObject = ProcessHelper.ReadJsonFile(jsonFile, ref json);
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

                if (jsonObject.orderData.shipments.Count > 0)
                    customerName = jsonObject.orderData.shipments[0].shipTo.name;

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

                try
                {
                    //check already in database then don't create again

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

                    bool onlyPreOrderItems = OrderContainsOnlyPreOrder(jsonObject);

                    bool orderContainsKnivesAndOtherProducts = OrderContainsMixProductsWithKnives(jsonObject);

                    bool orderContainsPreOrderAndOtherProducts = OrderContainsMixProductsWithPreOrder(jsonObject);

                    List<SiteflowOrder.Item> knifeJsonItems = new List<SiteflowOrder.Item>();
                    List<SiteflowOrder.Item> preOrderJsonItems = new List<SiteflowOrder.Item>();

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
                            if (sku == "Chateau-Stationery" || sku == "Chateau-StationerySet" ||
                                sku == "ChildBook-Chateau")
                            {
                                //donot do anything
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

                        //PDF modifications & update the json with new PDF path to database
                        string orderfileName = pdfPath + sourceItemId + ".PDF";
                        string ordersubstrateName = substrate;
                        string orderbarcode = sourceItemId;
                        string orderorderId = sourceOrderId;
                        string orderQty = Convert.ToString(qty);

                        var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

                        var finalPdfPath = originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode +
                                           ".PDF";

                        bool isDyeSub = sku == "Dye-Sub-Chateau";//|| sku == "Bag-Chateau" || sku == "Apron-Chateau";

                        if (ordersubstrateName == "Tote")
                            isDyeSub = false;

                        bool etchedProductCandle = sku == "EtchedProduct-Chateau";

                        if (sku == "Bag-Chateau" || sku == "Apron-Chateau")
                        {
                            if (!Directory.Exists(@"\\192.168.0.84\TheChateauTV\DyeSubChateau\Artwork\"))
                                Directory.CreateDirectory(@"\\192.168.0.84\TheChateauTV\DyeSubChateau\Artwork\");

                            if (!Directory.Exists(@"\\192.168.0.84\TheChateauTV\DyeSubChateau\Label\"))
                                Directory.CreateDirectory(@"\\192.168.0.84\TheChateauTV\DyeSubChateau\Label\");

                            //artwork
                            for (int i = 1; i <= qty; i++)
                            {
                                var artworkFileName =
                                    @"\\192.168.0.84\TheChateauTV\DyeSubChateau\Artwork\" + orderbarcode +
                                    "_Artwork_" + i.ToString() + ".pdf";
                                File.Copy(orderfileName, artworkFileName, true);
                            }

                            File.Copy(orderfileName, finalPdfPath, true);

                            //label
                            for (int i = 1; i <= qty; i++)
                            {
                                var labelFileName = @"\\192.168.0.84\TheChateauTV\DyeSubChateau\Label\" + orderbarcode +
                                                    "_Label_" + i.ToString() + ".pdf";

                                var qtyString = i.ToString() + " of " + qty.ToString();

                                _pdfModificationHelper.ChateauBagApronLabelGeneration(labelFileName, substrate,
                                    orderbarcode, orderorderId, qtyString);
                            }

                            _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                            item.components[0].path =
                                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                "_" + orderbarcode + ".PDF";

                        }
                        else
                        {
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
                                    var artworkFileName =
                                        @"\\192.168.0.84\TheChateauTV\Candles\Artwork\" + orderbarcode +
                                        "_Artwork_" + i.ToString() + ".pdf";
                                    File.Copy(orderfileName, artworkFileName, true);
                                }

                                File.Copy(orderfileName, finalPdfPath, true);

                                //label
                                for (int i = 1; i <= qty; i++)
                                {
                                    var labelFileName = @"\\192.168.0.84\TheChateauTV\Candles\Label\" + orderbarcode +
                                                        "_Label_" + i.ToString() + ".pdf";

                                    var qtyString = i.ToString() + " of " + qty.ToString();

                                    _pdfModificationHelper.ChateauCandleLabelGeneration(labelFileName, substrate,
                                        orderbarcode, orderorderId, qtyString);
                                }

                                _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                                item.components[0].path =
                                    "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                    "_" + orderbarcode + ".PDF";
                            }
                            else
                            {
                                if (isDyeSub)
                                {
                                    _pdfModificationHelper.PdfModifications(orderfileName, ordersubstrateName,
                                        orderbarcode,
                                        orderorderId, orderQty);

                                    if (!File.Exists(_localProcessingPath + "//PDFS//Modified//" + orderorderId + "_" +
                                                     orderbarcode + ".PDF"))
                                    {
                                        processingSummary.Add(sourceOrderId + "-" + sourceItemId,
                                            "Flatten PDF not found");

                                        if (processingSummary.ContainsKey(sourceOrderId))
                                            processingSummary[sourceOrderId] += "Order failed";
                                        else
                                            processingSummary.Add(sourceOrderId, "Order failed");

                                        continue;
                                    }

                                    File.Copy(
                                        _localProcessingPath + "//PDFS//Modified//" + orderorderId + "_" +
                                        orderbarcode +
                                        ".PDF",
                                        finalPdfPath, true);
                                }
                                else
                                {
                                    if (ordersubstrateName == "Tote" || sku == "Cushion-Chateau" ||
                                        sku == "StaticBag-Chateau" || sku == "Tour-Chateau" ||
                                        sku == "Belfield-Chateau" ||
                                        sku == "BelfieldFabric-Chateau")
                                    {

                                        //Belfield needs processing
                                        if (sku == "Belfield-Chateau" || sku == "BelfieldFabric-Chateau")
                                        {
                                            var artworkPathBelfield =
                                                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                                "_" + orderbarcode + ".PDF";

                                            //dump to database for impostions and processing
                                            DumpBelfieldToDatabase(sku, item, orderId, sourceOrderId, sourceItemId,
                                                orderbarcode, artworkPathBelfield);

                                            //modify the PDF
                                            _pdfModificationHelper.BelfieldPDFProcessing(orderfileName, orderbarcode,
                                                orderorderId);

                                            //copy to holiding folder based on quantity for impositions

                                            var holdingFolderDir =
                                                ConfigurationManager.AppSettings["BelfieldHolidingFolderPath"];

                                            if (!Directory.Exists(holdingFolderDir))
                                                Directory.CreateDirectory(holdingFolderDir);

                                            for (int i = 1; i <= qty; i++)
                                            {
                                                File.Copy(orderfileName,
                                                    holdingFolderDir + orderorderId + "_" + orderbarcode + "_" + i +
                                                    ".PDF",
                                                    true);
                                            }

                                        }

                                        File.Copy(orderfileName,
                                            originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode +
                                            ".PDF",
                                            true);

                                    }
                                    else
                                    {

                                        if (sku == "ChildBook-Chateau")
                                        {
                                            ChateauChildBookProcessing(item, sourceOrderId, pdfCount, finalPdfPath,
                                                orderorderId, orderbarcode, customerName, processingSummary);
                                        }
                                        else
                                        {
                                            if (sku == "Chateau-Stationery")
                                            {
                                                try
                                                {
                                                    ChateauStationeryProcessing(item, sourceOrderId, pdfCount,
                                                        finalPdfPath,
                                                        orderorderId, orderbarcode, customerName, processingSummary);

                                                }
                                                catch (Exception e)
                                                {
                                                    if (processingSummary.ContainsKey(sourceOrderId))
                                                        processingSummary[sourceOrderId] += "Order failed";
                                                    else
                                                        processingSummary.Add(sourceOrderId, "Order failed");
                                                }
                                            }
                                            else
                                            {

                                                if (sku == "Chateau-StationerySet")
                                                {
                                                    try
                                                    {
                                                        finalPdfPath = ChateauStationerySetProcessing(item,
                                                            finalPdfPath,
                                                            orderorderId, orderbarcode, customerName,
                                                            processingSummary);
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        if (processingSummary.ContainsKey(sourceOrderId))
                                                            processingSummary[sourceOrderId] += "Order failed";
                                                        else
                                                            processingSummary.Add(sourceOrderId, "Order failed");
                                                    }

                                                }
                                                else
                                                {
                                                    if (staticOrder)
                                                    {
                                                        File.Copy(
                                                            pdfPath + sourceItemId + ".PDF", finalPdfPath, true);
                                                    }
                                                    else
                                                    {
                                                        File.Copy(
                                                            _localProcessingPath + "/PDFS/" + sourceOrderId + "-" +
                                                            (pdfCount) +
                                                            ".PDF",
                                                            finalPdfPath, true);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        bool chateauStationery = sku == "Chateau-Stationery" || sku == "Chateau-StationerySet";
                        bool chateauChildBook = sku == "ChildBook-Chateau";

                        _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                        if (!chateauStationery && !chateauChildBook)
                        {
                            if (sku == "Chateau-WatercolourSet")
                            {
                                for (int compCount = 0; compCount < item.components.Count; compCount++)
                                {
                                    item.components[compCount].path =
                                        "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                        "_" + orderbarcode + ".PDF";
                                }
                            }
                            else
                            {
                                item.components[0].path =
                                    "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                                    "_" + orderbarcode + ".PDF";
                            }
                        }

                        //If item is knife then add this to database Knife table
                        DumpKnivesToDatabase(sku, orderContainsKnivesAndOtherProducts, knifeJsonItems, item, orderId,
                            sourceOrderId, sourceItemId, orderbarcode, jsonObject);
                        DumpPreOrderToDatabase(sku, orderContainsPreOrderAndOtherProducts, preOrderJsonItems, item,
                            orderId, sourceOrderId, sourceItemId, orderbarcode, jsonObject);
                    }

                    RemoveKnivesOrderItem(orderContainsKnivesAndOtherProducts, jsonObject, knifeJsonItems);
                    RemovePreOrderItem(orderContainsPreOrderAndOtherProducts, jsonObject, preOrderJsonItems);

                    var serializedResultJson = JsonConvert.SerializeObject(
                        jsonObject,
                        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                    if (!onlyKnives && !onlyPreOrderItems)
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


                    //if failed - order delete the knives and Belfeild order so that it can be processed later
                    if (!IsGoodOrder(processingSummary, sourceOrderId))
                    {
                        _orderHelper.DeleteKnives(sourceOrderId);
                        _orderHelper.DeletePreOrder(sourceOrderId);
                        _orderHelper.DeleteBelfield(sourceOrderId);
                    }

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

        private string ChateauStationerySetProcessing(SiteflowOrder.Item item, string finalPdfPath, string orderorderId,
            string orderbarcode, string customerName, Dictionary<string, string> processingSummary)
        {
            var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

            int componentCount = 1;
            foreach (var itemComponent in item.components)
            {
                var code = itemComponent.code;
                var stationeryStyle = itemComponent.attributes.StationeryStyle;
                var stationeryType = itemComponent.attributes.StationeryType;

                if (!string.IsNullOrEmpty(stationeryStyle))
                    stationeryStyle = stationeryStyle.Trim();

                if (!string.IsNullOrEmpty(stationeryType))
                    stationeryType = stationeryType.Trim();


                var pdfFileName = itemComponent.path.Split('/').Last();

                if (pdfFileName.Contains("-0"))
                    pdfFileName = pdfFileName.Replace("-0", "-");

                var SheetQuantity = Convert.ToInt32(itemComponent.attributes.SheetQuantity);

                if (code == "Stationery")
                {
                    var qtyPDF = SheetQuantity;

                    if (qtyPDF == 0)
                        qtyPDF = 1;

                    _pdfModificationHelper.SelectPages(_localProcessingPath + "/PDFS/" + pdfFileName, "1-2",
                        _localProcessingPath + "/PDFS/" + orderorderId + "-Stationery-In.PDF");

                    var newChateauStationeryPDFPath =
                        _pdfModificationHelper.ChateauStationeryPDFModifications(orderorderId,
                            _localProcessingPath + "/PDFS/" + orderorderId + "-Stationery-In.PDF", code,
                            stationeryStyle, stationeryType, customerName, qtyPDF, processingSummary);

                    finalPdfPath = finalPdfPath.Replace(".PDF", "_" + componentCount + ".PDF");
                    File.Copy(newChateauStationeryPDFPath,
                        originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_" +
                        componentCount + ".PDF", true);

                    itemComponent.path =
                        "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                        "_" + orderbarcode + "_" + componentCount + ".PDF";
                }

                if (code == "StationerySet")
                {
                    var qtyPDF = SheetQuantity;

                    if (qtyPDF == 0)
                        qtyPDF = 1;

                    _pdfModificationHelper.SelectPages(_localProcessingPath + "/PDFS/" + pdfFileName, "3-4",
                        _localProcessingPath + "/PDFS/" + orderorderId + "-StationerySet-In.PDF");

                    var newChateauStationerySetPDFPath =
                        _pdfModificationHelper.ChateauStationerySetPDFModifications(orderorderId,
                            _localProcessingPath + "/PDFS/" + orderorderId + "-StationerySet-In.PDF", code,
                            stationeryStyle, stationeryType, customerName, qtyPDF, processingSummary);



                    finalPdfPath = finalPdfPath.Replace(".PDF", "_" + componentCount + ".PDF");
                    File.Copy(newChateauStationerySetPDFPath,
                        originalOrderInputPath + "/Processed/" + orderorderId + "_" + orderbarcode + "_" +
                        componentCount + ".PDF", true);

                    itemComponent.path =
                        "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                        "_" + orderbarcode + "_" + componentCount + ".PDF";
                }

                componentCount++;
            }

            return finalPdfPath;
        }


        private void ChateauChildBookProcessing(SiteflowOrder.Item item, string sourceOrderId, int pdfCount,
            string finalPdfPath,
            string orderorderId, string orderbarcode, string customerName, Dictionary<string, string> processingSummary)
        {
            finalPdfPath = finalPdfPath.ToUpper();

            var coverfinalPdfPath = finalPdfPath.Replace(".PDF", "_C.PDF");
            var TextfinalPdfPath = finalPdfPath.Replace(".PDF", "_T.PDF");

            int coverIndex = 0;

            int textIndex = 0;

            if (item.components[0].code == "Cover")
                coverIndex = 0;

            if (item.components[1].code == "Cover")
                coverIndex = 1;

            if (item.components[0].code == "Text")
                textIndex = 0;

            if (item.components[1].code == "Text")
                textIndex = 1;


            var pdfFileName = item.components[0].path.Split('/').Last();

            _pdfModificationHelper.ChateauChildBookText(orderorderId, _localProcessingPath + "/PDFS/" + pdfFileName,
                TextfinalPdfPath, processingSummary);

            _pdfModificationHelper.ChateauChildBookCover(orderorderId, _localProcessingPath + "/PDFS/" + pdfFileName,
                coverfinalPdfPath, processingSummary);


            //File.Copy(newChateauStationeryPDFPath, finalPdfPath, true);


            item.components[coverIndex].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                "_" + orderbarcode + "_C.PDF";

            item.components[textIndex].path =
                "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                "_" + orderbarcode + "_T.PDF";
        }


        private void ChateauStationeryProcessing(SiteflowOrder.Item item, string sourceOrderId, int pdfCount,
            string finalPdfPath,
            string orderorderId, string orderbarcode, string customerName, Dictionary<string, string> processingSummary)
        {
            var code = item.components[0].code;
            var StationeryStyle = item.components[0].attributes.StationeryStyle;
            var StationeryType = item.components[0].attributes.StationeryType;

            if (!string.IsNullOrEmpty(StationeryStyle))
                StationeryStyle = StationeryStyle.Trim();

            if (!string.IsNullOrEmpty(StationeryType))
                StationeryType = StationeryType.Trim();

            var pdfFileName = item.components[0].path.Split('/').Last();

            var SheetQuantity = Convert.ToInt32(item.components[0].attributes.SheetQuantity);

            if (code == "Stationery")
            {
                var qtyPDF = SheetQuantity;

                if (qtyPDF == 0)
                    qtyPDF = 1;

                var newChateauStationeryPDFPath =
                    _pdfModificationHelper.ChateauStationeryPDFModifications(orderorderId,
                        _localProcessingPath + "/PDFS/" + pdfFileName, code, StationeryStyle, StationeryType,
                        customerName, qtyPDF, processingSummary);

                File.Copy(newChateauStationeryPDFPath, finalPdfPath, true);

                item.components[0].path =
                    "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + orderorderId +
                    "_" + orderbarcode + ".PDF";
            }
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



        private void DumpPreOrderToDatabase(string sku, bool orderContainsPreOrderAndOtherProducts,
            List<SiteflowOrder.Item> preOrderJsonItems, SiteflowOrder.Item item,
            long orderId, string sourceOrderId, string sourceItemId, string orderbarcode,
            SiteflowOrder.RootObject jsonObject)
        {
            if (sku == "Chateau-PreOrder")
            {
                if (orderContainsPreOrderAndOtherProducts)
                {
                    preOrderJsonItems.Add(item);
                }

                ChateauPreOrder model = new ChateauPreOrder
                {
                    OrderId = Convert.ToString(orderId),
                    OrderReference = sourceOrderId,
                    OrderDetailsReference = sourceItemId,
                    BarCode = orderbarcode,
                    Attribute = item.components[0].attributes.ProductCode + " " +
                                item.components[0].attributes.ProductFinishedPageSize,
                    Substrate = item.components[0].attributes.Substrate,
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

                _orderHelper.AddPreOrder(model);
            }
        }


        private void DumpKnivesToDatabase(string sku, bool orderContainsKnivesAndOtherProducts,
            List<SiteflowOrder.Item> knifeJsonItems, SiteflowOrder.Item item,
            long orderId, string sourceOrderId, string sourceItemId, string orderbarcode,
            SiteflowOrder.RootObject jsonObject)
        {
            if (sku == "ShenKnives-Chateau")
            {
                if (orderContainsKnivesAndOtherProducts)
                {
                    knifeJsonItems.Add(item);
                }

                ChateauKnivesReportData model = new ChateauKnivesReportData
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

        private void DumpBelfieldToDatabase(string sku, SiteflowOrder.Item item,
            long orderId, string sourceOrderId, string sourceItemId, string orderbarcode, string artworkPathBelfield)
        {

            BelfieldModel model = new BelfieldModel()
            {
                OrderId = Convert.ToInt64(orderId),
                OrderReference = sourceOrderId,
                OrderDetailsReference = sourceItemId,
                BarCode = orderbarcode,
                AttributeDesignCode = item.components[0].attributes.DesignCode,
                AttributeLength = item.components[0].attributes.Length,
                Quantity = Convert.ToInt32(item.quantity),
                ArtworkUrl = artworkPathBelfield,
            };

            _orderHelper.AddBelfield(model);

        }

        private static void RemoveKnivesOrderItem(bool orderContainsKnivesAndOtherProducts,
            SiteflowOrder.RootObject jsonObject,
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


        private static void RemovePreOrderItem(bool orderContainsPreOrderAndOtherProducts,
            SiteflowOrder.RootObject jsonObject,
            List<SiteflowOrder.Item> preOrderJsonItems)
        {
            //REMOVE THE PREORDER json order item so it doesn't get duplicate in siteflow
            if (orderContainsPreOrderAndOtherProducts)
            {
                List<SiteflowOrder.Item> modifiedItems = new List<SiteflowOrder.Item>();

                foreach (var item in jsonObject.orderData.items)
                {
                    if (!preOrderJsonItems.Contains(item))
                        modifiedItems.Add(item);
                }

                jsonObject.orderData.items = modifiedItems;
            }
        }


        private static bool OrderContainsOnlyPreOrder(SiteflowOrder.RootObject jsonObject)
        {
            bool onlyPreOrder = true;

            foreach (var item in jsonObject.orderData.items)
            {
                var sku = item.sku;

                if (sku != "Chateau-PreOrder")
                {
                    onlyPreOrder = false;
                    break;
                }
            }

            return onlyPreOrder;
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



        private static bool OrderContainsMixProductsWithPreOrder(SiteflowOrder.RootObject jsonObject)
        {
            bool preOrderFound = false;
            bool otherProductFound = false;

            foreach (var item in jsonObject.orderData.items)
            {
                var sku = item.sku;

                if (sku == "Chateau-PreOrder")
                {
                    preOrderFound = true;
                }

                if (sku != "Chateau-PreOrder")
                {
                    otherProductFound = true;
                }

            }

            if (preOrderFound && otherProductFound)
                return true;

            return false;
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


        public void ChateauWelcomeCardsProcessing()
        {
            var now = System.DateTime.Now;

            if (now.Hour == 15 || now.Hour == 16 || now.Hour == 17)
            {
                Dictionary<string, string> processingSummary = new Dictionary<string, string>();
                string AsposeLicense = ConfigurationManager.AppSettings["WorkingDirectory"] +
                                       ConfigurationManager.AppSettings["ServiceFolderPath"] +
                                       @"License/Aspose.Pdf.lic";

                Aspose.Pdf.License license = new Aspose.Pdf.License();
                license.SetLicense(AsposeLicense);

                string json = "";
                string ChateauWLJsonPath = ConfigurationManager.AppSettings["ChateauWLJsonPath"];
                var xlsxFolder = ConfigurationManager.AppSettings["ChateauWLXLSXFolderPath"];
                var ChateauWLPDFPath = ConfigurationManager.AppSettings["ChateauWLPDFPath"];
                var originalOrderInputPath = ConfigurationManager.AppSettings["OriginalOrderInputPath"];

                SiteflowOrder.RootObject jsonObject = new SiteflowOrder.RootObject();

                jsonObject = ProcessHelper.ReadJsonFile(new FileInfo(ChateauWLJsonPath), ref json);

                var xlsxFiles = new DirectoryInfo(xlsxFolder).GetFiles("*.xlsx", SearchOption.TopDirectoryOnly);

                //Read the xlsxInput file

                foreach (var xlsxInput in xlsxFiles)
                {

                    ExcelRecordImporter importer = new ExcelRecordImporter(xlsxInput.FullName);

                    int dataset = 1;

                    foreach (var dataSetName in importer.GetDataSetNames())
                    {
                        if (dataset > 1)
                            break;

                        var importedRows = importer.Import(dataSetName);
                        dataset++;
                        if (!importedRows.Any())
                            break;

                        //xlsx validation
                        int row = 1;

                        foreach (var importedRow in importedRows)
                        {
                            if (row == 1)
                            {
                                bool valid = true;
                                //validation
                                valid = ValidateWelcomeCardsColumns(importedRow, processingSummary, valid);

                                if (!valid)
                                    break;
                            }

                            if (string.IsNullOrEmpty(importedRow["Order #".ToLower()]))
                                continue;

                            var sourceOrderId = "SWP" + importedRow["Order #".ToLower()].PadLeft(9, '0');

                            bool duplicate = _orderHelper.DuplicateWelcomeCardsCheck(sourceOrderId);


                            if (duplicate)
                            {
                                processingSummary.Add(sourceOrderId, "Duplicate order, order rejected!");
                                continue;
                            }

                            //for each csv row
                            try
                            {

                                //generate the name pdf
                                ////create order and order details, address entry in database

                                var sourceItemId = sourceOrderId;

                                var finalPdfPath = originalOrderInputPath + "/Processed/" + sourceOrderId + "_" +
                                                   sourceItemId +
                                                   ".PDF";


                                Aspose.Pdf.Document pdfDocument = new Aspose.Pdf.Document(ChateauWLPDFPath);
                                var name = importedRow["Customer name".ToLower()];


                                PdfModificationHelper.DoFindReplace("#Name", pdfDocument, name);
                                pdfDocument.Save(finalPdfPath);

                                //build the json

                                //push the json to siteflow
                                DateTime orderDatetime = Convert.ToDateTime(importedRow["Created At".ToLower()]);


                                decimal orderTotal = 0;
                                decimal deliveryCost = 0;
                                var email = importedRow["Email".ToLower()];
                                var telephone = importedRow["Telephone".ToLower()];
                                var originalJson = json;

                                var orderId = _orderHelper.CreateOrder(sourceOrderId, orderDatetime, orderTotal,
                                    deliveryCost,
                                    email, telephone, originalJson);

                                var sku = "Chateau-WL";


                                int qty = 1;

                                //modify the json 
                                var substrate = "Chateau-WL";

                                jsonObject.orderData.sourceOrderId = sourceOrderId;
                                jsonObject.orderData.items[0].barcode = sourceOrderId;

                                jsonObject.orderData.items[0].sourceItemId = sourceOrderId;

                                jsonObject.orderData.items[0].components[0].barcode = sourceOrderId;
                                jsonObject.orderData.items[0].components[0].path =
                                    "https://smilepdf.espsmile.co.uk/pdfs/Processed/" + sourceOrderId +
                                    "_" + sourceItemId + ".PDF";

                                //       Country

                                var isoCountry = importedRow["ISO Country".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.name = importedRow["Customer name".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.companyName =
                                    importedRow["Company Name".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.address1 = importedRow["Address1".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.address2 = importedRow["Address2".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.town = importedRow["City".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.state = importedRow["Region".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.postcode = importedRow["Post Code".ToLower()];
                                jsonObject.orderData.shipments[0].shipTo.isoCountry = isoCountry;
                                jsonObject.orderData.shipments[0].shipTo.phone = telephone;
                                jsonObject.orderData.shipments[0].shipTo.email = email;
                                jsonObject.orderData.shipments[0].slaDays = 1;


                                //Get Carrier Alias based on country names
                                var carrierAlias = "ChateauP2P";

                                if (isoCountry == "GB")
                                    carrierAlias = "chateau.rm.48u";

                                jsonObject.orderData.shipments[0].carrier.alias = carrierAlias;

                                _orderHelper.AddOrderItem(orderId, sku, sourceItemId, qty, substrate, finalPdfPath);

                                var serializedResultJson = JsonConvert.SerializeObject(
                                    jsonObject,
                                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                                _orderHelper.SubmitModifiedSiteflowJson(orderId, serializedResultJson);

                                _siteflowEngine = new SiteFlowEngine(BaseUrlSiteFlow, SiteflowKey, SiteflowSecretKey);
                                _siteflowEngine.PushOrderToSiteFlow(orderId);
                                _orderHelper.MarkOrderPushedTositeFlow(sourceItemId);
                                processingSummary.Add(sourceOrderId, "OK");
                            }
                            catch (Exception ex)
                            {
                                processingSummary.Add(sourceOrderId, "ERROR - " + ex.Message);
                            }

                            row++;
                        }
                    }

                    //end for each


                    //Move the xlsx file to processed folder 

                    var filename = Path.GetFileName(xlsxInput.FullName);

                    if (File.Exists(xlsxFolder + "Processed" + "\\" + filename))
                        File.Delete(xlsxFolder + "Processed" + "\\" + filename);

                    File.Move(xlsxInput.FullName, xlsxFolder + "Processed" + "\\" + filename);
                }

                //send email

                if (processingSummary.Count > 0)
                    ProcessHelper.SendProcessingSummaryWelcomeCardsEmail(processingSummary);

            }
        }

        private static bool ValidateWelcomeCardsColumns(Dictionary<string, string> importedRow, Dictionary<string, string> processingSummary, bool valid)
        {
            if (!importedRow.ContainsKey("Created At".ToLower()))
            {
                processingSummary.Add("Created At column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Order #".ToLower()))
            {
                processingSummary.Add("Order # column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Customer name".ToLower()))
            {
                processingSummary.Add("Customer name column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Company Name".ToLower()))
            {
                processingSummary.Add("Company Name column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Address1".ToLower()))
            {
                processingSummary.Add("Address1 column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Address2".ToLower()))
            {
                processingSummary.Add("Address2 column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("City".ToLower()))
            {
                processingSummary.Add("City column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Region".ToLower()))
            {
                processingSummary.Add("Region column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Post Code".ToLower()))
            {
                processingSummary.Add("Post Code column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            if (!importedRow.ContainsKey("Country".ToLower()))
            {
                processingSummary.Add("Country column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }


            if (!importedRow.ContainsKey("ISO Country".ToLower()))
            {
                processingSummary.Add("ISO Country column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }


            if (!importedRow.ContainsKey("Telephone".ToLower()))
            {
                processingSummary.Add("Telephone column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }


            if (!importedRow.ContainsKey("Email".ToLower()))
            {
                processingSummary.Add("Email column is missing!", "INVALID SPREADSHEET");
                valid = false;
            }

            return valid;
        }
    }
}
