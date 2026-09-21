using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models
{
    public class CrateClientTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CrateClientId { get; set; }

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = string.Empty; // "منصرف للعميل" أو "مستلم من العميل"

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        // ربط الحركة بالعميل
        [ForeignKey("CrateClientId")]
        public virtual CrateClient? CrateClient { get; set; }
    }
}