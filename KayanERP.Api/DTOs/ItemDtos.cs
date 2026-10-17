using System.Collections.Generic;

namespace KayanERP.Api.DTOs;

// ==============================================================
// 1. DTOs خاصة بـ ItemsController
// ==============================================================

/// <summary>
/// Variant DTO لصنف داخل ItemsController (مختلف عن ItemVariantDto في ItemHierarchyDtos).
/// </summary>
public class ItemVariantApiDto
{
    public long Id { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public decimal? PriceOverride { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// Unit Conversion DTO لصنف داخل ItemsController.
/// </summary>
public class ItemUnitConversionApiDto
{
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionFactor { get; set; } = 1;
    public string? Barcode { get; set; }
}

/// <summary>
/// DTO رئيسي للصنف — للقراءة والكتابة.
/// </summary>
public class ItemApiDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long CategoryId { get; set; }
    public long? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemType { get; set; } = "منتج تام";
    public string Unit { get; set; } = "كجم";
    public bool Active { get; set; } = true;
    public bool HasExpiryDate { get; set; } = true;
    public int? ShelfLifeDays { get; set; }

    public decimal ReorderLevel { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal MaxStock { get; set; }

    public decimal BasePrice { get; set; }
    public string? Barcode { get; set; }

    public bool HasVariants { get; set; }
    public bool IsVariant { get; set; }
    public bool IsMerged { get; set; }
    public bool IsContainer { get; set; }
    public bool MultiUom { get; set; }
    public bool HideDetailPrint { get; set; }
    public long? ParentItemId { get; set; }

    public List<ItemVariantApiDto> Variants { get; set; } = new();
    public List<ItemUnitConversionApiDto> UnitsHierarchy { get; set; } = new();
}

/// <summary>
/// DTO للاستيراد السريع للأصناف.
/// </summary>
public class BulkQuickImportRequest
{
    public string RawData { get; set; } = string.Empty;
    public long DefaultCategoryId { get; set; } = 1;
}