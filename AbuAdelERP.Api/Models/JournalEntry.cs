using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("journal_entries")]
public class JournalEntry
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Required]
    [Column("entry_number")]
    [MaxLength(50)]
    public string EntryNumber { get; set; } = string.Empty;

    [Column("entry_date")]
    public DateOnly EntryDate { get; set; }

    [Column("reference_type")]
    [MaxLength(50)]
    public string ReferenceType { get; set; } = string.Empty; // مثل: PRODUCTION_ORDER

    [Column("reference_id")]
    public long? ReferenceId { get; set; } // رقم أمر الإنتاج الذي تسبب في هذا القيد

    [Column("notes")]
    public string Notes { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // الربط مع السطور
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}