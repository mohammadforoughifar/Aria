namespace Northwind.Api.ViewModels.Common;

/// <summary>
/// بدنهٔ خطایِ REST — نزدیک به RFC 7807 (ProblemDetails) اما با پیامِ فارسیِ
/// قابلِ نمایش به کاربر.
/// </summary>
public sealed record ApiErrorViewModel
{
    /// <summary>کدِ ماشین‌خوان، مثلِ <c>product_not_found</c>.</summary>
    public string Code { get; init; } = "error";

    /// <summary>پیامِ فارسی برایِ نمایش.</summary>
    public string Message { get; init; } = "";

    /// <summary>کدِ وضعیتِ HTTP.</summary>
    public int Status { get; init; } = 400;

    /// <summary>خطاهایِ اعتبارسنجی به تفکیکِ فیلد.</summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>شناسهٔ ردیابی برایِ پیگیری در لاگ.</summary>
    public string? TraceId { get; init; }
}
