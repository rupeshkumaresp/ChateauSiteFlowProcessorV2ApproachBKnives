﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class MediaClipEntities : DbContext
    {
        public MediaClipEntities()
            : base("name=MediaClipEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<tCustomer> tCustomer { get; set; }
        public virtual DbSet<tMediaClipOrder> tMediaClipOrder { get; set; }
        public virtual DbSet<tMediaClipOrderDetails> tMediaClipOrderDetails { get; set; }
        public virtual DbSet<tMediaClipOrderExtrinsic> tMediaClipOrderExtrinsic { get; set; }
    }
}
