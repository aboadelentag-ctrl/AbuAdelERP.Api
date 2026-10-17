using System;

namespace KayanERP.Api.DTOs;

public class ItemCategoryDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateItemCategoryDto
{
    public string CategoryCode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; } = true;
}