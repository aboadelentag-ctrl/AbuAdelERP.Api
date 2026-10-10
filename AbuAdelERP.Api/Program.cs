using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================
// 1. إعداد قاعدة البيانات (PostgreSQL - Neon) وضبط معيار Snake-Case
// ==============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention(); // يضمن تطابق أسماء الجداول والأعمدة 100% مع PostgreSQL
});

// ==============================================================
// 2. تفعيل محرك الـ SaaS وإدارة سياق المستأجر (Tenant Context)
// ==============================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ==============================================================
// 3. تسجيل خدمات النظام بحاوية الاعتمادية (Dependency Injection)
// ==============================================================

// قطاع التصنيع والتشغيل (MES & Production)
builder.Services.AddScoped<IProductionOrderService, ProductionOrderService>();
builder.Services.AddScoped<IProductionOperationsService, ProductionOperationsService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IProductivityService, ProductivityService>();

// قطاع المستودعات والأرصدة والتحويلات (Inventory & Warehouses)
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IStockCardService, StockCardService>();
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
builder.Services.AddScoped<IFinishedGoodsTransferService, FinishedGoodsTransferService>();
builder.Services.AddScoped<ICrateService, CrateService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IItemHierarchyService, ItemHierarchyService>();

// قطاع المشتريات والموردين وفحص الخامات (Procurement & Quality)
builder.Services.AddScoped<IPurchasingService, PurchasingService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IVendorQcService, VendorQcService>();
builder.Services.AddScoped<IQualityService, QualityService>();
builder.Services.AddScoped<IMaterialRequisitionService, MaterialRequisitionService>();
builder.Services.AddScoped<ISupplierPriceService, SupplierPriceService>();
builder.Services.AddScoped<IMaterialReceiptService, MaterialReceiptService>();

// قطاع المالية والتكاليف والمبيعات (Finance, Costing & Sales)
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<ICostingService, CostingService>();
builder.Services.AddScoped<ISalesService, SalesService>();

// ==============================================================
// 4. منظومة حماية الـ JWT
// ==============================================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "AbuAdelSuperSecretKeyForJwtAuthentication2026";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// ==============================================================
// 5. ضبط الـ Controllers ومنع مشاكل التسلسل (JSON Cycles)
// ==============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
});

// ==============================================================
// 6. ضبط سياسة الـ CORS لمنع تعارض المتصفح مع Blazor
// ==============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ==============================================================
// 7. خط أنابيب المعالجة (HTTP Request Pipeline)
// ==============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();