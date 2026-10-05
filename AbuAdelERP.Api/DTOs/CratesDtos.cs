using System;

namespace AbuAdelERP.Api.DTOs
{
    // 1. إذن صرف وتسليم الأسبات (طرف أول مُسلِّم)
    public class DispatchCrateDto
    {
        public string MovementType { get; set; } = "DISPATCH_BRANCH"; // DISPATCH_BRANCH | RETURN_FACTORY
        public string ContainerName { get; set; } = string.Empty;
        public int DispatchedQuantity { get; set; }
        public string SourceLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? DispatchedByCode { get; set; }
        public string? DispatchedByName { get; set; }
    }

    // 2. فحص وتأكيد الاستلام الميداني (طرف ثانٍ مُستلِم)
    public class ReceiveCrateDto
    {
        public int ActualReceivedQuantity { get; set; }
        public int DamagedCount { get; set; }
        public string? ReceiveNotes { get; set; }
        public string? ReceivedByCode { get; set; }
        public string? ReceivedByName { get; set; }
    }

    // 3. محضر فض وتسوية النزاع والعجز (لجنة الرقابة والتحكيم)
    public class SettleDisputeDto
    {
        public string ArbitrationDecision { get; set; } = string.Empty;
        public string ArbitrationText { get; set; } = string.Empty;
        public decimal FinancialPenalty { get; set; }
        public string? ArbitratedByCode { get; set; }
        public string? ArbitratedByName { get; set; }
    }
}