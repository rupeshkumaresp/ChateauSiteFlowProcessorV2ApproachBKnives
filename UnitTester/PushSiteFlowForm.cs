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


            //DOWNLOAD ORDERS FROM SFTP
            ProcessHelper.DownloadOrders();

            //CREATE THESE ORDERS TO DATABASE            
            var processingResults = processHelper.CreateOrder();

            //PUsH ORDERS TO SITEFLOW
            processHelper.PushOrdersToSiteFlow(processingResults);

            ProcessHelper.SendProcessingSummaryEmail(processingResults);

        }

      
    }
}
