using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    [Table("CirculatingContainers", Schema = "erp")]
    public class CirculatingContainer
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string ContainerName { get; set; } = string.Empty; // مثل: أسبات بلاستيكية خضراء (كبير)

        [MaxLength(200)]
        public string Specification { get; set; } = string.Empty;

        public int TotalFactoryQuantity { get; set; }
        public int InProductionFloorQuantity { get; set; }
        public int InPackagingWarehouseQuantity { get; set; }
        public int WithDriversAndBranchesQuantity { get; set; }
        public int DisputedQuantity { get; set; } // المعلق بمحاضر نزاع
        public int DamagedQuantity { get; set; }  // الهالك والكسر المثبت
    }

    [Table("CrateMovements", Schema = "erp")]
    public class CrateMovement
    {
        [Key]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        public string MovementNumber { get; set; } = string.Empty; // مثل: CRT-20261001-001

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(50)]
        public string MovementType { get; set; } = "DISPATCH_BRANCH"; // DISPATCH_BRANCH, FLOOR_TRANSFER, RETURN_FACTORY

        [Required, MaxLength(150)]
        public string ContainerName { get; set; } = string.Empty;

        public int DispatchedQuantity { get; set; }

        [Required, MaxLength(150)]
        public string SourceLocation { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string DispatchedByCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string DispatchedByName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string DestinationLocation { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // بيانات تأكيد الاستلام الميداني
        public int? ReceivedQuantity { get; set; }
        public int ShortageQuantity { get; set; } = 0;
        public int DamagedCount { get; set; } = 0;
        public DateTime? ReceivedAt { get; set; }

        [MaxLength(50)]
        public string? ReceivedByCode { get; set; }

        [MaxLength(150)]
        public string? ReceivedByName { get; set; }

        [MaxLength(500)]
        public string? ReceiveNotes { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = "PENDING_RECEIPT"; // PENDING_RECEIPT, COMPLETED, DISPUTED, RESOLVED

        // بيانات قرار فض المنازعات
        [MaxLength(100)]
        public string? ArbitrationDecision { get; set; } // CHARGE_CARRIER, CHARGE_DISPATCHER, CHARGE_RECEIVER, APPROVE_SCRAP

        [MaxLength(500)]
        public string? ArbitrationText { get; set; }

        [MaxLength(50)]
        public string? ArbitratedByCode { get; set; }

        [MaxLength(150)]
        public string? ArbitratedByName { get; set; }

        [MaxLength(100)]
        public string? FinancialPenalty { get; set; }
    }
}