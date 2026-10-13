using System;

namespace KayanERP.Api.DTOs;

// DTOs معاملات تحويل الوحدات
public class UomConversionDto
{
    public long Id { get; set; }
    public long? ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsGlobal => ItemId == null;
}

public class UomConversionCreateDto
{
    public long? ItemId { get; set; }
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; }
    public string Notes { get; set; } = string.Empty;
}

// DTOs تفريعات الأصناف
public class ItemVariantDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public long? ParentItemId { get; set; }
    public string ParentItemName { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0;
    public string? Barcode { get; set; }
    public bool Active { get; set; }
}

public class ItemVariantCreateDto
{
    public long ParentItemId { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string Unit { get; set; } = "قطعة";
    public decimal PackingFactor { get; set; } = 48;
    public string PackingUnit { get; set; } = "بلكة";
    public decimal Price { get; set; } = 0;
    public string? Barcode { get; set; }
}

public class ParentItemLookupDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}