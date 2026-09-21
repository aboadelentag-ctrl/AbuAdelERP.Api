using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AbuAdelERP.Api.Models;

[Table("items", Schema = "erp")]
public class Item
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("company_id")]
    public long CompanyId { get; set; }

    [Column("item_code")]
    public string ItemCode { get; set; } = string.Empty;

    [Column("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [Column("item_type")]
    public string ItemType { get; set; } = string.Empty;

    [Column("category_id")]
    public long CategoryId { get; set; }

    [ForeignKey("CategoryId")]
    [JsonIgnore]
    public virtual ItemCategory? ItemCategory { get; set; }

    [Column("department_id")]
    public long? DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    [JsonIgnore]
    public virtual Department? Department { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = string.Empty;

    [Column("parent_item_id")]
    public long? ParentItemId { get; set; }

    [ForeignKey("ParentItemId")]
    [JsonIgnore]
    public virtual Item? ParentItem { get; set; }

    [InverseProperty("ParentItem")]
    [JsonIgnore]
    public virtual ICollection<Item> Variants { get; set; } = new List<Item>();

    [Column("has_expiry_date")]
    public bool HasExpiryDate { get; set; } = true;

    [Column("shelf_life_days")]
    public int? ShelfLifeDays { get; set; }

    [Column("reorder_level")]
    public decimal ReorderLevel { get; set; } = 0;

    [Column("safety_stock")]
    public decimal SafetyStock { get; set; } = 0;

    [Column("max_stock")]
    public decimal MaxStock { get; set; } = 0;

    [Column("active")]
    public bool Active { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ==========================================
    // الخصائص الجديدة المتوافقة مع نظام كيان وقاعدة البيانات
    // ==========================================
    [Column("department_name")]
    public string? DepartmentName { get; set; }

    [Column("base_price")]
    public decimal BasePrice { get; set; } = 0;

    [Column("barcode")]
    public string? Barcode { get; set; }

    [Column("is_variant")]
    public bool IsVariant { get; set; } = false;

    [Column("is_merged")]
    public bool IsMerged { get; set; } = false;

    [Column("is_container")]
    public bool IsContainer { get; set; } = false;

    [Column("multi_uom")]
    public bool MultiUom { get; set; } = false;

    [Column("hide_detail_print")]
    public bool HideDetailPrint { get; set; } = false;
}