using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Northwind.Core.Models.Common;
using Northwind.Core.Services.Common;

namespace Northwind.Api.Middleware;

/// <summary>
/// همهٔ خطاهایِ API را به یک شکلِ واحد تبدیل می‌کند تا کلاینت همیشه
/// <see cref="ApiErrorViewModel"/> بگیرد، نه صفحهٔ HTML یا استک‌تریس.
///
/// سه دستهٔ خطا پوشش داده می‌شود:
///   ۱) استثناها (دامنه، احراز، پایگاه‌داده، بدنهٔ خرابِ JSON، و ناشناخته)؛
///   ۲) کدهایِ وضعیتی که بدونِ بدنه برمی‌گردند (۴۰۱ میان‌افزارِ احراز، ۴۰۳،
///      ۴۰۵، ۴۱۵ و ۴۰۴ مسیرهایِ ناشناخته)؛
///   ۳) پاسخِ ۵۰۰ که هرگز جزئیاتِ داخلی را لو نمی‌دهد.
///
/// هر پاسخ یک هدرِ <c>X-Trace-Id</c> هم دارد تا کاربر بتواند همان شناسه را به
/// پشتیبانی بگوید و ما در لاگ پیدایش کنیم.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var isApi = IsApiRequest(context);

        // شناسهٔ پیگیری پیش از هر چیز رویِ پاسخ می‌نشیند؛ حتی برایِ پاسخ‌هایِ
        // موفق هم بی‌ضرر است و اشکال‌زدایی را ساده می‌کند.
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        try
        {
            await _next(context);

            // هیچ استثنایی رخ نداده، اما پاسخ خطاست و بدنه‌ای هم ندارد
            // (مثلاً ۴۰۱ میان‌افزارِ احراز یا ۴۰۵ روشِ نادرست).
            if (isApi && !context.Response.HasStarted && context.Response.StatusCode >= 400)
                await WriteAsync(context, context.Response.StatusCode, null, null);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // کلاینت درخواست را رها کرده — چیزی برایِ پاسخ دادن نیست.
        }
        catch (AppException ex)
        {
            _logger.LogInformation("خطایِ دامنه [{Code}] {Path}: {Message}", ex.Code, context.Request.Path, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogInformation("دسترسیِ غیرمجاز {Path}: {Message}", context.Request.Path, ex.Message);
            await WriteAsync(context, StatusCodes.Status401Unauthorized, "unauthorized", ex.Message);
        }
        catch (BadHttpRequestException ex)
        {
            // بدنهٔ JSONِ ناقص، اندازهٔ بیش از حد، یا قالبِ نادرست — پیامِ خامِ
            // انگلیسیِ چارچوب هرگز به کاربر نمی‌رسد.
            _logger.LogInformation("درخواستِ نامعتبر {Path}: {Message}", context.Request.Path, ex.Message);
            await WriteAsync(context, StatusCodes.Status400BadRequest, "invalid_request",
                "بدنهٔ درخواست معتبر نیست. ساختارِ JSON و اندازهٔ فیلدها را بررسی کنید.");
        }
        catch (JsonException ex)
        {
            _logger.LogInformation("JSONِ نامعتبر {Path}: {Message}", context.Request.Path, ex.Message);
            await WriteAsync(context, StatusCodes.Status400BadRequest, "invalid_json",
                "بدنهٔ درخواست JSON معتبر نیست.");
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "تضادِ هم‌زمانی در {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status409Conflict, "concurrency_conflict",
                "این رکورد هم‌زمان جای دیگری تغییر کرده است. صفحه را تازه کنید و دوباره تلاش کنید.");
        }
        catch (DbUpdateException ex)
        {
            // بیشترِ این خطاها نقضِ قیدِ یکتایی است (مثلِ ایمیل یا اسلاگِ تکراری).
            _logger.LogWarning(ex, "خطایِ نوشتنِ پایگاه‌داده در {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status409Conflict, "conflict",
                "این داده با آنچه از پیش در پایگاه‌داده است تناقض دارد (مثلاً مقدارِ تکراری).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطایِ پیش‌بینی‌نشده در {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "server_error",
                "خطایِ غیرمنتظره‌ای در سرور رخ داد. لطفاً دوباره تلاش کنید.");
        }
    }

    /// <summary>آیا درخواست به API است؟ صفحه‌هایِ خودِ سایت و فایل‌هایِ ایستا خطایِ JSON نمی‌گیرند.</summary>
    private static bool IsApiRequest(HttpContext context)
    {
        var path = context.Request.Path;
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// پیامِ فارسی و کدِ ماشین‌خوانِ پیش‌فرض برایِ کدهایِ وضعیتی که خودِ سرور
    /// بدونِ توضیح برمی‌گرداند.
    /// </summary>
    private static (string Code, string Message) Describe(int status) => status switch
    {
        StatusCodes.Status400BadRequest => ("bad_request", "درخواست معتبر نیست."),
        StatusCodes.Status401Unauthorized => ("unauthorized", "برایِ این کار باید وارد حسابِ خود شوید."),
        StatusCodes.Status403Forbidden => ("forbidden", "به این بخش دسترسی ندارید."),
        StatusCodes.Status404NotFound => ("not_found", "نشانیِ درخواستی در سرور وجود ندارد."),
        StatusCodes.Status405MethodNotAllowed => ("method_not_allowed", "این درخواست با روشِ مجازِ این نشانی هم‌خوان نیست."),
        StatusCodes.Status408RequestTimeout => ("timeout", "پاسخِ سرور دیر شد؛ دوباره تلاش کنید."),
        StatusCodes.Status409Conflict => ("conflict", "این درخواست با وضعیتِ فعلیِ داده تناقض دارد."),
        StatusCodes.Status413PayloadTooLarge => ("payload_too_large", "حجمِ ارسالی بیش از حدِ مجاز است."),
        StatusCodes.Status415UnsupportedMediaType => ("unsupported_media_type", "قالبِ محتوایِ درخواست پشتیبانی نمی‌شود."),
        StatusCodes.Status422UnprocessableEntity => ("unprocessable", "دادهٔ ارسالی قابلِ پردازش نیست."),
        StatusCodes.Status429TooManyRequests => ("too_many_requests", "درخواست‌ها زیاد شده‌اند؛ کمی بعد دوباره تلاش کنید."),
        StatusCodes.Status500InternalServerError => ("server_error", "خطایِ غیرمنتظره‌ای در سرور رخ داد."),
        StatusCodes.Status503ServiceUnavailable => ("unavailable", "سرور موقتاً در دسترس نیست. کمی بعد دوباره تلاش کنید."),
        _ => ("error", "درخواست انجام نشد.")
    };

    private async Task WriteAsync(HttpContext context, int status, string? code, string? message)
    {
        if (context.Response.HasStarted) return;

        // اگر بدنه‌ای از پیش نوشته شده، دست نمی‌زنیم.
        if (context.Response.ContentLength is > 0) return;

        var (defaultCode, defaultMessage) = Describe(status);

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;

        var payload = new ApiErrorViewModel
        {
            Code = string.IsNullOrWhiteSpace(code) ? defaultCode : code!,
            Message = string.IsNullOrWhiteSpace(message) ? defaultMessage : message!,
            Status = status,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // متنِ فارسی باید خوانا بمانَد، نه \u06XX.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
