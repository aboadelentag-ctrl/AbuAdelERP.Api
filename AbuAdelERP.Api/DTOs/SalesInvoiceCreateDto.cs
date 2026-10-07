using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AbuAdelERP.Api.DTOs;

public class SalesInvoiceCreateDto
{
    public long? BranchId { get; set; } = 1;

    [Required(ErrorMessage = "يجب تحديد العميل أو الفرع.")]
    public long CustomerId { get; set; }

    public long? WarehouseId { get; set; }

    [Required(ErrorMessage = "يجب إدراج بنود الفاتورة.")]
    public List<SalesInvoiceLineDto> InvoiceLines { get; set; } = new();
}

public class SalesInvoiceLineDto
{
    [Required(ErrorMessage = "يجب تحديد المنتج المباع.")]
    public long ItemId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر.")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "السعر يجب أن تكون أكبر من صفر.")]
    public decimal UnitPrice { get; set; }
}

public class SalesInvoiceResultDto
{
    public bool Success { get; set; }
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class SalesInvoiceViewDto
{
    public long Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public long? BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemsCount { get; set; }
    public List<SalesInvoiceLineViewDto> Lines { get; set; } = new();
}

public class SalesInvoiceLineViewDto
{
    public long Id { get; set; }
    public long ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class SalesLookupsDto
{
    public List<CustomerLookupDto> Customers { get; set; } = new();
    public List<SalesFinishedItemLookupDto> FinishedItems { get; set; } = new();
    public List<SalesWarehouseLookupDto> Warehouses { get; set; } = new();
    public List<SalesBranchLookupDto> Branches { get; set; } = new();
}

public class SalesBranchLookupDto
{
    public long Id { get; set; }
    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
}

public class CustomerLookupDto
{
    public long Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class SalesFinishedItemLookupDto
{
    public long Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? ItemType { get; set; }
    public string? Unit { get; set; }
    public long? DepartmentId { get; set; }
    public int? ShelfLifeDays { get; set; }
}

public class SalesWarehouseLookupDto
{
    public long Id { get; set; }
    public long? BranchId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string? WarehouseType { get; set; }
}