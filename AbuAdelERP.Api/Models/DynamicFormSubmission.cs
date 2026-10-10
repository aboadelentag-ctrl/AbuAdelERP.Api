using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    [Table("DynamicFormSubmissions")]
    public class DynamicFormSubmission
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public long FormId { get; set; }

        [ForeignKey("FormId")]
        public virtual DynamicForm? Form { get; set; }

        public string SubmissionData { get; set; } = "{}";

        public long UserId { get; set; } = 1;

        public string SubmittedByName { get; set; } = string.Empty;

        public string Status { get; set; } = "معتمد";

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}