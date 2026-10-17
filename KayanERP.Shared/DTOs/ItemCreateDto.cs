using System;

namespace KayanERP.Shared.DTOs;

public class ItemCreateDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public long? DepartmentId { get; set; }
    public long BaseUomId { get; set; }
    public string ItemType { get; set; } = string.Empty;
    public string Unit { get; set; } = "كجم";
    public bool HasExpiryDate { get; set; } = true;
    public int? ShelfLifeDays { get; set; }
    public decimal ReorderLevel { get; set; } = 0;
    public decimal SafetyStock { get; set; } = 0;
    public decimal MaxStock { get; set; } = 0;

    // الخصائص التوافقية مع نظام كيان وقاعدة البيانات
    public string? DepartmentName { get; set; }
    public decimal BasePrice { get; set; } = 0;
    public string? Barcode { get; set; }
    public bool IsVariant { get; set; } = false;
    public bool IsMerged { get; set; } = false;
    public bool IsContainer { get; set; } = false;
    public bool MultiUom { get; set; } = false;
    public bool HideDetailPrint { get; set; } = false;
}

public class ItemDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public long CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public decimal BasePrice { get; set; } = 0;
    public string? Barcode { get; set; }
    public bool IsVariant { get; set; }
    public bool IsMerged { get; set; }
    public bool IsContainer { get; set; }
    public bool MultiUom { get; set; }
    public bool HideDetailPrint { get; set; }
    public bool HasExpiryDate { get; set; }
    public int? ShelfLifeDays { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal MaxStock { get; set; }
    public bool Active { get; set; }
    public long? ParentItemId { get; set; }
    public string? ParentItemName { get; set; }
}