using System.Text;
using System.Text.Json.Serialization;
using KayanERP.Api.Extensions;
using KayanERP.Api.Models;
using KayanERP.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// ==============================================================
// 1. قاعدة البيانات (PostgreSQL - Neon)
// ==============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. " +
        "Set it via User Secrets (Development) or Environment Variables (Production).");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention();
});

// ==============================================================
// 2. الـ SaaS و Tenant Context
// ==============================================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ==============================================================
// 3. تسجيل خدمات النظام (Services)
// ==============================================================
builder.Services.AddApplicationServices();

// ==============================================================
// 4. الـ JWT Authentication
// ==============================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key is missing. Set 'Jwt:Key' via User Secrets or Environment Variables.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "KayanERP";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "KayanERP.Clients";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

// تسجيل خدمة إصدار التوكنات
builder.Services.AddScoped<IJwtService, JwtService>();

// ==============================================================
// 5. Controllers + Authorize افتراضي
// ==============================================================
builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});

// ==============================================================
// 6. Swagger + JWT Support
// ==============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);

    // تعريف Bearer Auth
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "أدخل التوكن هنا بالشكل: Bearer {token}"
    });

    // تفعيل Bearer Auth لكل الـ endpoints
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// ==============================================================
// 7. CORS — Origins محددة من الإعدادات
// ==============================================================
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException(
        "No CORS origins configured. Add 'Cors:AllowedOrigins' to settings.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("KayanPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ==============================================================
// 8. بناء التطبيق
// ==============================================================
var app = builder.Build();

// ==============================================================
// 9. Pipeline
// ==============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("KayanPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();