//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PicsMeEntity.MediaClipEntity
{
    using System;
    using System.Collections.Generic;
    
    public partial class tMediaClipOrderDetails
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tMediaClipOrderDetails()
        {
            this.tMediaClipOrderExtrinsic = new HashSet<tMediaClipOrderExtrinsic>();
        }
    
        public long OrderDetailsId { get; set; }
        public long MediaClipOrderId { get; set; }
        public string SupplierPartAuxilliaryId { get; set; }
        public int LineNumber { get; set; }
        public int Quantity { get; set; }
    
        public virtual tMediaClipOrder tMediaClipOrder { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tMediaClipOrderExtrinsic> tMediaClipOrderExtrinsic { get; set; }
    }
}