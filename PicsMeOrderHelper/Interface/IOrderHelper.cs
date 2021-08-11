using System;
using System.Collections.Generic;
using PicsMeOrderHelper.Model;

namespace PicsMeOrderHelper.Interface
{
    /// <summary>
    /// ORDER HELPER INTERFACE
    /// </summary>
    public interface IOrderHelper
    {
        long CreateOrder(string orderReference, DateTime orderDatetime, decimal orderTotal, decimal deliveryCost, string email, string telephone, string originalJson);
        void AddOrderItem(long id, string sku, string referenceNumber, int quantity, string substrate, string artwork);
        List<long> GetOrdersToPushToSiteFlow();
        void MarkOrderPushedTositeFlow(string orderId);
        void SubmitModifiedSiteflowJson(long orderId, string modifiedSiteflowJson);
        List<long> GetSiteFlowPushedOrders();
        void PicsMeStatusProcessing(string sourceOrderId, string orderStatus, string json);
        bool IsSentToSiteFlow(long orderId);
        string GetOrderSourceOrderId(long orderId);
        long GetOrderIdFromReference(string orderReference);
        string GetModifiedSiteflowOrderJson(long orderId);
       
        bool DoesOrderExists(string sourceOrderId);

    }
}