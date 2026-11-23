// کلاینتِ تایپ‌دارِ API نسخهٔ ۱ (/api/v1).
//
// قراردادِ کار با این کلاینت:
//   · خواندن (GetAsync): اگر ارتباط یا پاسخ به‌هم بخورد، «null» برمی‌گردد و
//     وضعیت در ApiHealth ثبت می‌شود. صفحه‌ها با null اسکلت/خطا نشان می‌دهند و
//     هرگز صفر یا خالیِ گمراه‌کننده نمی‌کشند.
//   · نوشتن (SendAsync): خطا به‌صورتِ V1ApiException پرتاب می‌شود، همراهِ پیامِ
//     فارسیِ خودِ سرور؛ تا صفحه بتواند دقیقاً همان چیزی را بگوید که سرور گفته
//     («این کدِ تخفیف منقضی شده است») و متنِ از خود درآورده نشان ندهد.
//   · همهٔ فراخوانی‌ها CancellationToken می‌گیرند تا با رفتنِ کاربر از صفحه،
//     درخواستِ بی‌مصرف رها شود.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Northwind.Client.Services.V1;

/// <summary>خطایِ بازگشتیِ سرور با پیامِ آمادهٔ نمایش.</summary>
public sealed class V1ApiException : Exception
{
    public V1ApiException(string message, string? code = null, int status = 0, Exception? inner = null)
        : base(message, inner)
    {
        Code = code;
        Status = status;
    }

    /// <summary>کدِ ماشینیِ خطا، مثلِ <c>discount_expired</c> یا <c>invalid_credentials</c>.</summary>
    public string? Code { get; }

    public int Status { get; }

    /// <summary>سرور در دسترس نبود — نه اینکه درخواست غلط بود.</summary>
    public bool IsOffline => Status == 0 || Status is 404 or 502 or 503;
}

public interface IV1Api
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<T> SendAsync<T>(string method, string path, object? body = null, CancellationToken ct = default);
    Task SendAsync(string method, string path, object? body = null, CancellationToken ct = default);

    /// <summary>کلیدِ سبد برایِ کاربرِ مهمان؛ با احرازِ هویت لازم نیست.</summary>
    string SessionKey { get; set; }

    /// <summary>توکنِ دسترسی (نشستِ نسخهٔ ۱). تا موجِ بعدی خالی است.</summary>
    string? AccessToken { get; set; }
}

public sealed class V1Api : IV1Api
{
    private readonly HttpClient _http;
    private readonly ApiHealth _health;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public V1Api(HttpClient http, ApiHealth health)
    {
        _http = http;
        _health = health;
    }

    public string SessionKey { get; set; } = "demo";
    public string? AccessToken { get; set; }

    /* ------------------------------ خواندن ------------------------------ */

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            using var res = await SendAsync(Build(HttpMethod.Get, path), ct);

            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                // «پیدا نشد» خطایِ ارتباطی نیست؛ فقط یعنی داده‌ای نیست.
                _health.ReportSuccess();
                return default;
            }

            if (!res.IsSuccessStatusCode)
            {
                _health.ReportFailure($"سرور پاسخِ {(int)res.StatusCode} داد.");
                return default;
            }

            var value = await res.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct);
            _health.ReportSuccess();
            return value;
        }
        catch (OperationCanceledException)
        {
            // کاربر صفحه را ترک کرده؛ خطا نیست و چیزی گزارش نمی‌شود.
            return default;
        }
        catch (Exception ex)
        {
            _health.ReportFailure("ارتباط با سرور برقرار نشد.");
            Console.WriteLine($"[v1] GET {path}: {ex.Message}");
            return default;
        }
    }

    /* ------------------------------ نوشتن ------------------------------- */

    public async Task<T> SendAsync<T>(string method, string path, object? body = null, CancellationToken ct = default)
    {
        using var res = await SendAsync(Build(new HttpMethod(method), path, body), ct);
        await EnsureOkAsync(res, path, ct);

        if (res.Content.Headers.ContentLength == 0) return default!;
        var value = await res.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct);
        _health.ReportSuccess();
        return value!;
    }

    public async Task SendAsync(string method, string path, object? body = null, CancellationToken ct = default)
    {
        using var res = await SendAsync(Build(new HttpMethod(method), path, body), ct);
        await EnsureOkAsync(res, path, ct);
        _health.ReportSuccess();
    }

    /// <summary>پاسخِ ناموفق را به استثنایی با پیامِ فارسیِ سرور تبدیل می‌کند.</summary>
    private async Task EnsureOkAsync(HttpResponseMessage res, string path, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;

        V1Error? error = null;
        try { error = await res.Content.ReadFromJsonAsync<V1Error>(ApiJson.Options, ct); }
        catch { /* بدنهٔ خطا همیشه JSON نیست (مثلاً صفحهٔ HTMLِ وب‌سرور) */ }

        var status = (int)res.StatusCode;

        // ۴۰۴/۴۰۵ رویِ مسیرِ api/v1 یعنی درخواست اصلاً به API نرسیده.
        if (error is null && status is 404 or 405)
        {
            _health.ReportFailure("سرورِ API در دسترس نیست.");
            throw new V1ApiException("سرورِ API در دسترس نیست.", "offline", status);
        }

        _health.ReportSuccess();
        var message = string.IsNullOrWhiteSpace(error?.Message)
            ? $"درخواست انجام نشد (کدِ {status})."
            : error!.Message!;
        throw new V1ApiException(message, error?.Code, status);
    }

    /* ------------------------------ زیرساخت ----------------------------- */

    private HttpRequestMessage Build(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, path.TrimStart('/'));
        if (body is not null) req.Content = ApiJson.Body(body);
        return req;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(AccessToken))
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + AccessToken);

        req.Headers.TryAddWithoutValidation("X-Session-Key", SessionKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Timeout);
        return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
    }
}

/// <summary>کمکی‌هایِ رشتهٔ پرس‌وجو برایِ مسیرهایِ نسخهٔ ۱.</summary>
public static class V1Query
{
    public static string Of(params (string Key, object? Value)[] parts)
    {
        var items = parts
            .Where(p => p.Value is not null && !(p.Value is string s && s.Length == 0))
            .Select(p => $"{p.Key}={Uri.EscapeDataString(Convert.ToString(p.Value) ?? "")}");
        var joined = string.Join("&", items);
        return joined.Length == 0 ? "" : "?" + joined;
    }
}
