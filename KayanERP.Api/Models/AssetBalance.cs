using System;

namespace KayanERP.Api.Models
{
    public class AssetBalance
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public long ItemId { get; set; }

        // نوع الجهة: "CUSTOMER", "DEPARTMENT", "WAREHOUSE", "SUPPLIER"
        public string PartyType { get; set; } = null!;
        // كود الجهة (رقم العميل أو القسم...)
        public long PartyId { get; set; }

        public decimal Quantity { get; set; }
        public DateTime LastUpdatedAt { get; set; }

        public virtual Company Company { get; set; } = null!;
        public virtual Item Item { get; set; } = null!;
    }
}