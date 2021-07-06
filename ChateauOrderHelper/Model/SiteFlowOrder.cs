using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ChateauOrderHelper.Model
{
    [Serializable]
    public class SiteflowOrder
    {
        [Serializable]
        public class Destination
        {
            public string name { get; set; }
        }

        [Serializable]
        public class Attributes
        {
            public string ProductCode { get; set; }
            public string ProductFinishedPageSize { get; set; }
            public string TourProduct { get; set; }
            public string DesignCode { get; set; }
            public string Length { get; set; }
            [JsonProperty(PropertyName = "Size For Impo")]
            public string SizeForImpo { get; set; }
            public string Substrate { get; set; }
            public string CoverType { get; set; }
            public string PageDesign { get; set; }
            public string CardDesign { get; set; }
            public int? Pages { get; set; }

            public string Country { get; set; }
            public string RUSH { get; set; }
            public string StockCoverType { get; set; }

            public string StationeryStyle { get; set; }
            public string StationeryType { get; set; }

            public int? SheetQuantity { get; set; }
        }
        [Serializable]
        public class Component
        {
            public bool fetch { get; set; }
            public bool localFile { get; set; }
            public Attributes attributes { get; set; }
            public string barcode { get; set; }
            public string code { get; set; }
            public string componentId { get; set; }
            public string path { get; set; }
            public int? Pages { get; set; }
        }
        [Serializable]
        public class Item
        {
            public List<Component> components { get; set; }
            public int shipmentIndex { get; set; }
            public string barcode { get; set; }
            public string sourceItemId { get; set; }
            public int quantity { get; set; }
            public decimal unitWeight { get; set; }
            public string sku { get; set; }
            public string isoCountryOfOrigin { get; set; }
            public string harmonizedCode { get; set; }
            public double unitPrice { get; set; }
        }
        [Serializable]
        public class ShipTo
        {
            public string name { get; set; }
            public string address1 { get; set; }
            public string address2 { get; set; }
            public string address3 { get; set; }
            public string town { get; set; }
            public string state { get; set; }
            public string postcode { get; set; }
            public string isoCountry { get; set; }
            public string country { get; set; }
            public string email { get; set; }
            public string companyName { get; set; }
            public string phone { get; set; }
        }
        [Serializable]
        public class Carrier
        {
            //public string code { get; set; }
            //public string service { get; set; }
            public string alias { get; set; }
        }
        [Serializable]
        public class Cost
        {
            public double value { get; set; }
            public string currency { get; set; }
        }

        [Serializable]
        public class Shipment
        {
            public ShipTo shipTo { get; set; }
            public Carrier carrier { get; set; }
            public Cost cost { get; set; }
            public string unitWeight { get; set; }
            public int shipmentIndex { get; set; }
            public string ShipByDate { get; set; }
            public int slaDays { get; set; }
            public bool canShipEarly { get; set; }
            public bool pspBranding { get; set; }
        }
        [Serializable]
        public class OrderData
        {
            public List<Shipment> shipments { get; set; }
            public List<Item> items { get; set; }
            //public string sourceId { get; set; }
            public string printType { get; set; }
            public string sourceOrderId { get; set; }
            public string email { get; set; }
            public decimal amount { get; set; }
            public string customerName { get; set; }
            public DateTime slaTimestamp { get; set; }
        }
        [Serializable]
        public class RootObject
        {
            public OrderData orderData { get; set; }
            public Destination destination { get; set; }

        }

    }
}
