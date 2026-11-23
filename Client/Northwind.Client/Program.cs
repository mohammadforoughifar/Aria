using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Northwind.Client;
using Northwind.Client.Services;
using Northwind.Client.Services.V1;
using Northwind.Client.Models;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

/* ---------- نشانیِ API ----------
   برنامه دو جور اجرا می‌شود و در هر دو باید بی‌نیاز از تغییرِ دستی کار کند:
     · تک‌مبدأ (رابط و API روی یک نشانی) → درخواستِ نسبی درست است.
     · کلاینتِ جدا + سرورِ جدا        → باید نشانیِ سرور معلوم باشد.
   ApiEndpoint پیش از نخستین درخواست نشانیِ درست را می‌آزماید و برمی‌گزیند؛
   ApiBaseHandler هم آن را روی همهٔ درخواست‌هایِ api/... اعمال می‌کند.
   نشانیِ سرور را (اگر پورتش فرق دارد) در wwwroot/appsettings.json بنویسید.      */
builder.Services.AddSingleton(_ =>
    new ApiEndpoint(builder.Configuration["ApiBaseUrl"], builder.HostEnvironment.BaseAddress));
builder.Services.AddTransient<ApiBaseHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ApiBaseHandler>();
    handler.InnerHandler = new HttpClientHandler();
    return new HttpClient(handler) { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
});

/* ---------- لایهٔ داده ----------
   پیاده‌سازیِ واقعی روی HttpClient؛ داده‌ها از پایگاه‌دادهٔ سرور می‌آیند و
   بعد از رفرش هم باقی می‌مانند.
   برای بازگشت به دادهٔ ساختگیِ درون‌حافظه‌ای (بدون نیاز به سرور):
       builder.Services.AddSingleton(_ => MockData.Build());
       builder.Services.AddScoped<IStoreApi, MockStoreApi>();                    */
builder.Services.AddSingleton<ApiHealth>();
// داده‌هایِ کم‌تغییر (تنظیمات، دسته‌بندی، نظرات، پرسش‌ها) یک‌بار گرفته می‌شوند
builder.Services.AddSingleton<ApiCache>();
// کلاینتِ نسخهٔ ۱ + مسیریاب:
//   HttpStoreApi قدیمی به‌عنوانِ «پشتوانه» تزریق می‌شود و StoreApiRouter هر
//   فراخوانی را یا به /api/v1 می‌برد یا (اگر آن بخش هنوز مهاجرت نکرده) به
//   همان مسیرِ قدیمی. صفحه‌ها هیچ تغییری نمی‌کنند.
builder.Services.AddScoped<V1Api>();
builder.Services.AddScoped<IV1Api>(sp => sp.GetRequiredService<V1Api>());
builder.Services.AddScoped<HttpStoreApi>();
builder.Services.AddScoped<IStoreApi>(sp => new StoreApiRouter(
    sp.GetRequiredService<HttpStoreApi>(),
    sp.GetRequiredService<IV1Api>()));

/* ---------- وضعیتِ سمت کلاینت ---------- */
builder.Services.AddScoped<JsInterop>();
builder.Services.AddScoped<AuthState>();
builder.Services.AddScoped<CartState>();
builder.Services.AddScoped<CompareState>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddSingleton<EcosystemData>();

var host = builder.Build();

/* ---------- پیش‌بارگیریِ پیکربندیِ اکوسیستم ----------
   فایلِ JSON پیش از نخستین رندر خوانده می‌شود تا بخشِ اکوسیستم همان بارِ
   اول کامل رسم شود و با رسیدنِ داده زیرِ پایش جابه‌جا نشود.            */
try
{
    var eco = host.Services.GetRequiredService<EcosystemData>();
    using var http = new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    };
    var json = await http.GetStringAsync("data/ecosystem.json");
    eco.Set(JsonSerializer.Deserialize<EcosystemConfig>(json, EcosystemData.Options));
}
catch
{
    /* اگر فایل در دسترس نبود، مؤلفه با دادهٔ تهی کار می‌کند و چیزی نمی‌شکند */
}

await host.RunAsync();
