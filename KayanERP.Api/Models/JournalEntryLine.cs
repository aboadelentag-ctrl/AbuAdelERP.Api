using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KayanERP.Api.Models;

[Table("journal_entry_lines")]
public class JournalEntryLine
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("journal_entry_id")]
    public long JournalEntryId { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("debit", TypeName = "decimal(18,4)")]
    public decimal Debit { get; set; } = 0; // الجانب المدين

    [Column("credit", TypeName = "decimal(18,4)")]
    public decimal Credit { get; set; } = 0; // الجانب الدائن

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // العلاقات
    [ForeignKey("JournalEntryId")]
    public JournalEntry? JournalEntry { get; set; }

    [ForeignKey("AccountId")]
    public Account? Account { get; set; }
}