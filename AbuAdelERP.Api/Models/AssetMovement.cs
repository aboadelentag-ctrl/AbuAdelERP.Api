using System;

namespace AbuAdelERP.Api.Models
{
    public class AssetMovement
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public DateTime MovementDate { get; set; }
        public long ItemId { get; set; }

        // الجهة المُسلِمة (من)
        public string FromPartyType { get; set; } = null!;
        public long FromPartyId { get; set; }

        // الجهة المُستلِمة (إلى)
        public string ToPartyType { get; set; } = null!;
        public long ToPartyId { get; set; }

        public decimal Quantity { get; set; }

        // نوع الحركة: "ISSUE" (صرف), "RETURN" (استرداد), "TRANSFER" (نقل), "WRITE_OFF" (إهلاك)
        public string MovementType { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual Company Company { get; set; } = null!;
        public virtual Item Item { get; set; } = null!;
    }
}