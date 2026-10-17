namespace KayanERP.Shared.DTOs;

public class StockMovementCreateDto
{
    public long ItemId { get; set; }
    public long WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Notes { get; set; }
}