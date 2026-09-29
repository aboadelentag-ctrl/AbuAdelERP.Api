using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using AbuAdelERP.Api.Models;
using AbuAdelERP.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. إعدادات قاعدة البيانات الأساسية
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. تسجيل جميع الخدمات وربطها بحاوية الاعتمادية (Dependency Injection)
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IFinanceService, FinanceService>();
builder.Services.AddScoped<IPurchasingService, PurchasingService>();
builder.Services.AddScoped<IProductivityService, ProductivityService>();
builder.Services.AddScoped<IQualityService, QualityService>();
builder.Services.AddScoped<ICrateService, CrateService>();
builder.Services.AddScoped<ICostingService, CostingService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<IProductionOrderService, ProductionOrderService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IItemHierarchyService, ItemHierarchyService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IVendorQcService, VendorQcService>();
builder.Services.AddScoped<IStockCardService, StockCardService>();
builder.Services.AddScoped<IMaterialRequisitionService, MaterialRequisitionService>();
builder.Services.AddScoped<IFinishedGoodsTransferService, FinishedGoodsTransferService>();

// ====== إضافة خدمة التحويلات بين المخازن ولجان فض المنازعات ======
builder.Services.AddScoped<IStockTransferService, StockTransferService>();

// 3. محرك الحماية الفعلي (JWT)
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. واجهة العرض الافتراضية النقية (Swagger & CORS)
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");

// 5. إعدادات التشغيل
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();