using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models
{
    [Table("fixed_assets", Schema = "erp")]
    public class FixedAsset
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required, MaxLength(50)]
        [Column("asset_code")]
        public string AssetCode { get; set; } = string.Empty; // مثل: AST-OVN-01

        [Required, MaxLength(200)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("department")]
        public string Department { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("category")]
        public string? Category { get; set; } = "أفران وتسوية";

        [MaxLength(150)]
        [Column("brand_model")]
        public string? BrandModel { get; set; }

        [MaxLength(100)]
        [Column("serial_number")]
        public string? SerialNumber { get; set; }

        [MaxLength(50)]
        [Column("custodian_code")]
        public string CustodianCode { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("custodian_name")]
        public string CustodianName { get; set; } = string.Empty;

        [Column("installation_date")]
        public DateTime InstallationDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "OPERATIONAL"; // OPERATIONAL, UNDER_MAINTENANCE, OUT_OF_SERVICE, SCRAPPED

        [MaxLength(50)]
        [Column("active_sos_task_number")]
        public string? ActiveSOSTaskNumber { get; set; }

        // العلاقة مع سجل حياة الأصل
        public virtual ICollection<AssetHistoryLog> HistoryLogs { get; set; } = new List<AssetHistoryLog>();
    }

    [Table("asset_history_logs", Schema = "erp")]
    public class AssetHistoryLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("fixed_asset_id")]
        public long FixedAssetId { get; set; }

        [ForeignKey(nameof(FixedAssetId))]
        [JsonIgnore] // يمنع حلقة التكرار الدائرية (Object Cycle) عند تحويل الكائن إلى JSON
        public virtual FixedAsset? FixedAsset { get; set; }

        [Column("date")]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required, MaxLength(100)]
        [Column("event_type")]
        public string EventType { get; set; } = "صيانة"; // شراء وتركيب, صيانة وقائية, عطل طارئ (SOS), نقل عهدة

        [Required, MaxLength(500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("performed_by_code")]
        public string? PerformedByCode { get; set; }

        [MaxLength(150)]
        [Column("performed_by_name")]
        public string? PerformedByName { get; set; }

        [MaxLength(100)]
        [Column("cost_impact")]
        public string? CostImpact { get; set; } = "---";
    }
}