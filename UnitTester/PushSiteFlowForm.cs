using System;
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
            Cleanup();

            var processHelper = new ProcessHelper();
            //DOWNLOAD ORDERS FROM SFTP
            //ProcessHelper.DownloadOrders();
            //CREATE THESE ORDERS TO DATABASE            

            var processingResults = processHelper.CreateOrder();

            //PUsH ORDERS TO SITEFLOW
            processHelper.PushOrdersToSiteFlow(processingResults);

            //processHelper.ManualPushOrdersProcessing();

            ProcessHelper.SendProcessingSummaryEmail(processingResults);

            //processHelper.ProcessPostBacks();

            ChateauKnivesProcessing();
        }

        private static void ChateauKnivesProcessing()
        {
            GenerateOutputSpreadsheet chateauKnivesReportengine = new GenerateOutputSpreadsheet();

            var now = System.DateTime.Now;

            if (now.Hour == 15)
            {
                OrderHelper orderHelper = new OrderHelper();
                chateauKnivesReportengine.CreateSpreadSheet(orderHelper.ExtractKnifeReportData());
            }
        }

        private void Cleanup()
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
    }
}
