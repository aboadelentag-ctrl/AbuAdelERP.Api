using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    [Table("productivity_logs")]
    public class ProductivityLog
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("production_order_id")]
        public long ProductionOrderId { get; set; }

        // التعديل: توحيد المسمى إلى EmployeeId
        [Column("employee_id")]
        public long EmployeeId { get; set; }

        [Column("work_date")]
        public DateTime WorkDate { get; set; }

        [Column("hours_worked", TypeName = "decimal(18,2)")]
        public decimal HoursWorked { get; set; }

        [Column("produced_quantity", TypeName = "decimal(18,3)")]
        public decimal ProducedQuantity { get; set; }

        [Column("notes")]
        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        // التعديل: ربط المفتاح الأجنبي بكيان الموظفين الجديد
        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }

        [ForeignKey("ProductionOrderId")]
        public virtual ProductionOrder? ProductionOrder { get; set; }
    }
}