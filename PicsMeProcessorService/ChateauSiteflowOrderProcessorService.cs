using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using OfficeOpenXml;
using System.Runtime.InteropServices;
using System.Net;
using ChateauOrderHelper;
using SiteFlowHelper;

namespace ChateauSiteflowProcessor
{
    public partial class ChateauSiteflowOrderProcessorService : ServiceBase
    {
        #region data members

        Timer _timer;
        static int runningProcessCount = 0;
        static object _processLock = new object();
        EventLog ChateauSiteFlowProcessorLog = new EventLog();
        private static string baseUrlSiteFlow = ConfigurationManager.AppSettings["BaseUrlSiteFlow"];
        private static string SiteflowKey = ConfigurationManager.AppSettings["SiteflowKey"];
        private static string SiteflowSecretKey = ConfigurationManager.AppSettings["SiteflowSecretKey"];

        OrderHelper _orderHelper = new OrderHelper();
        SiteFlowEngine _siteflowEngine;



        #endregion

        #region Service detault methods

        public ChateauSiteflowOrderProcessorService()
        {
            InitializeComponent();
            // Create the source, if it does not already exist.
            if (!EventLog.SourceExists("ChateauSiteFlowProcessorEventLog"))
                EventLog.CreateEventSource("ChateauSiteFlowProcessorEventLog", "ChateauSiteFlowProcessorLog");

            // Create an EventLog instance and assign its source.
            ChateauSiteFlowProcessorLog.Source = "ChateauSiteFlowProcessorLog";

        }

        protected override void OnStart(string[] args)
        {
            ChateauSiteFlowProcessorLog.WriteEntry("....Processing Chateau order to Siteflow Started/Restarted.....");

            _timer = new Timer();

            double processMin = 10;

            var processOnServiceStart = ConfigurationManager.AppSettings["processOnServiceStart"];

            bool processOnServiceStartFlag = false;

            if (!string.IsNullOrEmpty(processOnServiceStart))
                processOnServiceStartFlag = Convert.ToBoolean(processOnServiceStart);

            if (processOnServiceStartFlag)
                TriggerTrackingCodeOrderProcess();

            var processEveryNMinutes = ConfigurationManager.AppSettings["processEveryNMinutes"];

            if (!string.IsNullOrEmpty(processEveryNMinutes))
                processMin = Convert.ToDouble(processEveryNMinutes);

            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = (1000) * (processMin) * (60);
            _timer.Enabled = true;
            _timer.Start();
        }

        private void _timer_Elapsed(object sender, EventArgs e)
        {
            bool continueFlag = ProcessingStatusAsPerConfiguration();

            if (!continueFlag)
                return;

            TriggerTrackingCodeOrderProcess();
        }

        private static bool ProcessingStatusAsPerConfiguration()
        {
            bool continueFlag = false;
            var now = System.DateTime.Now;

            //read settings from config file
            var processDailyAtTime = ConfigurationManager.AppSettings["processDailyAtTime"];
            var processEveryNMinutes = ConfigurationManager.AppSettings["processEveryNMinutes"];
            var processOnSaturday = ConfigurationManager.AppSettings["processOnSaturday"];
            var processOnSunday = ConfigurationManager.AppSettings["processOnSunday"];

            bool saturdayProcesssingFlag = false;
            bool sundayProcesssingFlag = false;

            if (!string.IsNullOrEmpty(processOnSaturday))
            {
                processOnSaturday = processOnSaturday.ToLower();
                saturdayProcesssingFlag = Convert.ToBoolean(processOnSaturday);
            }
            else
                saturdayProcesssingFlag = false;


            if (!string.IsNullOrEmpty(processOnSunday))
            {
                processOnSunday = processOnSunday.ToLower();
                sundayProcesssingFlag = Convert.ToBoolean(processOnSunday);
            }
            else
                sundayProcesssingFlag = false;


            if (!string.IsNullOrEmpty(processDailyAtTime))
            {
                var processTimeSplit = processDailyAtTime.Split(new char[] { ':' });

                int processHour = Convert.ToInt32(processTimeSplit[0]);
                int processMinute = Convert.ToInt32(processTimeSplit[1]);
                int processMinInterval = Convert.ToInt32(processEveryNMinutes);

                if (now.Hour == processHour && now.Minute >= processMinute && now.Minute < processMinute + processMinInterval)
                    continueFlag = true;
            }
            else
                continueFlag = true;


            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)
            {
                continueFlag = true;
            }


            return continueFlag;
        }

        private void TriggerTrackingCodeOrderProcess()
        {
            ChateauSiteFlowProcessorLog.WriteEntry("Processing Chateau order to Siteflow Started:" + System.DateTime.Now.ToLongDateString());
            ChateauSiteFlowProcessorLog.WriteEntry("************************************************************************");

            try
            {
                if (runningProcessCount == 1)
                    return;

                lock (_processLock)
                {
                    runningProcessCount = 1;
                }

                //ProcessMe();

                //mark the process as complete so that it is ready for next run
                lock (_processLock)
                {
                    runningProcessCount = 0;
                }

                ChateauSiteFlowProcessorLog.WriteEntry("Chateau order to Siteflow finished:" + System.DateTime.Now.ToLongDateString());
                ChateauSiteFlowProcessorLog.WriteEntry("************************************************************************");

            }
            catch (Exception ex)
            {
                lock (_processLock)
                {
                    runningProcessCount = 0;
                }
            }
        }

        #endregion

        //private void ProcessMe()
        //{
        //    PushOrdersToSiteFlow();

        //    ProcessPostBacks();

        //    SentShipmentEmail();
        //}

        //private void ProcessPostBacks()
        //{
        //    var SiteFlowSentOrders = _orderHelper.GetSiteFlowPushedOrders();


        //    foreach (var siteflowOrder in SiteFlowSentOrders)
        //    {
        //        _orderHelper.ProcessPostBacks(siteflowOrder);
        //    }
        //}

        //private void SentShipmentEmail()
        //{
        //    //send shipment tracking email
        //    var shippedOrders = _orderHelper.GetShippedOrders();

        //    foreach (var shippedOrder in shippedOrders)
        //    {
        //        try
        //        {
        //            //send shipment email
        //            _orderHelper.SendOrderShipmentMail(shippedOrder);
        //            _orderHelper.MarkShipmentEmailSent(shippedOrder);
        //        }
        //        catch (Exception ex)
        //        {
        //            _orderHelper.WriteLog("Shipment Email failure: " + ex.Message, shippedOrder);
        //        }

        //    }
        //}

        //private void PushOrdersToSiteFlow()
        //{
        //    var orders = _orderHelper.GetOrdersToPushToSiteFlow();

        //    foreach (var order in orders)
        //    {
        //        try
        //        {
        //            var orderDetails = _orderHelper.GetOrderDetails(order);

        //            _siteflowEngine = new SiteFlowEngine(baseUrlSiteFlow, SiteflowKey, SiteflowSecretKey);
        //            _siteflowEngine.PushOrderToSiteFlow(orderDetails);
        //            _orderHelper.MarkOrderPushedTositeFlow(order);
        //        }
        //        catch (Exception ex)
        //        {
        //            _orderHelper.WriteLog("Siteflow submission error: " + ex.Message, order);
        //        }

        //    }
        //}

        
        protected override void OnStop()
        {
        }
    }


}
