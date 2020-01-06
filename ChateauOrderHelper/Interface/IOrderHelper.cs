using System;
using System.Collections.Generic;
using ChateauOrderHelper.Model;

namespace ChateauOrderHelper.Interface
{
    /// <summary>
    /// ORDER HELPER INTERFACE
    /// </summary>
    public interface IOrderHelper
    {
        long CreateOrder(string orderReference, DateTime orderDatetime, decimal orderTotal, decimal deliveryCost, string email, string telephone, string originalJson);
        void AddOrderItem(long id, string sku, string referenceNumber, int quantity, string substrate, string artwork);
        List<string> GetOrdersToPushToSiteFlowManual();
        List<long> GetOrdersToPushToSiteFlow();
        void WriteLog(string message, long orderId);
        void MarkOrderPushedTositeFlow(string orderId);
        void SubmitModifiedSiteflowJson(long orderId, string modifiedSiteflowJson);
        List<long> GetSiteFlowPushedOrders();
        void ChateauStatusProcessing(string sourceOrderId, string orderStatus, string json);
        void MarkManualSiteFlowProcessingComplete();
        bool IsSentToSiteFlow(long orderId);
        string GetOrderSourceOrderId(long orderId);
        long GetOrderIdFromReference(string orderReference);
        string GetModifiedSiteflowOrderJson(long orderId);
        void ProcessPostBacks(long orderId);
        bool DoesOrderExists(string sourceOrderId);

        void AddKnife(ReportData model);

        void MarkKnifeSentToProduction(long id);
    }
}