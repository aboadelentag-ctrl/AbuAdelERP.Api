using System;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.Models
{
    public class EmergencyTask
    {
        public long Id { get; set; }

        [MaxLength(50)]
        public string? TaskNumber { get; set; } // اختياري ليتم توليده آلياً من السيرفر

        [Required(ErrorMessage = "عنوان المشكلة مطلوب")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // الأقسام والموظفين
        public string SenderDepartment { get; set; } = string.Empty;
        public string SenderEmployeeName { get; set; } = string.Empty;

        public string TargetDepartment { get; set; } = string.Empty;
        public string? TargetEmployeeName { get; set; }

        // التكليف والإحالة الميدانية
        public string? AssignedTechnicianName { get; set; }
        public string? AssignmentNotes { get; set; }
        public DateTime? AssignedAt { get; set; }

        // الأولوية والنوع
        public string Priority { get; set; } = "NORMAL"; // CRITICAL, HIGH, NORMAL

        // الوسائط المتعددة (صور، صوت، فيديو)
        public string MediaType { get; set; } = "TEXT";
        public string? MediaUrl { get; set; }
        public string? MediaFileName { get; set; }

        // التوقيتات وحساب الـ SLA
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int TargetResolutionMinutes { get; set; } = 30;
        public DateTime? RespondedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // الحالة التشغيلية
        public string Status { get; set; } = "PENDING"; // PENDING, ASSIGNED, IN_PROGRESS, CLOSED

        // إثبات الإغلاق بالدليل
        public string? ClosureNotes { get; set; }
        public string? ClosureProofMediaUrl { get; set; }
        public string? ClosedBy { get; set; }
    }
}