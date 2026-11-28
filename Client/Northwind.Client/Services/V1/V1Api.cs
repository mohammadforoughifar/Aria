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

    /// <summary>
    /// بارگذاریِ فایل (multipart/form-data). فایل به همان API می‌رود و نشانیِ
    /// ذخیره‌شده برمی‌گردد؛ پیامِ خطایِ سرور هم مثلِ بقیهٔ نوشتن‌ها پرتاب می‌شود.
    /// </summary>
    Task<T> UploadAsync<T>(string path, Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task SendAsync(string method, string path, object? body = null, CancellationToken ct = default);

    /// <summary>کلیدِ سبد برایِ کاربرِ مهمان؛ با احرازِ هویت لازم نیست.</summary>
    string SessionKey { get; set; }

    /// <summary>توکنِ دسترسی (نشستِ نسخهٔ ۱).</summary>
    string? AccessToken { get; set; }

    /// <summary>
    /// کدِ وضعیتِ آخرین پاسخِ HTTP (۰ = ارتباط برقرار نشد). صفحه‌ها و روترِ API
    /// با این مقدار «۴۰۱» را از «سرور خواب است» تفکیک می‌کنند.
    /// </summary>
    int LastStatus { get; }

    /// <summary>
    /// نشست دیگر معتبر نیست (سرور ۴۰۱ داد یا گفت کاربر پیدا نشد). مشترکان
    /// باید کاربر را بیرون کنند و به صفحهٔ ورود ببرند.
    /// </summary>
    event Action? SessionExpired;

    /// <summary>
    /// اعلامِ اینکه بازیابیِ نشست تمام شد. تا این لحظه هیچ درخواستی بیرون نمی‌رود،
    /// چون ممکن است توکن هنوز از localStorage خوانده نشده باشد.
    /// </summary>
    void MarkSessionResolved();
}

public sealed class V1Api : IV1Api
{
    private readonly HttpClient _http;
    private readonly ApiHealth _health;
    private readonly ApiErrorState _errors;

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public V1Api(HttpClient http, ApiHealth health, ApiErrorState errors)
    {
        _http = http;
        _health = health;
        _errors = errors;
    }

    public string SessionKey { get; set; } = "demo";
    public string? AccessToken { get; set; }

    public event Action? SessionExpired;

    public int LastStatus { get; private set; }

    // ── دروازهٔ نشست ────────────────────────────────────────────────────────
    // مشکلِ واقعی: با بارگذاریِ کاملِ صفحه، مؤلفه‌ها زودتر از خواندنِ توکن از
    // localStorage درخواست می‌فرستادند → ۴۰۱ → و روتر به API قدیمی می‌افتاد که
    // دادهٔ کاربرانِ دیگر را برمی‌گرداند. این دروازه همهٔ درخواست‌هایِ نسخهٔ ۱ را
    // تا پایانِ بازیابیِ نشست نگه می‌دارد؛ یک‌جا، برایِ همهٔ صفحه‌ها.
    private readonly TaskCompletionSource _sessionReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile bool _sessionResolved;

    public void MarkSessionResolved()
    {
        _sessionResolved = true;
        _sessionReady.TrySetResult();
    }

    private async Task WaitForSessionAsync(CancellationToken ct)
    {
        if (_sessionResolved) return;

        using var gate = CancellationTokenSource.CreateLinkedTokenSource(ct);
        gate.CancelAfter(TimeSpan.FromSeconds(8)); // اگر بازیابی گیر کرد، صفحه قفل نشود
        try { await _sessionReady.Task.WaitAsync(gate.Token); }
        catch (OperationCanceledException) { /* با انقضا رد می‌شویم؛ سرور خودش ۴۰۱ می‌دهد */ }
    }

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
                // پیامِ سرور را همان‌جور که هست به کاربر می‌رسانیم (بنرِ خطا).
                var error = await ReadErrorAsync(res, ct);
                ReportHttp(res, error, path);

                if ((int)res.StatusCode is >= 500 or 0 or 502 or 503)
                    _health.ReportFailure(error?.Message ?? $"سرور پاسخِ {(int)res.StatusCode} داد.");
                else
                    _health.ReportSuccess();

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
            _errors.Report("ارتباط با سرور برقرار نشد. اینترنت و اجرایِ سرور را بررسی کنید.",
                "offline", 0, null, path);
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

    /* ------------------------------ بارگذاری ----------------------------- */

    public async Task<T> UploadAsync<T>(string path, Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // بدنهٔ multipart نباید مثلِ بقیهٔ فراخوانی‌ها JSON بسته‌بندی شود، پس
        // درخواست را دستی می‌سازیم و همان مسیرِ احرازِ هویت را به کار می‌بریم.
        using var form = new MultipartFormDataContent();
        var part = new StreamContent(content);
        part.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);
        form.Add(part, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/')) { Content = form };
        using var res = await SendAsync(req, ct);
        await EnsureOkAsync(res, path, ct);

        if (res.Content.Headers.ContentLength == 0) return default!;
        var value = await res.Content.ReadFromJsonAsync<T>(ApiJson.Options, ct);
        _health.ReportSuccess();
        return value!;
    }

    /// <summary>پاسخِ ناموفق را به استثنایی با پیامِ فارسیِ سرور تبدیل می‌کند.</summary>
    private async Task EnsureOkAsync(HttpResponseMessage res, string path, CancellationToken ct)
    {
        if (res.IsSuccessStatusCode) return;

        var error = await ReadErrorAsync(res, ct);
        var status = (int)res.StatusCode;

        // همان خطا در بنرِ سراسری هم ثبت می‌شود؛ صفحه‌ای که آن را در Toast
        // نشان می‌دهد، باز هم پیامِ یکسانِ سرور را می‌گوید (نه متنِ ساختگی).
        ReportHttp(res, error, path);

        // سرور برایِ خطاهایِ اعتبارسنجی پیامِ هر فیلد را جدا می‌فرستد؛ اگر فقط
        // «برخی از فیلدها معتبر نیستند» نشان دهیم، کاربر نمی‌فهمد کدام فیلد.
        string? details = error?.Errors is { Count: > 0 } fields
            ? string.Join("؛ ", fields.SelectMany(f => f.Value).Where(v => !string.IsNullOrWhiteSpace(v)))
            : null;

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

        if (details is not null) message = $"{message} — {details}";
        throw new V1ApiException(message, error?.Code, status);
    }

    /// <summary>
    /// خطایِ پاسخ را برایِ نمایش ثبت می‌کند — با یک استثنا: اگر هیچ توکنی
    /// نفرستاده‌ایم، «۴۰۱» یعنی «هنوز وارد نشده‌اید»، نه خطا؛ دروازهٔ ورود
    /// خودش کاربر را به صفحهٔ ورود می‌برد و بنر زدن رویِ همان صفحه شلوغی است.
    /// </summary>
    private void ReportHttp(HttpResponseMessage res, V1Error? error, string path)
    {
        var status = (int)res.StatusCode;

        // توکن داشتیم و سرور گفت «شناسنامه‌ات معتبر نیست» → نشست مرده است.
        // نه بنرِ خطا، نه تلاشِ دوباره: فقط خروج و بازگشت به صفحهٔ ورود.
        if (status == 401 && !string.IsNullOrWhiteSpace(AccessToken))
        {
            AccessToken = null;
            SessionExpired?.Invoke();
            return;
        }

        if (status == 401) return;

        _errors.ReportHttp(status, error?.Code, error?.Message,
            error?.TraceId ?? TraceIdHeader(res), path);
    }

    /// <summary>بدنهٔ خطای سرور را می‌خواند؛ اگر JSON نبود، <c>null</c>.</summary>
    private static async Task<V1Error?> ReadErrorAsync(HttpResponseMessage res, CancellationToken ct)
    {
        try
        {
            if (res.Content.Headers.ContentLength == 0) return null;
            return await res.Content.ReadFromJsonAsync<V1Error>(ApiJson.Options, ct);
        }
        catch
        {
            // بدنهٔ خطا همیشه JSON نیست (مثلاً صفحهٔ HTMLِ وب‌سرور)
            return null;
        }
    }

    /// <summary>شناسهٔ پیگیری از هدرِ پاسخ (سرور همیشه آن را می‌فرستد).</summary>
    private static string? TraceIdHeader(HttpResponseMessage res)
        => res.Headers.TryGetValues("X-Trace-Id", out var values) ? values.FirstOrDefault() : null;

    /* ------------------------------ زیرساخت ----------------------------- */

    private HttpRequestMessage Build(HttpMethod method, string path, object? body = null)
    {
        var req = new HttpRequestMessage(method, path.TrimStart('/'));
        if (body is not null) req.Content = ApiJson.Body(body);
        return req;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, CancellationToken ct)
    {
        await WaitForSessionAsync(ct);

        if (!string.IsNullOrWhiteSpace(AccessToken))
            req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + AccessToken);

        req.Headers.TryAddWithoutValidation("X-Session-Key", SessionKey);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(Timeout);
        try
        {
            var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            LastStatus = (int)res.StatusCode;
            return res;
        }
        catch
        {
            LastStatus = 0; // نه پاسخِ HTTP — یعنی ارتباط برقرار نشد
            throw;
        }
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
