namespace KayanERP.Api.DTOs;

public class WarehouseCreateDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = "GENERAL"; // أنواع مقترحة: RAW, FINISHED, GENERAL
    public string? Description { get; set; }
}