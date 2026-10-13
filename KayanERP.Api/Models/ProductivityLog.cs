using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models
{
    [Table("productivity_logs")]
    public class ProductivityLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("production_order_id")]
        public long ProductionOrderId { get; set; }

        [Column("employee_id")]
        public long EmployeeId { get; set; }

        [Column("work_date")]
        public DateTime WorkDate { get; set; } = DateTime.UtcNow;

        // حالة الحضور للوردية: حاضر / غائب / إجازة
        [Column("attendance_status")]
        [MaxLength(20)]
        public string AttendanceStatus { get; set; } = "حاضر";

        // أوقات الحضور والانصراف المرنة للوردية
        [Column("check_in")]
        public TimeOnly? CheckIn { get; set; }

        [Column("check_out")]
        public TimeOnly? CheckOut { get; set; }

        [Column("hours_worked", TypeName = "decimal(18,2)")]
        public decimal HoursWorked { get; set; } = 0;

        // لقطة لحظية من أجر الساعة وقت التشغيل (Snapshot)
        [Column("hourly_rate", TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; } = 0;

        // التكلفة الفعلية المحسوبة للعامل في هذا الأمر = الساعات * أجر الساعة
        [Column("labor_cost", TypeName = "decimal(18,2)")]
        public decimal LaborCost { get; set; } = 0;

        [Column("produced_quantity", TypeName = "decimal(18,3)")]
        public decimal ProducedQuantity { get; set; } = 0;

        // كمية الهالك المنسوبة للعامل في حال الإهمال التشغيلي
        [Column("waste_quantity", TypeName = "decimal(18,3)")]
        public decimal WasteQuantity { get; set; } = 0;

        // تصنيف الهالك: طبيعي تقطيع / إهمال عامل / عطل ماكينة / خامة معيبة
        [Column("waste_type")]
        [MaxLength(50)]
        public string? WasteType { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder? ProductionOrder { get; set; }
    }
}