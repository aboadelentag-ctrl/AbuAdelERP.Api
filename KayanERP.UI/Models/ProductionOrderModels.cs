using System;
using System.Collections.Generic;

namespace KayanERP.UI.Models
{
    public class ProductionOrderItemInputDto
    {
        public long ItemId { get; set; }
        public long RecipeId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; } = 1;
        public string Unit { get; set; } = "صاج";
    }

    public class CreateProductionOrderDto
    {
        public long CompanyId { get; set; } = 1;
        public long WarehouseId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? Notes { get; set; }
        public List<ProductionOrderItemInputDto> Items { get; set; } = new();
    }

    public class ProductionOrderMaterialViewDto
    {
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal ConsumedQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class ProductionOrderDetailViewDto
    {
        public long Id { get; set; }
        public long ItemId { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public decimal PlannedQuantity { get; set; }
        public decimal ProducedQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string BatchNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ProductionOrderMaterialViewDto> Materials { get; set; } = new();
    }

    public class ProductionOrderListDto
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public string? Notes { get; set; }
        public List<ProductionOrderDetailViewDto> Details { get; set; } = new();
    }
}