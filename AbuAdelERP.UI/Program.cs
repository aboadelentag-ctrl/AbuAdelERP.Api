using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Blazored.LocalStorage;
using AbuAdelERP.UI;
using AbuAdelERP.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// المكونات الجذرية للتطبيق
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. ربط الواجهة بعنوان سيرفر الـ API المركزي (المنفذ 7273)
var apiBaseUri = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7273/";
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUri)
});

// 2. تفعيل مكتبة التخزين المحلي (LocalStorage)
builder.Services.AddBlazoredLocalStorage();

// 3. تفعيل خدمات الصلاحيات والأمان الأساسية لـ Blazor
builder.Services.AddAuthorizationCore();

// 4. تسجيل خدمة إدارة حالة المستخدم وصلاحيات الأزرار (AppState)
builder.Services.AddScoped<AppState>();
builder.Services.AddScoped<ISalesClientService, SalesClientService>();
builder.Services.AddScoped<AbuAdelERP.UI.Services.IBranchContextService, AbuAdelERP.UI.Services.BranchContextService>();

await builder.Build().RunAsync();