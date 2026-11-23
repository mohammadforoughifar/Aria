// سرویسِ مستقلِ احرازِ هویت.
//
// چرا جدا از IStoreApi است؟
//   IStoreApi (HttpStoreApi) برای فرستادنِ هدرِ X-User-Id به AuthState نیاز دارد؛
//   اگر AuthState هم برای ورود و ثبت‌نام به IStoreApi وابسته باشد، در تزریقِ
//   وابستگی یک حلقه ساخته می‌شود:
//       IStoreApi -> AuthState -> IStoreApi   (خطای Circular dependency)
//   جدا کردنِ این چند فراخوانی حلقه را می‌شکند و وابستگی را یک‌سویه می‌کند:
//       IStoreApi -> AuthState -> IAuthApi -> HttpClient

using System.Net.Http.Json;
using Northwind.Shared.Contracts;

namespace Northwind.Client.Services;

public interface IAuthApi
{
    Task<UserDto?> LoginAsync(string email, string password);
    Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country);
    Task<UserDto?> MeAsync();
}

public sealed class AuthApi : IAuthApi
{
    private readonly HttpClient _http;
    private readonly ApiHealth _health;

    public AuthApi(HttpClient http, ApiHealth health)
    {
        _http = http;
        _health = health;
    }

    public async Task<UserDto?> LoginAsync(string email, string password)
        => await Send<UserDto>("api/auth/login", ApiJson.Body(new { email, password }));

    public async Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country)
        => await Send<UserDto>("api/auth/register",
               ApiJson.Body(new { fullName, email, password, company, country }));

    public async Task<UserDto?> MeAsync()
        => await Get<UserDto>("api/account/me");

    /* ------------------------------------------------------------------
       چرا این پوشش لازم است؟
       اگر سرور در دسترس نباشد، درخواست یا خطا پرتاب می‌کند (ارتباط رد شد)
       یا از وب‌سرورِ خودِ کلاینت پاسخی مثلِ ۴۰۵ می‌گیرد. در هر دو حالت
       واقعیت این است که «سرور پاسخ نمی‌دهد»، نه اینکه «رمز اشتباه است».
       پس خطا را مهار می‌کنیم و وضعیت را به ApiHealth گزارش می‌دهیم تا
       پیامِ درست به کاربر نشان داده شود.
       ------------------------------------------------------------------ */
    private async Task<T?> Send<T>(string url, HttpContent body)
    {
        try
        {
            var res = await _http.PostAsync(url, body);
            if (res.IsSuccessStatusCode)
            {
                _health.ReportSuccess();
                return await res.Content.ReadFromJsonAsync<T>(ApiJson.Options);
            }

            // ۴۰۴ و ۴۰۵ یعنی اصلاً به Api نرسیدیم — سرور در دسترس نیست
            if (res.StatusCode is System.Net.HttpStatusCode.NotFound
                             or System.Net.HttpStatusCode.MethodNotAllowed)
                _health.ReportFailure("سرورِ API در دسترس نیست (پاسخ: " + (int)res.StatusCode + ").");
            else
                _health.ReportSuccess();     // سرور پاسخ داده، فقط ورود ناموفق بود
            return default;
        }
        catch (HttpRequestException)
        {
            _health.ReportFailure("ارتباط با سرور برقرار نشد.");
            return default;
        }
        catch (Exception ex)
        {
            _health.ReportFailure(ex.Message);
            return default;
        }
    }

    private async Task<T?> Get<T>(string url)
    {
        try
        {
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) return default;
            _health.ReportSuccess();
            return await res.Content.ReadFromJsonAsync<T>(ApiJson.Options);
        }
        catch (HttpRequestException) { _health.ReportFailure("ارتباط با سرور برقرار نشد."); return default; }
        catch { return default; }
    }
}
