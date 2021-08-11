using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using PicsMeOrderHelper.Interface;
using PicsMeOrderHelper.Model;
using PicsMeEntity.Entity;
using PicsMeEntity.MediaClipEntity;

namespace PicsMeOrderHelper
{

    /// <summary>
    /// ORDER HELPER- CREATE ORDER ITEM IN DATABASE, UPDATE SITEFLOW PUSH STATUS, WRITE LOGS, PROCESS POSTBACKS
    /// </summary>
    public class OrderHelper : IOrderHelper
    {
        private readonly PicsMeEntities _contextPicsMe = new PicsMeEntities();

        public long CreateOrder(string orderReference, DateTime orderDatetime, decimal orderTotal, decimal deliveryCost, string email, string telephone, string originalJson)
        {
            var order = new tOrders
            {
                OrderStatus = "Order Confirmed",
                CreatedAt = orderDatetime,
                ReferenceNumber = orderReference,
                OriginalSiteflowJson = originalJson
            };

            _contextPicsMe.tOrders.Add(order);
            _contextPicsMe.SaveChanges();

            return order.ID;
        }

        public void AddOrderItem(long id, string sku, string referenceNumber, int quantity, string substrate, string artwork)
        {

            var orderDetail = new tOrderDetails
            {
                OrderId = id,
                ReferenceNumber = referenceNumber,
                CreatedAt = DateTime.Now,
                Status = "Proof Accepted",
                Quantity = quantity,
                Substrate = substrate,
                Artwork = artwork,
                SKU = sku
            };
            _contextPicsMe.tOrderDetails.Add(orderDetail);
            _contextPicsMe.SaveChanges();
        }

       

        public List<long> GetOrdersToPushToSiteFlow()
        {
            var orders = _contextPicsMe.tOrders.Where(o => o.OrderStatus == "Order Confirmed" && (o.SentToSiteFlow == false || o.SentToSiteFlow == null)).Select(o => o.ID).ToList();
            return orders;
        }

        public void MarkOrderPushedTositeFlow(string orderRef)
        {
            var order = _contextPicsMe.tOrders.FirstOrDefault(o => o.ReferenceNumber == orderRef);
            if (order != null)
            {
                order.SentToSiteFlow = true;
                order.SiteflowSentDatetme = DateTime.Now;
                _contextPicsMe.SaveChanges();

            }

        }

        public void SubmitModifiedSiteflowJson(long orderId, string modifiedSiteflowJson)
        {
            var order = _contextPicsMe.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (order != null)
            {
                order.ModifiedSiteflowJson = modifiedSiteflowJson;
                _contextPicsMe.SaveChanges();
            }
        }

        public List<long> GetSiteFlowPushedOrders()
        {
            var shippedOrders = _contextPicsMe.tOrders.Where(o => o.SentToSiteFlow == true && o.SiteflowOrderStatus != "shipped").Select(o => o.ID).ToList();

            return shippedOrders;
        }

        public void PicsMeStatusProcessing(string sourceOrderId, string orderStatus, string json)
        {

            try
            {
                long PicsMesourceOrderId = Convert.ToInt64(sourceOrderId);
                var PicsMeOrder = _contextPicsMe.tOrders.FirstOrDefault(o => o.ID == PicsMesourceOrderId);

                if (PicsMeOrder != null)
                {
                    if (PicsMeOrder.SiteflowOrderStatus == "shipped")
                        return;

                    PicsMeOrder.SiteflowOrderStatus = orderStatus;
                    _contextPicsMe.SaveChanges();
                }
            }
            catch (Exception ex)
            {

               
            }

        }

      
        public bool IsSentToSiteFlow(long orderId)
        {
            var PicsMeOrder = _contextPicsMe.tOrders.FirstOrDefault(o => o.ID == orderId);

            if (PicsMeOrder != null)
                return Convert.ToBoolean(PicsMeOrder.SentToSiteFlow);

            return false;

        }

        public string GetOrderSourceOrderId(long orderId)
        {
            var PicsMeOrder = _contextPicsMe.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (PicsMeOrder != null) return PicsMeOrder.ReferenceNumber;

            return null;
        }

        public long GetOrderIdFromReference(string orderReference)
        {
            var PicsMeOrder = _contextPicsMe.tOrders.FirstOrDefault(o => o.ReferenceNumber == orderReference);

            if (PicsMeOrder != null) return PicsMeOrder.ID;

            return 0;
        }

        public string GetModifiedSiteflowOrderJson(long orderId)
        {
            var PicsMeOrder = _contextPicsMe.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (PicsMeOrder != null) return PicsMeOrder.ModifiedSiteflowJson;

            return null;
        }


        public bool DoesOrderExists(string sourceOrderId)
        {
            var order = _contextPicsMe.tOrders.FirstOrDefault(o => o.ReferenceNumber == sourceOrderId && o.SentToSiteFlow == true);

            if (order == null)
            {
                order = _contextPicsMe.tOrders.FirstOrDefault(o => o.ReferenceNumber == sourceOrderId && (o.SentToSiteFlow == null || o.SentToSiteFlow == false));

                if (order != null)
                {
                    var id = order.ID;

                    var orderDetails = _contextPicsMe.tOrderDetails.Where(od => od.OrderId == id).ToList();

                    foreach (var details in orderDetails)
                    {
                        _contextPicsMe.tOrderDetails.Remove(details);
                    }

                    _contextPicsMe.tOrders.Remove(order);
                    _contextPicsMe.SaveChanges();

                }

                return false;
            }
            else
            {
                return true;
            }
        }


    }
}
