using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Northwind.Api;
using Northwind.Api.Middleware;
using Northwind.Api.Services.Security;
using Northwind.Api.Services.Seed;
using Northwind.Infrastructure;
using Northwind.Infrastructure.Persistence;
using Northwind.Server;
using Northwind.Server.Data;

var builder = WebApplication.CreateBuilder(args);

/* ------------------------------- پایگاه‌داده ------------------------------- */
// فایلِ SQLite داخلِ workspace است تا همهٔ داده‌ها بعد از ریست هم باقی بمانند.
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "aria.db");
builder.Configuration["DatabasePath"] = dbPath;

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

/* --------------------- بک‌اندِ تازه (SQL Server + REST) ---------------------
   لایه‌هایِ Domain/Infrastructure/Api. اگر رشتهٔ اتصالِ SQL Server تنظیم شده باشد
   مهاجرت‌ها رویِ آن اعمال می‌شود؛ وگرنه برایِ دیدنِ سایت رویِ SQLite بالا می‌آید.
   مسیرِ فالبک عمداً جدا از پایگاه‌دادهٔ قدیمی است تا این دو قاطی نشوند.          */
var dbOptions = builder.Configuration.ReadDatabaseOptions();
if (!dbOptions.HasSqlServer && !Path.IsPathRooted(dbOptions.SqlitePath))
    dbOptions.SqlitePath = Path.Combine(builder.Environment.ContentRootPath, "data", "northwind-api.db");

builder.Services.AddNorthwindPersistence(builder.Configuration, dbOptions);
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
    p.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true)));

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

/* --------------------- ایجاد و seed در اولین اجرا --------------------- */
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed.Run(db);
    Seed.Patch(db);          // به‌روزرسانیِ افزاینده: برند و بسته‌ها
    DemoData.Run(db, m => app.Logger.LogWarning(m));   // دادهٔ تستی
    app.Logger.LogInformation("پایگاه‌داده آماده است: {Path}", dbPath);
}

/* ------------------ آماده‌سازیِ پایگاه‌دادهٔ تازه + دادهٔ نمایشی ------------------ */
await app.Services.InitializeNorthwindDatabaseAsync();
using (var scope = app.Services.CreateScope())
{
    var apiDb = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (dbOptions.SeedDemoData)
        await DemoSeeder.SeedAsync(apiDb, hasher, app.Logger);

    app.Logger.LogInformation("API تازه روی {Provider} آماده است",
        apiDb.Database.IsSqlServer() ? "SQL Server" : "SQLite (حالتِ نمایشی)");
}

app.UseSwagger();
app.UseSwaggerUI(o => { o.SwaggerEndpoint("/swagger/v1/swagger.json", "فروغ آریا پارسیان API"); o.RoutePrefix = "swagger"; });

// خطاهایِ API را یکدست می‌کند و باید پیش از بقیه باشد.
app.UseMiddleware<ApiExceptionMiddleware>();

app.UseCors("client");
app.UseResponseCompression();     // باید پیش از سروِ فایل‌های ایستا باشد
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
app.MapFallbackToFile("index.html");

app.Run();
