using System;
using System.Collections.Generic;

namespace KayanERP.UI.DTOs
{
    // ==============================================================
    // 1. نماذج استعراض الفواتير في جدول العرض والمعاينة
    // ==============================================================
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

    // ==============================================================
    // 2. نماذج إنشاء واعتماد فاتورة جديدة
    // ==============================================================
    public class SalesInvoiceCreateDto
    {
        public long? BranchId { get; set; } = 1;
        public long CustomerId { get; set; }
        public long? WarehouseId { get; set; }
        public List<SalesInvoiceLineDto> InvoiceLines { get; set; } = new();
    }

    public class SalesInvoiceLineDto
    {
        public long ItemId { get; set; }
        public int Quantity { get; set; } = 1;
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

    // ==============================================================
    // 3. القوائم المرجعية لشاشة الفاتورة (فروع + عملاء + أصناف + مخازن)
    // ==============================================================
    public class SalesLookupsDto
    {
        public List<SalesBranchLookupDto> Branches { get; set; } = new();
        public List<CustomerLookupDto> Customers { get; set; } = new();
        public List<SalesFinishedItemLookupDto> FinishedItems { get; set; } = new();
        public List<SalesWarehouseLookupDto> Warehouses { get; set; } = new();
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
        public decimal SellingPrice { get; set; } // <--- تم التثبيت
    }

    public class SalesWarehouseLookupDto
    {
        public long Id { get; set; }
        public long? BranchId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string? WarehouseType { get; set; }
    }

    public class CustomerItemDto
    {
        public long Id { get; set; }
        public long CompanyId { get; set; } = 1;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = "RETAIL";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public decimal CreditLimit { get; set; }
        public string? PaymentTerms { get; set; }
        public string? TaxNumber { get; set; }
        public bool Active { get; set; } = true;
        public string? Notes { get; set; }
    }
}