﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChateauOrderHelper.Model
{
    public class ChateauPreOrder
    {

        public long Id { get; set; }
        public string OrderId { get; set; }
        public string OrderReference { get; set; }
        public string OrderDetailsReference { get; set; }
        public string BarCode { get; set; }
        public string Attribute { get; set; }
        public string Substrate { get; set; }
        public string Quantity { get; set; }
        public string ArtworkUrl { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress1 { get; set; }
        public string CustomerAddress2 { get; set; }
        public string CustomerAddress3 { get; set; }
        public string CustomerTown { get; set; }
        public string CustomerState { get; set; }
        public string CustomerPostcode { get; set; }
        public string CustomerCountry { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerCompanyName { get; set; }
        public string CustomerPhone { get; set; }
    }
}
