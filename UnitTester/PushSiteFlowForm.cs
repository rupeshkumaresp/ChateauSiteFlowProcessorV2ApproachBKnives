using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
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

        private void ProcessJsonOrders()
        {
            //PdfModificationHelper test = new PdfModificationHelper();

            //test.CreateBarcodeMirrorImageBelfield("0000169940202", "000016994");
            //test.AddBarcodeImageBelfield(@"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\modified\000016994_0000169940201.pdf", @"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\modified\0000169940202_barcode_Normal.jpg");
            //test.ApplyAdditionalTextToCover("000013904", @"\\nas3\TheChateauTV\STATIC\Chateau-Stationery\Card\Potagerie.PDF", @"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\Rupes.PDF");
            //test.SelectPages(@"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\Product 3 Potagerie.pdf", "3-4", @"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\Product 3 Potagerie_SS_In.pdf");
            //test.ChateauStationerySetPDFModifications("235253563", @"C:\Development\MergeIt\ChateauSiteFlowProcessorV2ApproachBKnives\PDFs\Product 3 Potagerie_SS_In.pdf", "StationerySet", "Potagerie", "Paper");

            Cleanup();

            var processHelper = new ProcessHelper();
            //DOWNLOAD ORDERS FROM SFTP
            //ProcessHelper.DownloadOrders();

            //CREATE THESE ORDERS TO DATABASE            
            var processingResults = processHelper.CreateOrder();

            //PUsH ORDERS TO SITEFLOW
            processHelper.PushOrdersToSiteFlow(processingResults);

            ProcessHelper.SendProcessingSummaryEmail(processingResults);

            return;

            ChateauKnivesProcessing();

            ChateauBelfieldProcessing();

        }

        private void ChateauBelfieldProcessing()
        {
            GenerateReportOutputSpreadsheet chateauBelfieldReportengine = new GenerateReportOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15)
            {
                OrderHelper orderHelper = new OrderHelper();
                var path = chateauBelfieldReportengine.CreateSpreadSheetBelfield(
                    orderHelper.ExtractBelfieldReportData());

                //CREATE IMPOSTIONS PDFS AND SAVE TO FOLDER AND MARK TO DATABASE THAT IMPOSTIONS DONE
                //GEt all the files from holiding folder

                var baseHoldingFolder = ConfigurationManager.AppSettings["BelfieldHolidingFolderPath"];

                if (!Directory.Exists(baseHoldingFolder))
                    Directory.CreateDirectory(baseHoldingFolder);

                var pdfLabelFiles = new DirectoryInfo(baseHoldingFolder).GetFiles("*.PDF", SearchOption.TopDirectoryOnly);

                List<string> mergedPDFList = new List<string>();

                List<string> pagesToMeMerged = new List<string>();

                var pdfStacks = GetNearestMultipleQuantity(pdfLabelFiles.Length, 8);

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
                        if (p == pdfLabelFiles.Length-1)
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

                do
                {
                    Thread.Sleep(60000);

                } while (CheckBelfieldPrinergyOutputGenerated(mergedPDFList));

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


                var mergedImposedSingleLabelFile = ConfigurationManager.AppSettings["PrinergyOutputMergedFinalLabelsPath"] + "Belfield_" + System.DateTime.Now.ToString("ddMMyyyy") + ".pdf";

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

                if (!string.IsNullOrEmpty(path))
                    EmailHelper.SendBelfieldReportEmail(path);
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

        private static void ChateauKnivesProcessing()
        {
            GenerateReportOutputSpreadsheet chateauKnivesReportengine = new GenerateReportOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15)
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
