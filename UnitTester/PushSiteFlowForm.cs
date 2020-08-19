﻿using System;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
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

            ChateauKnivesProcessing();

            ChateauBelfieldProcessing();
        }

        private void ChateauBelfieldProcessing()
        {
            GenerateOutputSpreadsheet chateauBelfieldReportengine = new GenerateOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15)
            {
                OrderHelper orderHelper = new OrderHelper();
                chateauBelfieldReportengine.CreateSpreadSheetBelfield(orderHelper.ExtractBelfieldReportData());
            }
        }

        private static void ChateauKnivesProcessing()
        {
            GenerateOutputSpreadsheet chateauKnivesReportengine = new GenerateOutputSpreadsheet();

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
