using KayanERP.Api.Services;

namespace KayanERP.Api.Extensions;

/// <summary>
/// Extension methods لتسجيل كل خدمات النظام في DI Container.
/// الهدف: تخلي Program.cs نظيف وموجز.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// يسجّل كل خدمات الـ Business Logic (Services) دفعة واحدة.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // ===== قطاع التصنيع والتشغيل (MES & Production) =====
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        services.AddScoped<IProductionOperationsService, ProductionOperationsService>();
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IProductivityService, ProductivityService>();

        // ===== قطاع المستودعات والأرصدة (Inventory & Warehouses) =====
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IStockService, StockService>();
        services.AddScoped<IStockCardService, StockCardService>();
        services.AddScoped<IStockTransferService, StockTransferService>();
        services.AddScoped<IFinishedGoodsTransferService, FinishedGoodsTransferService>();
        services.AddScoped<ICrateService, CrateService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IItemHierarchyService, ItemHierarchyService>();

        // ===== قطاع المشتريات والموردين (Procurement & Quality) =====
        services.AddScoped<IPurchasingService, PurchasingService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
        services.AddScoped<IProcurementService, ProcurementService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IVendorQcService, VendorQcService>();
        services.AddScoped<IQualityService, QualityService>();
        services.AddScoped<IMaterialRequisitionService, MaterialRequisitionService>();
        services.AddScoped<ISupplierPriceService, SupplierPriceService>();
        services.AddScoped<IMaterialReceiptService, MaterialReceiptService>();

        // ===== قطاع المالية والتكاليف والمبيعات (Finance, Costing & Sales) =====
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IFinanceService, FinanceService>();
        services.AddScoped<ICostingService, CostingService>();
        services.AddScoped<ISalesService, SalesService>();

        return services;
    }
}