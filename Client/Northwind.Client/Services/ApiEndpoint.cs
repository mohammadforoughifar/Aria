// انتخاب‌گرِ نشانیِ API.
//
// چرا لازم است؟
// برنامه دو جور اجرا می‌شود و در هر دو باید بدون تغییرِ دستی کار کند:
//
//   ۱) تک‌مبدأ (رابط و API روی یک نشانی، مثل http://localhost:5210)
//      → درخواست‌های نسبیِ «api/...» درست کار می‌کنند.
//
//   ۲) کلاینتِ جدا (مثل http://localhost:5212) + سرورِ جدا (http://localhost:5182)
//      → درخواستِ نسبی به وب‌سرورِ خودِ کلاینت می‌رود و به‌جای JSON صفحهٔ HTML
//        برمی‌گردد؛ همان خطای معروفِ «'<' is an invalid start of a value».
//
// این کلاس یک‌بار، پیش از نخستین درخواست، نشانی‌هایِ محتمل را یکی‌یکی می‌آزماید
// و اولین نشانی‌ای که واقعاً JSON برگرداند را برمی‌گزیند. نتیجه کش می‌شود؛
// ولی اگر همه شکست خوردند، کشِ شکست کوتاه‌مدت است تا بعد از روشن شدنِ سرور
// برنامه خودبه‌خود درست شود (بدون نیاز به رفرش).

using System.Net.Http.Headers;

namespace Northwind.Client.Services;

public sealed class ApiEndpoint
{
    private readonly string[] _candidates;
    private readonly HttpClient _probe;

    private string? _resolved;
    private DateTimeOffset _failedUntil;
    private int _failures;               // شمارِ شکست‌هایِ پیاپی — برایِ بازگشتِ تدریجی
    private Task<string>? _pending;

    /// <param name="configured">نشانیِ تنظیم‌شده در appsettings (می‌تواند تهی باشد).</param>
    /// <param name="sameOrigin">نشانیِ هم‌ریشه با خودِ برنامه.</param>
    public ApiEndpoint(string? configured, string sameOrigin)
    {
        var list = new List<string>();
        Add(list, sameOrigin);   // نخست هم‌ریشه — در اجرایِ تک‌مبدأ همان درست است
        Add(list, configured);   // سپس نشانیِ تنظیم‌شده — برای اجرایِ کلاینتِ جدا
        if (list.Count == 0) list.Add(sameOrigin);
        _candidates = list.ToArray();

        _probe = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
    }

    private static void Add(List<string> list, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var v = value.Trim();
        if (!v.EndsWith('/')) v += '/';
        if (!Uri.TryCreate(v, UriKind.Absolute, out _)) return;
        if (!list.Contains(v, StringComparer.OrdinalIgnoreCase)) list.Add(v);
    }

    /// <summary>نشانیِ پایهٔ API (با اسلشِ پایانی).</summary>
    public Task<string> ResolveAsync(CancellationToken ct = default)
    {
        if (_resolved is not null) return Task.FromResult(_resolved);

        // اگر هنوز وقتِ تلاشِ دوباره نرسیده، همان نشانیِ نخست را برمی‌گردانیم؛
        // خطا در لایهٔ داده مهار می‌شود و پیامِ دوستانه نشان داده خواهد شد.
        if (DateTimeOffset.UtcNow < _failedUntil) return Task.FromResult(_candidates[0]);

        // تلاشِ هم‌زمانِ چند درخواست نباید چند بار کاوش کند
        return _pending ??= ProbeAsync(ct);
    }

    private async Task<string> ProbeAsync(CancellationToken ct)
    {
        try
        {
            foreach (var candidate in _candidates)
            {
                if (await LooksLikeApiAsync(candidate, ct))
                {
                    _resolved = candidate;
                    _failures = 0;
                    return candidate;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[endpoint] " + ex.Message);
        }
        finally
        {
            _pending = null;
        }

        /* همه شکست خوردند. دوباره می‌آزماییم، اما با فاصلهٔ بیشتر:
           هر بار که بی‌نتیجه ماند، دوبرابر صبر می‌کنیم (تا حداکثرِ یک دقیقه).
           علت: وقتی سرور روشن نیست، تلاشِ پیاپیِ هر سه ثانیه فقط ترمینال را
           پر از خطا می‌کند؛ و وقتی بالاخره روشن شد، برنامه خودبه‌خود درست
           می‌شود — بی‌آنکه کاربر نیازی به رفرش داشته باشد.                  */
        _failures++;
        var wait = TimeSpan.FromSeconds(Math.Min(60, 3 * (1 << Math.Min(_failures, 5))));
        _failedUntil = DateTimeOffset.UtcNow.Add(wait);
        return _candidates[0];
    }

    /// <summary>
    /// آیا این نشانی واقعاً API است؟ ملاک: پاسخِ موفق که محتوایش JSON باشد.
    /// (وب‌سرورِ کلاینت در صورتِ نبودنِ API، index.html با Content-Type
    /// متفاوت برمی‌گرداند — دقیقاً همان چیزی که باید رد شود.)
    /// </summary>
    private async Task<bool> LooksLikeApiAsync(string candidate, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, candidate + "api/v1/settings");
            using var res = await _probe.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return false;

            var type = res.Content.Headers.ContentType?.MediaType ?? "";
            return type.Contains("json", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;   // connection refused، timeout و غیره
        }
    }
}

/// <summary>
/// نشانیِ هر درخواست را روی پایهٔ درستِ API می‌نشاند.
/// با این کار هیچ کدی در صفحه‌ها یا لایهٔ داده نیاز به تغییر ندارد.
/// </summary>
public sealed class ApiBaseHandler : DelegatingHandler
{
    private readonly ApiEndpoint _endpoint;

    public ApiBaseHandler(ApiEndpoint endpoint) => _endpoint = endpoint;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        var path = uri is null ? null : (uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString);

        // فقط درخواست‌هایِ API را بازنویسی می‌کنیم؛ فایل‌هایِ محلیِ خودِ برنامه
        // (مثل data/ecosystem.json) دست‌نخورده می‌مانند.
        if (!string.IsNullOrEmpty(path) && path.Contains("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = await _endpoint.ResolveAsync(cancellationToken);
            request.RequestUri = new Uri(new Uri(baseUrl), path.TrimStart('/'));
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
