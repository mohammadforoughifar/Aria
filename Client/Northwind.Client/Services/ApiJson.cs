// زیرساختِ مشترکِ فراخوانیِ API: تنظیماتِ JSON و ساختِ رشتهٔ پرس‌وجو.
// این کلاس هیچ وابستگی‌ای به AuthState یا IStoreApi ندارد، بنابراین می‌تواند
// توسطِ سرویس‌های مستقل (مثل AuthApi) هم استفاده شود بدون اینکه حلقهٔ
// وابستگی در تزریقِ وابستگی (DI) ایجاد شود.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Northwind.Client.Services;

public static class ApiJson
{
    /// <summary>تنظیماتِ یکسان با سرور: نام‌گذاریِ camelCase و enum به‌صورت رشته.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static StringContent Body(object? value)
        => new(JsonSerializer.Serialize(value, Options), Encoding.UTF8, "application/json");

    /// <summary>ساختِ رشتهٔ پرس‌وجو؛ بخش‌های تهی حذف می‌شوند.</summary>
    public static string Q(params (string, object?)[] parts)
    {
        var list = parts.Where(p => p.Item2 is not null)
            .Select(p => $"{p.Item1}={Uri.EscapeDataString(p.Item2!.ToString()!)}");
        var joined = string.Join("&", list);
        return joined.Length == 0 ? "" : "?" + joined;
    }
}
