using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AbuAdelERP.UI;
using Blazored.LocalStorage;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ربط الواجهة برابط الـ API الخاص بمصنع أبو عادل
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7273/") });

// تفعيل مكتبة التخزين لحفظ توكن الدخول
builder.Services.AddBlazoredLocalStorage();

await builder.Build().RunAsync();