using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using Blazored.LocalStorage;
using AbuAdelERP.UI;
using AbuAdelERP.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// المكونات الجذرية للتطبيق
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// 1. ربط الواجهة بعنوان سيرفر الـ API الخاص بمصنع حلواني أبو عادل
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7273/")
});

// 2. تفعيل مكتبة التخزين المحلي لحفظ التوكن وبيانات المستخدم (LocalStorage)
builder.Services.AddBlazoredLocalStorage();

// 3. تفعيل خدمات الصلاحيات والأمان الأساسية لـ Blazor
builder.Services.AddAuthorizationCore();

// 4. تسجيل خدمة إدارة حالة المستخدم وصلاحيات الأزرار (AppState)
builder.Services.AddScoped<AppState>();

await builder.Build().RunAsync();