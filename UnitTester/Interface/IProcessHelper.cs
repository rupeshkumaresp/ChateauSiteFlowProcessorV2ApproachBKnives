using System;
using System.Collections.Generic;
using System.IO;
using PicsMeOrderHelper.Model;

namespace PicsMeSiteFlowApp.Interface
{
    public interface IProcessHelper
    {
        SiteflowOrder.RootObject ReadJsonFile(FileInfo jsonFile, ref string json);
        bool DownloadPdf(string url, string filename);
        void PushOrdersToSiteFlow(Dictionary<string, string> processingStatus);
        Dictionary<string, string> CreateOrder();
        string SetCustomerName(SiteflowOrder.RootObject jsonObject, string customerName);
        void MediaClipFilesDownload(bool hasMediaClipItem, SiteflowOrder.RootObject jsonObject, int pdfCount);
        void PhotobookProcessing(string sourceOrderId, string originalOrderInputPath, string orderorderId, string orderbarcode, SiteflowOrder.Item item);
        bool ContainsMediaClipItem(SiteflowOrder.RootObject jsonObject);
        bool IsGoodOrder(Dictionary<string, string> processingSummary, string sourceOrderId);
        DateTime SetOrderDatetime(SiteflowOrder.RootObject jsonObject);
    }
}