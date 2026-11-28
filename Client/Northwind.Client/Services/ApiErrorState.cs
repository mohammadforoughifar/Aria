// ثبتِ خطاهایِ سرور برایِ نمایش به کاربر.
//
// چرا؟ پیش‌تر خطاهایِ «خواندن» (GET) در لایهٔ داده مهار می‌شدند و فقط یک
// «هیچ داده‌ای نیست» نشان داده می‌شد؛ کاربر نمی‌دانست سرور خطا داده یا واقعاً
// چیزی نیست. حالا هر خطایِ سرور که پیام و کدِ ماشین‌خوان دارد (همان
// ApiErrorViewModel) این‌جا ثبت می‌شود و <c>ApiErrorBanner</c> در هر دو
// چیدمانِ سایت و پنل آن را با متنِ فارسیِ خودِ سرور نشان می‌دهد.
//
// قاعده‌ها:
//   · «۴۰۴ پیدا نشد» ثبت نمی‌شود؛ صفحهٔ مربوطه خودش حالتِ خالیِ روشن دارد
//     و تکرارِ بنر برایِ هر محصولِ حذف‌شده آزاردهنده است.
//   · خطاهایِ نوشتن (POST/PUT/DELETE) هم این‌جا می‌آیند و هم به‌صورتِ
//     V1ApiException به صفحه می‌رسند تا همان‌جا در قالبِ Toast دیده شوند.
//   · جدیدترین خطا جایِ قبلی را می‌گیرد؛ تاریخچه نگه نمی‌داریم تا بنر
//     انبارِ خطاهایِ کهنه نشود.

namespace Northwind.Client.Services;

/// <summary>یک خطایِ سرور با پیامِ آمادهٔ نمایش.</summary>
public sealed record ApiError(
    string Message,
    string? Code,
    int Status,
    string? TraceId,
    string? Path);

public sealed class ApiErrorState
{
    public event Action? OnChange;

    /// <summary>آخرین خطایِ نمایش‌دادنی؛ <c>null</c> یعنی چیزی برایِ گفتن نیست.</summary>
    public ApiError? Current { get; private set; }

    public void Report(string message, string? code, int status, string? traceId = null, string? path = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        Current = new ApiError(message, code, status, traceId, path);
        OnChange?.Invoke();
    }

    /// <summary>
    /// خطایِ پاسخِ HTTP را ثبت می‌کند؛ «۴۰۴» را رد می‌کند چون صفحهٔ مقصد
    /// خودش حالتِ «پیدا نشد» را نشان می‌دهد.
    /// </summary>
    public void ReportHttp(int status, string? code, string? message, string? traceId, string? path)
    {
        if (status == 404 && code is null or "not_found") return;
        Report(string.IsNullOrWhiteSpace(message) ? $"درخواست انجام نشد (کدِ {status})." : message!,
               code, status, traceId, path);
    }

    public void Clear()
    {
        if (Current is null) return;
        Current = null;
        OnChange?.Invoke();
    }
}
