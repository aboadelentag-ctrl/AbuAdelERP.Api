using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs
{
    // ==========================================
    // 1. القوائم المرجعية والأرصدة
    // ==========================================
    public class ContainerLookupDto
    {
        public int Id { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public string? Specification { get; set; }
        public int TotalFactoryQuantity { get; set; }
    }

    public class CrateLookupsDto
    {
        public List<ContainerLookupDto> Containers { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public List<string> Warehouses { get; set; } = new();
    }

    public class CirculatingContainerViewDto
    {
        public int Id { get; set; }
        public string ContainerName { get; set; } = string.Empty;
        public string? Specification { get; set; }
        public int TotalFactoryQuantity { get; set; }
        public int InPackagingWarehouseQuantity { get; set; }
        public int WithDriversAndBranchesQuantity { get; set; }
        public int DisputedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
    }

    // ==========================================
    // 2. دورة الصرف والاستلام الميداني
    // ==========================================
    public class DispatchCrateDto
    {
        [Required(ErrorMessage = "يجب تحديد نوع الحركة.")]
        public string MovementType { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب تحديد نوع الحاوية/السبت.")]
        public string ContainerName { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "الكمية المصروفة يجب أن تكون أكبر من الصفر.")]
        public int DispatchedQuantity { get; set; }

        [Required(ErrorMessage = "يجب تحديد موقع الصرف.")]
        public string SourceLocation { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب تحديد موقع الاستلام.")]
        public string DestinationLocation { get; set; } = string.Empty;

        public string? DispatchedByCode { get; set; }
        public string? DispatchedByName { get; set; }
        public string? Notes { get; set; }
    }

    public class ReceiveCrateDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "الكمية المستلمة لا يمكن أن تكون سالبة.")]
        public int ActualReceivedQuantity { get; set; }

        public int DamagedCount { get; set; }
        public string? ReceivedByCode { get; set; }
        public string? ReceivedByName { get; set; }
        public string? ReceiveNotes { get; set; }
    }

    public class SettleDisputeDto
    {
        [Required(ErrorMessage = "يجب تحديد القرار التحكيمي.")]
        public string ArbitrationDecision { get; set; } = string.Empty;

        [Required(ErrorMessage = "يجب إدخال نص القرار ومحضر التسوية.")]
        public string ArbitrationText { get; set; } = string.Empty;

        public decimal FinancialPenalty { get; set; }
        public string? ArbitratedByCode { get; set; }
        public string? ArbitratedByName { get; set; }
    }

    public class CrateMovementViewDto
    {
        public long Id { get; set; }
        public string MovementNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public int DispatchedQuantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public int ShortageQuantity { get; set; }
        public int DamagedCount { get; set; }
        public string SourceLocation { get; set; } = string.Empty;
        public string DestinationLocation { get; set; } = string.Empty;
        public string DispatchedByCode { get; set; } = string.Empty;
        public string DispatchedByName { get; set; } = string.Empty;
        public string? ReceivedByCode { get; set; }
        public string? ReceivedByName { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? ReceiveNotes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ArbitrationDecision { get; set; }
        public string? ArbitrationText { get; set; }
        public string? FinancialPenalty { get; set; }
    }

    // ==========================================
    // 3. معاملات عهد العملاء والمصنع
    // ==========================================
    public class CreateCrateClientTransactionDto
    {
        [Required(ErrorMessage = "يجب تحديد معرف العميل.")]
        public int CrateClientId { get; set; }

        [Required(ErrorMessage = "يجب تحديد نوع الحركة (منصرف للعميل / مستلم من العميل).")]
        public string TransactionType { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من الصفر.")]
        public int Quantity { get; set; }

        public string? Notes { get; set; }
        public string? DriverName { get; set; }
    }

    public class CrateClientTransactionViewDto
    {
        public int Id { get; set; }
        public int CrateClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? Notes { get; set; }
        public string? DriverName { get; set; }
        public int ClientCurrentBalance { get; set; }
    }
}