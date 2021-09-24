using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ChateauOrderHelper;

namespace ChateauSiteFlowApp
{
    /// <summary>
    /// DOWNLOAD ORDERS, CREATE ORDERS AND PUSH TO SITE-FLOW
    /// </summary>
    public partial class PushSiteFlowForm : Form
    {
        public PushSiteFlowForm()
        {
            InitializeComponent();
        }

        private void UnitTestForm_Load(object sender, EventArgs e)
        {
            ProcessJsonOrders();
            this.Close();
        }

        public bool CheckPrinegyStatus()
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            // Use the default Ttl value which is 128,
            // but change the fragmentation behavior.
            options.DontFragment = true;

            // Create a buffer of 32 bytes of data to be transmitted.
            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send("192.168.16.231", timeout, buffer, options);

            if (reply.Status == IPStatus.Success)
            {
                if (Directory.Exists(@"\\192.168.16.231\AraxiVolume_HW33546-46_J\Jobs\Auto_Impose\SmartHotFolders"))
                {
                    return true;
                }
            }


            return false;
        }

        private void ProcessJsonOrders()
        {
            var processHelper = new ProcessHelper();

            Cleanup();

            //DOWNLOAD ORDERS FROM SFTP
            ProcessHelper.DownloadOrders();

            //CREATE THESE ORDERS TO DATABASE            
            var processingResults = processHelper.CreateOrder();

            //PUsH ORDERS TO SITEFLOW
            processHelper.PushOrdersToSiteFlow(processingResults);

            ProcessHelper.SendProcessingSummaryEmail(processingResults);

            ChateauKnivesProcessing();

            ChateauPreOrderProcessing();

            ChateauBelfieldProcessing();

            processHelper.ChateauWelcomeCardsProcessing();

        }


        private void ChateauBelfieldProcessing()
        {
            GenerateReportOutputSpreadsheet chateauBelfieldReportengine = new GenerateReportOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15 || now.Hour == 16 || now.Hour == 17)
            {


                string path = "";
                try
                {
                    if (!CheckPrinegyStatus())
                        throw new Exception("PRINERGY SERVER IS NOT ACCESSIBLE");

                    OrderHelper orderHelper = new OrderHelper();
                    path = chateauBelfieldReportengine.CreateSpreadSheetBelfield(
                        orderHelper.ExtractBelfieldReportData());

                    //CREATE IMPOSTIONS PDFS AND SAVE TO FOLDER AND MARK TO DATABASE THAT IMPOSTIONS DONE
                    //GEt all the files from holiding folder

                    var baseHoldingFolder = ConfigurationManager.AppSettings["BelfieldHolidingFolderPath"];

                    if (!Directory.Exists(baseHoldingFolder))
                        Directory.CreateDirectory(baseHoldingFolder);

                    var pdfLabelFiles = new DirectoryInfo(baseHoldingFolder).GetFiles("*.PDF", SearchOption.TopDirectoryOnly);

                    List<FileInfo> pdfLabelFilesSortedList = new List<FileInfo>();

                    List<string> distinctOrderDetailsReferenceBelfield = new List<string>();

                    for (int p = 0; p < pdfLabelFiles.Length; p++)
                    {
                        var fileShortName = Path.GetFileNameWithoutExtension(pdfLabelFiles[p].FullName);

                        var orderDetailsArray = fileShortName.Split('_');

                        if (orderDetailsArray.Length >= 2)
                        {
                            if (!distinctOrderDetailsReferenceBelfield.Contains(orderDetailsArray[1]))
                                distinctOrderDetailsReferenceBelfield.Add(orderDetailsArray[1]);
                        }

                    }

                    Dictionary<string, string> dictionaryOnDesignCode = new Dictionary<string, string>();

                    //get all the design code first for all orde details reference
                    foreach (var orderDetailsRef in distinctOrderDetailsReferenceBelfield)
                    {
                        var designCode = orderHelper.GetDesignCode(orderDetailsRef);
                        dictionaryOnDesignCode.Add(orderDetailsRef, designCode);

                    }

                    var sortedDictionaryOnDesignCode = dictionaryOnDesignCode.OrderBy(x => x.Value).ToList();

                    //pdfLabelFiles Sorted List based on design code for merge and prinergy push
                    for (int i = 0; i < sortedDictionaryOnDesignCode.Count; i++)
                    {
                        var key = sortedDictionaryOnDesignCode[i].Key;

                        foreach (var pdflabel in pdfLabelFiles)
                        {
                            if (pdflabel.Name.Contains(key))
                            {
                                if (!pdfLabelFilesSortedList.Contains(pdflabel))
                                    pdfLabelFilesSortedList.Add(pdflabel);
                            }
                        }
                    }

                    //assign back the sorted labels array
                    pdfLabelFiles = pdfLabelFilesSortedList.ToArray();

                    List<string> mergedPDFList = new List<string>();

                    List<string> pagesToMeMerged = new List<string>();

                    var count = 1;

                    var fileCount = 1;


                    //merge them 8 at a times and save to Merged folder

                    for (int p = 0; p < pdfLabelFiles.Length; p++)
                    {
                        pagesToMeMerged.Add(pdfLabelFiles[p].FullName);

                        count++;

                        if (count == 9)
                        {
                            count = 1;
                            var nowDateTime = System.DateTime.Now;
                            var mergeFileName = baseHoldingFolder + "//Merged//" + "Belfield_" + nowDateTime.ToString("ddMMyyyy") + "_" + fileCount + ".pdf";
                            MergeFiles(mergeFileName, pagesToMeMerged);
                            mergedPDFList.Add(mergeFileName);
                            pagesToMeMerged.Clear();
                            fileCount++;
                        }
                        else
                        {
                            if (p == pdfLabelFiles.Length - 1)
                            {
                                var nowDateTime = System.DateTime.Now;
                                var mergeFileName = baseHoldingFolder + "//Merged//" + "Belfield_" + nowDateTime.ToString("ddMMyyyy") + "_" + fileCount + ".pdf";
                                MergeFiles(mergeFileName, pagesToMeMerged);
                                mergedPDFList.Add(mergeFileName);
                                fileCount++;
                            }
                        }
                    }

                    //push to Prinergy

                    ProcessPrinergyInput(mergedPDFList);

                    //wait till all Prinergy output has been generated - check merge file count and prinergy output count shuould match


                    var startTime = System.DateTime.Now;

                    var imposedOutputNoFound = false;
                    do
                    {
                        Thread.Sleep(60000);

                        var timeAfterImpostions = System.DateTime.Now;

                        if (timeAfterImpostions.Subtract(startTime).TotalMinutes > 59)
                        {
                            if (CheckBelfieldPrinergyOutputGenerated(mergedPDFList))
                                imposedOutputNoFound = true;
                            break;
                        }

                    } while (CheckBelfieldPrinergyOutputGenerated(mergedPDFList));


                    if (imposedOutputNoFound)
                    {
                        EmailHelper.SendBelfieldNoImpositionsErrorEmail(path);
                        return;
                    }

                    //all good, get all the Prinergy output

                    //Merge them and save to final path in BelfieldLabels folder - PrinergyOutputMergedFinalLabelsPath

                    List<string> PrinergyOutputImposedLabelFiles = new List<string>();

                    var PrinergyOutputPath = ConfigurationManager.AppSettings["PrinergyOutputPath"];

                    foreach (var file in mergedPDFList)
                    {
                        var shortFileName = Path.GetFileNameWithoutExtension(file);
                        var PDFFileName = shortFileName + " Imposed.pdf";

                        PrinergyOutputImposedLabelFiles.Add(PrinergyOutputPath + PDFFileName);
                    }


                    var tempName = "Belfield_" + System.DateTime.Now.ToString("ddMMyyyy") + ".pdf";
                    var mergedImposedSingleLabelFile =
                        ConfigurationManager.AppSettings["PrinergyOutputMergedFinalLabelsPath"] + tempName;

                    MergeFiles(mergedImposedSingleLabelFile, PrinergyOutputImposedLabelFiles);

                    //move holiding folder single pdfs to Processed folder

                    //clean up the merged folder
                    for (int p = 0; p < pdfLabelFiles.Length; p++)
                    {
                        pdfLabelFiles[p]
                            .CopyTo(
                                baseHoldingFolder + "//Processed//" + Path.GetFileNameWithoutExtension(pdfLabelFiles[p].FullName) + ".pdf", true);

                        pdfLabelFiles[p].Delete();

                    }

                    for (int p = 0; p < PrinergyOutputImposedLabelFiles.Count; p++)
                    {
                        File.Delete(PrinergyOutputImposedLabelFiles[p]);
                    }

                    var sftpFinalLabelsPath = ConfigurationManager.AppSettings["SFTPFinalLabelsPath"];

                    var finalLabelAtFtp = sftpFinalLabelsPath + tempName;

                    File.Copy(mergedImposedSingleLabelFile, finalLabelAtFtp, true);

                    if (!string.IsNullOrEmpty(path))
                        EmailHelper.SendBelfieldReportEmail(path, "");

                    if (distinctOrderDetailsReferenceBelfield.Count > 0)
                    {
                        orderHelper.MarkOrdersProcessed(distinctOrderDetailsReferenceBelfield);
                    }

                }
                catch (Exception e)
                {
                    EmailHelper.SendBelfieldErrorEmail(path, e.Message + "-" + e.InnerException);
                }



            }
        }

        public bool CheckBelfieldPrinergyOutputGenerated(List<string> mergedPDFList)
        {
            bool allImposed = true;
            var PrinergyOutputPath = ConfigurationManager.AppSettings["PrinergyOutputPath"];

            foreach (var file in mergedPDFList)
            {
                var shortFileName = Path.GetFileNameWithoutExtension(file);
                var PDFFileName = shortFileName + " Imposed.pdf";

                if (!File.Exists(PrinergyOutputPath + PDFFileName))
                {
                    allImposed = false;
                    break;
                }
            }

            if (!allImposed)
                return true;

            return false;

        }

        private void ProcessPrinergyInput(List<string> mergedPdfList)
        {
            //send to prinergy

            var PrinergyInputPath = ConfigurationManager.AppSettings["PrinergyInputPath"];
            var UserName = ConfigurationManager.AppSettings["UserName"];
            var Password = ConfigurationManager.AppSettings["Password"];
            var domain = ConfigurationManager.AppSettings["Domain"];


            foreach (var pdfFile in mergedPdfList)
            {

                var fileShortName = Path.GetFileNameWithoutExtension(pdfFile);

                using (new NetworkConnection(PrinergyInputPath, new NetworkCredential(UserName, Password, domain)))
                {
                    if (File.Exists(PrinergyInputPath + @"\" + fileShortName + ".pdf"))
                        File.Delete(PrinergyInputPath + @"\" + fileShortName + ".pdf");

                    File.Move(pdfFile,
                        PrinergyInputPath + @"\" + fileShortName + ".pdf");
                }
            }

        }

        private static int GetNearestMultipleQuantity(int qty, int NoUp)
        {
            var modifiedQty = qty;

            if (qty % NoUp != 0)
            {
                // Smaller multiple 
                int smallerMultiple = (qty / NoUp) * NoUp;

                // Larger multiple 
                int largerMultiple = smallerMultiple + NoUp;

                // Return of closest of two 
                modifiedQty = (qty - smallerMultiple > largerMultiple - qty) ? largerMultiple : smallerMultiple;

                if (modifiedQty < qty)
                    modifiedQty = largerMultiple;

            }
            return modifiedQty;
        }

        private static void MergeFiles(string mergeFileName, List<string> filesToBeMerged)
        {

            if (File.Exists(mergeFileName))
                File.Delete(mergeFileName);

            List<byte[]> filesByte = new List<byte[]>();


            for (int i = 0; i <= filesToBeMerged.Count; i++)
            {
                try
                {
                    var thisFileBytes = System.IO.File.ReadAllBytes(filesToBeMerged[i]);
                    filesByte.Add(thisFileBytes);
                }
                catch { }

            }

            System.IO.File.WriteAllBytes(mergeFileName, PdfMerger.MergeFiles(filesByte));

        }


        private static void ChateauPreOrderProcessing()
        {
            GenerateReportOutputSpreadsheet chateaupreOrderReportengine = new GenerateReportOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15 || now.Hour == 16 || now.Hour == 17)
            {
                OrderHelper orderHelper = new OrderHelper();
                chateaupreOrderReportengine.CreateSpreadSheetPreOrder(orderHelper.ExtractPreOrderReportData());
            }
        }

        private static void ChateauKnivesProcessing()
        {
            GenerateReportOutputSpreadsheet chateauKnivesReportengine = new GenerateReportOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15 || now.Hour == 16 || now.Hour == 17)
            {
                OrderHelper orderHelper = new OrderHelper();
                chateauKnivesReportengine.CreateSpreadSheetKnives(orderHelper.ExtractKnifeReportData());
            }
        }

        private void Cleanup()
        {
            try
            {
                var localpath = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"] + @"PDFs/Modified/";

                var pdfFiles = new DirectoryInfo(localpath).GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var fileInfo in pdfFiles)
                {
                    fileInfo.Delete();
                }

                localpath = ConfigurationManager.AppSettings["WorkingDirectory"] + ConfigurationManager.AppSettings["ServiceFolderPath"] + @"PDFs/original/";

                pdfFiles = new DirectoryInfo(localpath).GetFiles("*.*", SearchOption.AllDirectories);

                foreach (var fileInfo in pdfFiles)
                {
                    fileInfo.Delete();
                }
            }
            catch (Exception e)
            {

            }


        }
    }
}
