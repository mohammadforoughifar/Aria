using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Northwind.Api.Services.Common;
using Northwind.Api.ViewModels.Common;

namespace Northwind.Api.Middleware;

/// <summary>
/// همهٔ خطاهایِ API را به یک شکلِ واحد تبدیل می‌کند تا کلاینت همیشه
/// <see cref="ApiErrorViewModel"/> بگیرد، نه صفحهٔ HTML یا استک‌تریس.
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
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogInformation("خطایِ دامنه [{Code}] {Path}: {Message}", ex.Code, context.Request.Path, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteAsync(context, StatusCodes.Status401Unauthorized, "unauthorized", ex.Message);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // کلاینت درخواست را رها کرده — چیزی برایِ پاسخ دادن نیست.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطایِ پیش‌بینی‌نشده در {Path}", context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "server_error",
                "خطایِ غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید.");
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new ApiErrorViewModel
        {
            Code = code,
            Message = message,
            Status = status,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
