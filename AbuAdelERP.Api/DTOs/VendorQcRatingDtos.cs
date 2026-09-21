using System;
using System.Collections.Generic;

namespace AbuAdelERP.Api.DTOs
{
    public class VendorQcScoreDto
    {
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int TotalShipments { get; set; }
        public decimal TotalReceivedQty { get; set; }
        public decimal TotalAcceptedQty { get; set; }
        public decimal TotalRejectedQty { get; set; }
        public decimal AcceptanceRate { get; set; } // نسبة القبول المئوية %
        public string QualityGrade { get; set; } = "A"; // A: ممتاز >= 95% | B: جيد 85-94% | C: حرج < 85%
        public int RejectionIncidentsCount { get; set; }
    }

    public class QcInspectionHistoryDto
    {
        public long MovementId { get; set; }
        public DateTime InspectionDate { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}