// وضعیتِ ارتباط با سرورِ API.
//
// چرا لازم است؟ اگر API در دسترس نباشد (مثلاً سرور اجرا نشده یا نشانی‌اش
// اشتباه است)، وب‌سرورِ میزبان به‌جای JSON صفحهٔ HTML برمی‌گرداند و تجزیهٔ آن
// خطا می‌دهد. پیش از این، آن خطا تا بالاترین سطح بالا می‌رفت و کلِ برنامه را
// از کار می‌انداخت. حالا خطا در لایهٔ داده مهار می‌شود و فقط یک پیامِ
// دوستانه بالای صفحه نشان داده می‌شود.

namespace Northwind.Client.Services;

public sealed class ApiHealth
{
    public event Action? OnChange;

    /// <summary>آیا آخرین ارتباط با سرور موفق بوده است؟</summary>
    public bool Reachable { get; private set; } = true;

    /// <summary>توضیحِ کوتاه برای نمایش در صورتِ بروز مشکل.</summary>
    public string? Detail { get; private set; }

    private bool _announced = true;

    public void ReportSuccess()
    {
        if (Reachable && _announced) return;
        Reachable = true;
        Detail = null;
        _announced = true;
        OnChange?.Invoke();
    }

    public void ReportFailure(string detail)
    {
        Detail = detail;
        // فقط در نخستین شکست اعلان می‌کنیم تا در هر خطا صفحه نلرزد
        if (!Reachable && _announced) return;
        Reachable = false;
        _announced = true;
        OnChange?.Invoke();
    }
}
