using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChateauOrderHelper.Model
{
    public class BelfieldModel
    {
        public long Id { get; set; }
        public Nullable<long> OrderId { get; set; }
        public string OrderReference { get; set; }
        public string OrderDetailsReference { get; set; }
        public string BarCode { get; set; }
        public string AttributeDesignCode { get; set; }
        public string AttributeLength { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string ArtworkUrl { get; set; }

    }
}
