using System.ComponentModel.DataAnnotations;

namespace KayanERP.Api.DTOs;

public class CrateTransactionCreateDto
{
    [Required]
    public long CrateId { get; set; } // رقم السبت (الصنف)

    [Required]
    public string TransactionType { get; set; } = null!; // نوع الحركة: DISPATCH (منصرف) | RETURN (مرتجع) | DAMAGE (هالك)

    [Required]
    public string PartyType { get; set; } = null!; // الجهة: BRANCH (فرع) | CUSTOMER (عميل)

    [Required]
    public long PartyId { get; set; } // رقم الفرع أو العميل

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public int Quantity { get; set; }

    public string? Notes { get; set; }
}