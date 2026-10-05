using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    [Table("emergency_tasks", Schema = "erp")]
    public class EmergencyTask
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [MaxLength(50)]
        [Column("task_number")]
        public string? TaskNumber { get; set; }

        [Required, MaxLength(250)]
        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("sender_department")]
        public string SenderDepartment { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("sender_employee_code")]
        public string SenderEmployeeCode { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("sender_employee_name")]
        public string SenderEmployeeName { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        [Column("target_department")]
        public string TargetDepartment { get; set; } = string.Empty;

        [MaxLength(150)]
        [Column("target_employee_name")]
        public string? TargetEmployeeName { get; set; }

        [MaxLength(50)]
        [Column("assigned_technician_code")]
        public string? AssignedTechnicianCode { get; set; }

        [NotMapped]
        public string? TechnicianCode
        {
            get => AssignedTechnicianCode;
            set => AssignedTechnicianCode = value;
        }

        [MaxLength(150)]
        [Column("assigned_technician_name")]
        public string? AssignedTechnicianName { get; set; }

        [NotMapped]
        public string? TechnicianName
        {
            get => AssignedTechnicianName;
            set => AssignedTechnicianName = value;
        }

        [Column("assignment_notes")]
        public string? AssignmentNotes { get; set; }

        [MaxLength(30)]
        [Column("priority")]
        public string Priority { get; set; } = "NORMAL";

        [MaxLength(30)]
        [Column("media_type")]
        public string MediaType { get; set; } = "TEXT";

        [Column("media_url")]
        public string? MediaUrl { get; set; }

        [MaxLength(200)]
        [Column("media_file_name")]
        public string? MediaFileName { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("target_resolution_minutes")]
        public int TargetResolutionMinutes { get; set; } = 30;

        [Column("responded_at")]
        public DateTime? RespondedAt { get; set; }

        [NotMapped]
        public DateTime? StartedAt
        {
            get => RespondedAt;
            set => RespondedAt = value;
        }

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [NotMapped]
        public DateTime? ResolvedAt
        {
            get => CompletedAt;
            set => CompletedAt = value;
        }

        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "PENDING";

        [Column("closure_notes")]
        public string? ClosureNotes { get; set; }

        [NotMapped]
        public string? ResolutionNotes
        {
            get => ClosureNotes;
            set => ClosureNotes = value;
        }

        [Column("closure_proof_media_url")]
        public string? ClosureProofMediaUrl { get; set; }

        [MaxLength(150)]
        [Column("closed_by")]
        public string? ClosedBy { get; set; }

        [MaxLength(50)]
        [Column("closed_by_code")]
        public string? ClosedByCode { get; set; }

        [MaxLength(50)]
        [Column("asset_code")]
        public string? AssetCode { get; set; }

        [MaxLength(150)]
        [Column("cost_impact")]
        public string? CostImpact { get; set; }

        [Column("spare_parts_used")]
        public string? SparePartsUsed { get; set; }

        [Column("root_cause")]
        public string? RootCause { get; set; }

        [Column("downtime_minutes")]
        public int DowntimeMinutes { get; set; } = 0;
    }
}