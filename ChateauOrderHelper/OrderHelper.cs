using ChateauEntity.Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using ChateauEntity.SiteFlowEntity;
using ChateauOrderHelper.Interface;
using ChateauOrderHelper.Model;

namespace ChateauOrderHelper
{

    /// <summary>
    /// ORDER HELPER- CREATE ORDER ITEM IN DATABASE, UPDATE SITEFLOW PUSH STATUS, WRITE LOGS, PROCESS POSTBACKS
    /// </summary>
    public class OrderHelper : IOrderHelper
    {
        private readonly Chateau_V2Entities _contextChateau = new Chateau_V2Entities();
        readonly SiteFlowEntities _contextSiteFlowEntities = new SiteFlowEntities();

        public long CreateOrder(string orderReference, DateTime orderDatetime, decimal orderTotal, decimal deliveryCost, string email, string telephone, string originalJson)
        {
            var order = new tOrders
            {
                OrderStatus = "Order Confirmed",
                CreatedAt = orderDatetime,
                ReferenceNumber = orderReference,
                OriginalSiteflowJson = originalJson
            };

            _contextChateau.tOrders.Add(order);
            _contextChateau.SaveChanges();

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
            _contextChateau.tOrderDetails.Add(orderDetail);
            _contextChateau.SaveChanges();
        }

        public List<string> GetOrdersToPushToSiteFlowManual()
        {
            var orders = _contextChateau.tSiteFlowOrderPushManual.Where(p => p.Processed == null).Select(p => p.SiteFlowReadyOrders).FirstOrDefault();

            if (orders == null)
                return null;

            return orders.Split(',').ToList();

        }

        public List<long> GetOrdersToPushToSiteFlow()
        {
            var orders = _contextChateau.tOrders.Where(o => o.OrderStatus == "Order Confirmed" && (o.SentToSiteFlow == false || o.SentToSiteFlow == null)).Select(o => o.ID).ToList();
            return orders;
        }

        public void WriteLog(string message, long orderId)
        {
            var log = new tSiteFlowResponse
            {
                Response = message,
                OrderId = orderId,
                CreatedAt = DateTime.Now
            };

            _contextChateau.tSiteFlowResponse.Add(log);
            _contextChateau.SaveChanges();
        }

        public void MarkOrderPushedTositeFlow(string orderRef)
        {
            var order = _contextChateau.tOrders.FirstOrDefault(o => o.ReferenceNumber == orderRef);
            if (order != null)
            {
                order.SentToSiteFlow = true;
                order.SiteflowSentDatetme = DateTime.Now;
                _contextChateau.SaveChanges();

                try
                {
                    WriteLog("Siteflow submission success", Convert.ToInt64(orderRef));
                }
                catch
                {
                }
            }


        }

        public void SubmitModifiedSiteflowJson(long orderId, string modifiedSiteflowJson)
        {
            var order = _contextChateau.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (order != null)
            {
                order.ModifiedSiteflowJson = modifiedSiteflowJson;
                _contextChateau.SaveChanges();
            }
        }

        public List<long> GetSiteFlowPushedOrders()
        {
            var shippedOrders = _contextChateau.tOrders.Where(o => o.SentToSiteFlow == true && o.SiteflowOrderStatus != "shipped").Select(o => o.ID).ToList();

            return shippedOrders;
        }

        public void ChateauStatusProcessing(string sourceOrderId, string orderStatus, string json)
        {

            try
            {
                long chateausourceOrderId = Convert.ToInt64(sourceOrderId);
                var chateauOrder = _contextChateau.tOrders.FirstOrDefault(o => o.ID == chateausourceOrderId);

                if (chateauOrder != null)
                {
                    if (chateauOrder.SiteflowOrderStatus == "shipped")
                        return;

                    chateauOrder.SiteflowOrderStatus = orderStatus;
                    _contextChateau.SaveChanges();
                }
            }
            catch (Exception ex)
            {

                tSiteFlowResponse response = new tSiteFlowResponse();

                response.OrderId = Convert.ToInt64(sourceOrderId);
                response.Response = "Site flow postback service error - " + ex.Message + ex.InnerException;
                _contextChateau.tSiteFlowResponse.Add(response);
                _contextChateau.SaveChanges();
            }

        }

        public void MarkManualSiteFlowProcessingComplete()
        {
            var orders = _contextChateau.tSiteFlowOrderPushManual.FirstOrDefault(p => p.Processed == null);
            if (orders != null)
            {
                orders.Processed = true;
                _contextChateau.SaveChanges();
            }

        }

        public bool IsSentToSiteFlow(long orderId)
        {
            var chateauOrder = _contextChateau.tOrders.FirstOrDefault(o => o.ID == orderId);

            if (chateauOrder != null)
                return Convert.ToBoolean(chateauOrder.SentToSiteFlow);

            return false;

        }

        public string GetOrderSourceOrderId(long orderId)
        {
            var chateauOrder = _contextChateau.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (chateauOrder != null) return chateauOrder.ReferenceNumber;

            return null;
        }

        public long GetOrderIdFromReference(string orderReference)
        {
            var chateauOrder = _contextChateau.tOrders.FirstOrDefault(o => o.ReferenceNumber == orderReference);

            if (chateauOrder != null) return chateauOrder.ID;

            return 0;
        }

        public string GetModifiedSiteflowOrderJson(long orderId)
        {
            var chateauOrder = _contextChateau.tOrders.FirstOrDefault(o => o.ID == orderId);
            if (chateauOrder != null) return chateauOrder.ModifiedSiteflowJson;

            return null;
        }

        public void ProcessPostBacks(long orderId)
        {
            var order = _contextChateau.tOrders.FirstOrDefault(o => o.ID == orderId);

            if (order != null)
            {
                var siteFlowOrder = _contextSiteFlowEntities.tSiteFlowInputDatas.FirstOrDefault(i => i.SourceOrderId == order.ReferenceNumber);

                if (siteFlowOrder != null)
                {
                    var status = siteFlowOrder.OrderStatus;

                    order.SiteflowOrderStatus = status;
                    _contextChateau.SaveChanges();

                }
            }

        }

        public bool DoesOrderExists(string sourceOrderId)
        {
            var order = _contextChateau.tOrders.FirstOrDefault(o => o.ReferenceNumber == sourceOrderId && o.SentToSiteFlow == true);

            if (order == null)
            {
                order = _contextChateau.tOrders.FirstOrDefault(o => o.ReferenceNumber == sourceOrderId && (o.SentToSiteFlow == null || o.SentToSiteFlow == false));

                if (order != null)
                {
                    var id = order.ID;

                    var orderDetails = _contextChateau.tOrderDetails.Where(od => od.OrderId == id).ToList();

                    foreach (var details in orderDetails)
                    {
                        _contextChateau.tOrderDetails.Remove(details);
                    }

                    _contextChateau.tOrders.Remove(order);
                    _contextChateau.SaveChanges();

                }

                return false;
            }
            else
            {
                return true;
            }
        }

        public void AddPreOrder(ChateauPreOrder model)
        {
            tChateauPreOrder preOrder = new tChateauPreOrder
            {
                OrderId = Convert.ToInt64(model.OrderId),
                OrderReference = model.OrderReference,
                OrderDetailsReference = model.OrderDetailsReference,
                BarCode = model.BarCode,
                Attribute = model.Attribute,
                Quantity = Convert.ToInt32(model.Quantity),
                ArtworkUrl = model.ArtworkUrl,
                CustomerName = model.CustomerName,
                CustomerAddress1 = model.CustomerAddress1,
                CustomerAddress2 = model.CustomerAddress2,
                CustomerAddress3 = model.CustomerAddress3,
                CustomerTown = model.CustomerTown,
                CustomerState = model.CustomerState,
                CustomerPostcode = model.CustomerPostcode,
                CustomerCountry = model.CustomerCountry,
                CustomerEmail = model.CustomerEmail,
                CustomerCompanyName = model.CustomerCompanyName,
                CustomerPhone = model.CustomerPhone,
                EmailSentToProduction = false
            };

            _contextChateau.tChateauPreOrder.Add(preOrder);
            _contextChateau.SaveChanges();
        }


        public void AddKnife(ChateauKnivesReportData model)
        {
            tChateauKnives knives = new tChateauKnives
            {
                OrderId = Convert.ToInt64(model.OrderId),
                OrderReference = model.OrderReference,
                OrderDetailsReference = model.OrderDetailsReference,
                BarCode = model.BarCode,
                Attribute = model.Attribute,
                Quantity = Convert.ToInt32(model.Quantity),
                ArtworkUrl = model.ArtworkUrl,
                CustomerName = model.CustomerName,
                CustomerAddress1 = model.CustomerAddress1,
                CustomerAddress2 = model.CustomerAddress2,
                CustomerAddress3 = model.CustomerAddress3,
                CustomerTown = model.CustomerTown,
                CustomerState = model.CustomerState,
                CustomerPostcode = model.CustomerPostcode,
                CustomerCountry = model.CustomerCountry,
                CustomerEmail = model.CustomerEmail,
                CustomerCompanyName = model.CustomerCompanyName,
                CustomerPhone = model.CustomerPhone,
                EmailSentToProduction = false
            };

            _contextChateau.tChateauKnives.Add(knives);
            _contextChateau.SaveChanges();
        }




        public List<BelfieldModel> ExtractBelfieldReportData()
        {
            var belfield = _contextChateau.tChateauBelfield.Where(k => k.EmailSentToProduction == false).ToList();

            return belfield.Select(data => new BelfieldModel
            {
                Id = data.Id,
                OrderId = Convert.ToInt64(data.OrderId),
                OrderReference = data.OrderReference,
                OrderDetailsReference = data.OrderDetailsReference,
                BarCode = data.BarCode,
                AttributeDesignCode = data.AttributeDesignCode,
                AttributeLength = data.AttributeLength,
                Quantity = Convert.ToInt32(data.Quantity),
                ArtworkUrl = data.ArtworkUrl

            })
                .ToList();
        }



        public List<ChateauPreOrder> ExtractPreOrderReportData()
        {
            var preOrder = _contextChateau.tChateauPreOrder.Where(k => k.EmailSentToProduction == false).ToList();

            return preOrder.Select(preo => new ChateauPreOrder
            {
                Id = preo.Id,
                OrderId = Convert.ToString(preo.OrderId),
                OrderReference = preo.OrderReference,
                OrderDetailsReference = preo.OrderDetailsReference,
                BarCode = preo.BarCode,
                Attribute = preo.Attribute,
                Substrate = preo.Substrate,
                Quantity = Convert.ToString(preo.Quantity),
                ArtworkUrl = preo.ArtworkUrl,
                CustomerName = preo.CustomerName,
                CustomerAddress1 = preo.CustomerAddress1,
                CustomerAddress2 = preo.CustomerAddress2,
                CustomerAddress3 = preo.CustomerAddress3,
                CustomerTown = preo.CustomerTown,
                CustomerState = preo.CustomerState,
                CustomerPostcode = preo.CustomerPostcode,
                CustomerCountry = preo.CustomerCountry,
                CustomerEmail = preo.CustomerEmail,
                CustomerCompanyName = preo.CustomerCompanyName,
                CustomerPhone = preo.CustomerPhone
            })
                .ToList();
        }


        public List<ChateauKnivesReportData> ExtractKnifeReportData()
        {
            var knives = _contextChateau.tChateauKnives.Where(k => k.EmailSentToProduction == false).ToList();

            return knives.Select(knife => new ChateauKnivesReportData
            {
                Id = knife.Id,
                OrderId = Convert.ToString(knife.OrderId),
                OrderReference = knife.OrderReference,
                OrderDetailsReference = knife.OrderDetailsReference,
                BarCode = knife.BarCode,
                Attribute = knife.Attribute,
                Quantity = Convert.ToString(knife.Quantity),
                ArtworkUrl = knife.ArtworkUrl,
                CustomerName = knife.CustomerName,
                CustomerAddress1 = knife.CustomerAddress1,
                CustomerAddress2 = knife.CustomerAddress2,
                CustomerAddress3 = knife.CustomerAddress3,
                CustomerTown = knife.CustomerTown,
                CustomerState = knife.CustomerState,
                CustomerPostcode = knife.CustomerPostcode,
                CustomerCountry = knife.CustomerCountry,
                CustomerEmail = knife.CustomerEmail,
                CustomerCompanyName = knife.CustomerCompanyName,
                CustomerPhone = knife.CustomerPhone
            })
                .ToList();
        }




        public void MarkBelfieldSentToProduction(long id)
        {
            var belfield = _contextChateau.tChateauBelfield.FirstOrDefault(k => k.Id == id);

            if (belfield != null)
            {
                belfield.EmailSentToProduction = true;
                belfield.EmailSentDatetime = DateTime.Now;
                _contextChateau.SaveChanges();
            }
        }


        public void MarkPreOrderSentToProduction(long id)
        {
            var preorder = _contextChateau.tChateauPreOrder.FirstOrDefault(k => k.Id == id);

            if (preorder != null)
            {
                preorder.EmailSentToProduction = true;
                preorder.EmailSentDatetime = DateTime.Now;
                _contextChateau.SaveChanges();
            }
        }
        public void MarkKnifeSentToProduction(long id)
        {
            var knife = _contextChateau.tChateauKnives.FirstOrDefault(k => k.Id == id);

            if (knife != null)
            {
                knife.EmailSentToProduction = true;
                knife.EmailSentDatetime = DateTime.Now;
                _contextChateau.SaveChanges();
            }
        }

        public void AddBelfield(BelfieldModel model)
        {
            tChateauBelfield belfield = new tChateauBelfield
            {
                OrderId = Convert.ToInt64(model.OrderId),
                OrderReference = model.OrderReference,
                OrderDetailsReference = model.OrderDetailsReference,
                BarCode = model.BarCode,
                AttributeDesignCode = model.AttributeDesignCode,
                AttributeLength = model.AttributeLength,
                Quantity = Convert.ToInt32(model.Quantity),
                ArtworkUrl = model.ArtworkUrl,
                CreatedAt = System.DateTime.Now,
                EmailSentToProduction = false
            };

            _contextChateau.tChateauBelfield.Add(belfield);
            _contextChateau.SaveChanges();
        }

        public void MarkOrdersProcessed(List<string> distinctOrderDetailsReferenceBelfield)
        {

            foreach (var reference in distinctOrderDetailsReferenceBelfield)
            {

                var belfield = _contextChateau.tChateauBelfield.FirstOrDefault(o => o.OrderDetailsReference == reference);

                if (belfield != null)
                {
                    belfield.PDFSentToPrinergy = true;
                    belfield.PDFPrinergyOutputProcessed = true;
                    belfield.DateSentToPrinergy = System.DateTime.Now;
                    belfield.PrinergyOutputProcessedDatetime = System.DateTime.Now;
                    _contextChateau.SaveChanges();
                }

            }
        }

        public void DeleteKnives(string sourceOrderId)
        {
            var chateauKniveses = _contextChateau.tChateauKnives.Where(o => o.OrderReference == sourceOrderId).ToList();

            if (chateauKniveses.Count > 0)
            {
                foreach (var knives in chateauKniveses)
                {
                    _contextChateau.tChateauKnives.Remove(knives);
                }

                _contextChateau.SaveChanges();
            }
        }

        public void DeletePreOrder(string sourceOrderId)
        {
            var preOrderItems = _contextChateau.tChateauPreOrder.Where(o => o.OrderReference == sourceOrderId).ToList();

            if (preOrderItems.Count > 0)
            {
                foreach (var preorder in preOrderItems)
                {
                    _contextChateau.tChateauPreOrder.Remove(preorder);
                }

                _contextChateau.SaveChanges();
            }
        }

        public void DeleteBelfield(string sourceOrderId)
        {
            var holdingFolderDir = ConfigurationManager.AppSettings["BelfieldHolidingFolderPath"];

            var belfieldItems = _contextChateau.tChateauBelfield.Where(o => o.OrderReference == sourceOrderId).ToList();

            if (belfieldItems.Count > 0)
            {
                foreach (var belfield in belfieldItems)
                {
                    _contextChateau.tChateauBelfield.Remove(belfield);

                    for (int i = 1; i <= belfield.Quantity; i++)
                    {
                        var file = holdingFolderDir + belfield.OrderReference + "_" + belfield.OrderDetailsReference +
                                   "_" + i + ".PDF";

                        try
                        {
                            if (File.Exists(file))
                                File.Delete(file);
                        }
                        catch { }


                    }

                    _contextChateau.SaveChanges();

                }


            }
        }

        public string GetDesignCode(string orderDetailsRef)
        {
            var belfield =
                _contextChateau.tChateauBelfield.FirstOrDefault(b => b.OrderDetailsReference == orderDetailsRef);

            if (belfield != null)
                return belfield.AttributeDesignCode;

            return null;
        }
    }
}
