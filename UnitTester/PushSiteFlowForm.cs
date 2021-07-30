using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;
using PicsMeOrderHelper;

namespace PicsMeSiteFlowApp
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
            var processHelper = new ProcessHelper();

            Cleanup();

            //DOWNLOAD ORDERS FROM SFTP
            ProcessHelper.DownloadOrders();

            //CREATE THESE ORDERS TO DATABASE            
            var processingResults = processHelper.CreateOrder();

            //PUsH ORDERS TO SITEFLOW
            processHelper.PushOrdersToSiteFlow(processingResults);

            ProcessHelper.SendProcessingSummaryEmail(processingResults);

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
