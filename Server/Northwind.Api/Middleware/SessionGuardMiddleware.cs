using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Northwind.Core.Models.Common;
using Northwind.Data.Persistence;

namespace Northwind.Api.Middleware;

/* ==================== نگهبانِ نشست: کاربرِ توکن باید وجود داشته باشد ====================
   سناریویِ واقعی: مرورگر توکنِ قبلی را نگه می‌دارد، اما پایگاه‌داده دوباره
   ساخته/بازنشانی شده و آن شناسهٔ کاربر دیگر وجود ندارد. پیش‌تر اولین درخواستِ
   صفحه (مثلِ /account/me) با «NotFoundException: کاربر با شناسهٔ … پیدا نشد»
   می‌ترکید؛ هم در لاگِ سرور یک استثنا ثبت می‌شد و هم کاربر پیامِ عجیب می‌دید.

   حالا همان وضعیت یک چیزِ روشن است: **نشست منقضی شده** → ۴۰۱ با کدِ
   session_expired. کلاینت آن را می‌فهمد، نشستِ کهنه را پاک می‌کند و کاربر را
   به صفحهٔ ورود می‌برد.                                                        */

public sealed class SessionGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionGuardMiddleware> _logger;

    /// <summary>هر چند وقت یک‌بار وجودِ کاربر دوباره از پایگاه‌داده پرسیده شود.</summary>
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public SessionGuardMiddleware(RequestDelegate next, ILogger<SessionGuardMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, NorthwindDbContext db, IMemoryCache cache)
    {
        if (context.User?.Identity?.IsAuthenticated == true &&
            context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            var raw = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                   ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(raw, out var userId))
            {
                var key = $"session-guard:{userId:n}";
                if (!cache.TryGetValue(key, out bool exists))
                {
                    exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userId, context.RequestAborted);
                    cache.Set(key, exists, CacheTtl);
                }

                if (!exists)
                {
                    _logger.LogInformation(
                        "نشستِ منقضی: کاربرِ {UserId} در پایگاه‌داده نیست — مسیر {Path}",
                        userId, context.Request.Path);

                    await WriteExpiredAsync(context);
                    return;
                }
            }
        }

        await _next(context);
    }

    private static async Task WriteExpiredAsync(HttpContext context)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        var payload = new ApiErrorViewModel
        {
            Code = "session_expired",
            Message = "نشستِ شما معتبر نیست (حساب پیدا نشد یا پایگاه‌داده بازنشانی شده). دوباره وارد شوید.",
            Status = StatusCodes.Status401Unauthorized,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
