using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs;

public class InitialStockDto
{
    [Required]
    public long ItemId { get; set; }

    [Required]
    public long WarehouseId { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public string Notes { get; set; } = "رصيد افتتاحي تجريبي";
}