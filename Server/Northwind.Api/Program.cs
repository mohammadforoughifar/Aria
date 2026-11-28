using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Northwind.Api;
using Northwind.Api.Middleware;
using Northwind.Core.Services.Security;
using Northwind.Core.Services.Seed;
using Northwind.Data;
using Northwind.Data.Persistence;

/* ---------- سروِ فایل‌هایِ کلاینت در اجرایِ تک‌پروژه‌ای (ویندوز/ویژوال استودیو) ----------
   وقتی API را تنها اجرا می‌کنید، پوشهٔ wwwroot کنارِ خودش خالی است و آدرسِ API
   صفحهٔ سفید نشان می‌داد. اگر متغیرِ ASPNETCORE_WEBROOT ست نشده باشد، خروجیِ
   بیلدِ کلاینت (bin/<Config>/net9.0/wwwroot) ریشهٔ فایل‌هایِ استاتیک می‌شود؛
   نتیجه: http://localhost:5182 خودش سایت را نشان می‌دهد و به اجرایِ هم‌زمانِ
   دو پروژه نیازی نیست. (ریشهٔ وب باید پیش از ساختِ builder تعیین شود، وگرنه
   ASP.NET Core خطایِ «web root changed» می‌دهد.)                                */
(string? WebRoot, string? FrameworkRoot) ClientStaticRoots()
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_WEBROOT")))
        return (null, null);   // اسکریپت‌هایِ انتشار خودشان ریشه را می‌دهند

    var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var config = string.IsNullOrEmpty(env) || env.Equals("Development", StringComparison.OrdinalIgnoreCase)
        ? "Debug" : "Release";

    var client = Path.GetFullPath(Path.Combine(
        Directory.GetCurrentDirectory(), "..", "..", "Client", "Northwind.Client"));

    // فایل‌هایِ خودِ سایت (index.html، css، فونت‌ها) در سورسِ کلاینت‌اند.
    var web = Path.Combine(client, "wwwroot");

    // فریم‌ورکِ بیلدشده (_framework) در خروجیِ بیلد است، نه در سورس.
    var framework = Path.Combine(client, "bin", config, "net9.0", "wwwroot", "_framework");

    return (Directory.Exists(web) ? web : null,
            Directory.Exists(framework) ? framework : null);
}

var (clientWebRoot, clientFrameworkRoot) = ClientStaticRoots();
var builder = clientWebRoot is null
    ? WebApplication.CreateBuilder(args)
    : WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, WebRootPath = clientWebRoot });

/* ------------------------------- پایگاه‌داده -------------------------------
   ساختارِ چهارلایه: تنها پایگاه‌دادهٔ برنامه در لایهٔ Northwind.Data تعریف شده
   است. اگر رشتهٔ اتصالِ SQL Server تنظیم شده باشد مهاجرت‌ها رویِ آن اعمال
   می‌شود؛ وگرنه برایِ دیدنِ سایت رویِ SQLite (حالتِ نمایشی) بالا می‌آید.        */
var dbOptions = builder.Configuration.ReadDatabaseOptions();

// مسیرِ پیش‌فرض باید جایِ ثابتی باشد، نه داخلِ پوشهٔ ساخت. اگر برنامه از
// bin/Debug یا پوشهٔ publish اجرا شود، ContentRoot همان‌جاست و پاک‌کردنِ ساخت
// فایلِ پایگاه‌داده را با خودش می‌بَرد؛ StableRoot از آن پوشه‌ها بیرون می‌زند.
if (!dbOptions.HasSqlServer && !Path.IsPathRooted(dbOptions.SqlitePath))
    dbOptions.SqlitePath = Path.Combine(
        Northwind.Data.DataRegistration.StableRoot(), "data", "northwind-api.db");

// راهِ صریح برایِ تعیینِ محلِ فایلِ داده (مثلاً برایِ نگه‌داشتنِ آن بیرون از مخزن)
var dbPathOverride = Environment.GetEnvironmentVariable("ARIA_DB_PATH");
if (!string.IsNullOrWhiteSpace(dbPathOverride))
    dbOptions.SqlitePath = Path.GetFullPath(dbPathOverride);

builder.Services.AddNorthwindData(builder.Configuration, dbOptions);
builder.Services.AddNorthwindApi(builder.Configuration);

/* --------------------------------- API ------------------------------------ */
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

/* --------------------------- فشرده‌سازیِ پاسخ ---------------------------
   کتابخانهٔ دات‌نت برای اجرا در مرورگر چند مگابایت است و بدونِ فشرده‌سازی،
   بارِ اول بسیار سنگین می‌شود (اندازه‌گیری: ۷٫۶ مگابایت برای نخستین بار).
   با Brotli این حجم حدود یک‌چهارم می‌شود.                                  */
builder.Services.AddResponseCompression(o =>
{
    o.EnableForHttps = true;
    o.Providers.Add<BrotliCompressionProvider>();
    o.Providers.Add<GzipCompressionProvider>();
    // نوع‌هایِ پیش‌فرض شاملِ wasm و فونت نیست — باید دستی اضافه شوند
    o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/wasm", "application/octet-stream",
        "font/woff2", "image/svg+xml", "application/json",
    });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Optimal);

builder.Services.AddCors(o => o.AddPolicy("client", p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)
     // کلاینت برایِ نمایشِ «کدِ پیگیری» به این هدر نیاز دارد.
     .WithExposedHeaders("X-Trace-Id")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "نورث‌ویند — API", Version = "v1" });
});

/* ---------- سروِ فایل‌های کلاینت (تک‌مبدأ: رابط و API روی یک آدرس) ----------
   مسیرِ ریشهٔ فایل‌ها با متغیرِ استانداردِ ASPNETCORE_WEBROOT مشخص می‌شود
   (اسکریپتِ start-preview.sh آن را به خروجیِ انتشارِ کلاینت اشاره می‌دهد).
   نتیجه: رابط و API روی یک مبدأ هستند و نه به CORS نیاز است و نه به پروکسی.   */

var app = builder.Build();
app.Logger.LogInformation("ریشهٔ فایل‌های کلاینت: {Path}", builder.Environment.WebRootPath);

/* اگر کاربر فقط API را اجرا کرده و کلاینت هنوز بیلد نشده باشد، به‌جایِ صفحهٔ
   سفید، صریح در لاگ می‌گوییم چه کاری لازم است.                                  */
if (clientFrameworkRoot is null && clientWebRoot is not null)
    app.Logger.LogWarning(
        "خروجیِ بیلدِ کلاینت پیدا نشد — یک بار «dotnet build Client/Northwind.Client» " +
        "یا «start-windows.cmd» را اجرا کنید، وگرنه مرورگر فایل‌هایِ _framework را ۴۰۴ می‌گیرد.");

/* ------------------ آماده‌سازیِ پایگاه‌داده + دادهٔ نمایشی ------------------ */
await app.Services.InitializeNorthwindDatabaseAsync();
using (var scope = app.Services.CreateScope())
{
    var apiDb = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (dbOptions.SeedDemoData)
        await DemoSeeder.SeedAsync(apiDb, hasher, app.Logger);

    app.Logger.LogInformation("API روی {Provider} آماده است",
        apiDb.Database.IsSqlServer() ? "SQL Server" : "SQLite (حالتِ نمایشی)");
}

app.UseSwagger();
app.UseSwaggerUI(o => { o.SwaggerEndpoint("/swagger/v1/swagger.json", "فروغ آریا پارسیان API"); o.RoutePrefix = "swagger"; });

// خطاهایِ API را یکدست می‌کند و باید پیش از بقیه باشد.
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseCors("client");
app.UseResponseCompression();     // باید پیش از سروِ فایل‌های ایستا باشد

// فریم‌ورکِ بیلدشدهٔ کلاینت (فقط در اجرایِ تک‌پروژه‌ای؛ در حالتِ انتشار ریشهٔ
// وب همین را دارد و این میان‌افزار لازم نیست).
if (clientFrameworkRoot is not null)
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(clientFrameworkRoot),
        RequestPath = "/_framework",
        // پسوندهایی مثلِ dll/pdb/dat در نقشهٔ نوعِ محتوایِ پیش‌فرض نیستند و
        // بدونِ این دو خط ۴۰۴ می‌گیرند — همان چیزی که باعث می‌شد موتورِ
        // بلیزر بالا نیاید («Failed to fetch … .pdb»).
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
    });
}
app.UseDefaultFiles();

/* ------------------------- حافظهٔ مرورگر -------------------------
   فایل‌هایِ _framework اثرانگشت دارند (با هر تغییر، نامشان عوض می‌شود)،
   پس می‌توان آن‌ها را برای همیشه کش کرد: بارِ دوم تقریباً بی‌درنگ است.
   در مقابل، index.html و فایل‌هایِ داده باید هر بار بررسی شوند که اگر
   نسخهٔ تازه‌ای منتشر شد، کاربر همان را ببیند.                        */
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        var path = ctx.Context.Request.Path.Value ?? "";
        var h = ctx.Context.Response.Headers;
        if (path.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase))
            h.CacheControl = "public, max-age=31536000, immutable";
        else if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
              || path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            h.CacheControl = "no-cache";
    }
});
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

/* نقطهٔ پایانیِ تشخیصی — فقط در محیطِ توسعه. برایِ آزمودنِ مسیرِ خطایِ
   پیش‌بینی‌نشده (۵۰۰) و اطمینان از این‌که هیچ استک‌تریسی به بیرون نمی‌رود. */
if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/v1/_diag/boom", () => { throw new InvalidOperationException("خطایِ عمدیِ تشخیصی"); });
}

/* نشانیِ ناشناخته زیرِ /api باید ۴۰۴ با بدنهٔ JSON بدهد، نه صفحهٔ HTMLِ سایت.
   پیش‌تر «/api/v1/nope» صفحهٔ اصلی را با کدِ ۲۰۰ برمی‌گرداند و کلاینت فکر
   می‌کرد پاسخِ درستی گرفته است. */
app.MapFallback("/api/{**slug}", (HttpContext ctx) =>
    Results.Json(new Northwind.Core.Models.Common.ApiErrorViewModel
    {
        Code = "not_found",
        Message = "این نشانی در API وجود ندارد یا شناسهٔ آن معتبر نیست.",
        Status = StatusCodes.Status404NotFound,
        TraceId = ctx.TraceIdentifier
    }, statusCode: StatusCodes.Status404NotFound));

app.MapFallbackToFile("index.html");

app.Run();
