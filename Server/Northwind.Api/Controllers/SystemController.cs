using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Data;
using Northwind.Data.Persistence;

namespace Northwind.Api.Controllers;

/* ========================= وضعیتِ پایگاه‌داده =========================
   پرسشِ همیشگیِ مدیر: «محصولی که ساختم کجاست؟ رویِ چه پایگاه‌داده‌ای و در
   کدام فایل ذخیره شد؟» این نقطهٔ پایانی همان را شفاف می‌گوید: نوعِ پایگاه‌داده،
   مسیرِ فایل و شمارِ رکوردهایی که همین حالا از آن خوانده می‌شود. اگر شمارش‌ها
   پس از راه‌اندازیِ دوباره صفر شد، یعنی فایل عوض شده است.                   */

[ApiController]
[Route("api/v1/admin/system")]
[Tags("مدیریت")]
[Authorize(Roles = "Admin")]
public sealed class SystemController : ControllerBase
{
    private readonly NorthwindDbContext _db;
    private readonly DatabaseOptions _options;

    public SystemController(NorthwindDbContext db, DatabaseOptions options)
    {
        _db = db;
        _options = options;
    }

    /// <summary>نوع و مسیرِ پایگاه‌داده به‌همراهِ شمارِ رکوردهایِ اصلی.</summary>
    [HttpGet("database")]
    public async Task<object> Database(CancellationToken ct)
    {
        var isSqlServer = _db.Database.IsSqlServer();
        var path = isSqlServer ? null : DataRegistration.ResolveSqlitePath(_options);

        long sizeBytes = 0;
        if (path is not null && System.IO.File.Exists(path))
            sizeBytes = new FileInfo(path).Length;

        return new
        {
            provider = isSqlServer ? "SQL Server" : "SQLite",
            location = path ?? (_options.ConnectionString ?? ""),
            sizeBytes,
            reachable = await _db.Database.CanConnectAsync(ct),
            counts = new
            {
                products = await _db.Products.CountAsync(ct),
                categories = await _db.Categories.CountAsync(ct),
                users = await _db.Users.CountAsync(ct),
                orders = await _db.Orders.CountAsync(ct),
                licenses = await _db.Licenses.CountAsync(ct),
                articles = await _db.Articles.CountAsync(ct),
            },
        };
    }
}
