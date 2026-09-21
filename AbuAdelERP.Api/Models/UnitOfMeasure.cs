using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbuAdelERP.Api.Models;

[Table("unit_of_measures")]
public class UnitOfMeasure
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("name_ar")]
    public string NameAr { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}