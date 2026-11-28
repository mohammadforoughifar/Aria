// وضعیتِ سراسریِ سمت کلاینت: احراز هویت، سبد خرید، مقایسه و اعلان‌ها.

using Northwind.Client.Models;

using Northwind.Client.Contracts;

namespace Northwind.Client.Services;

/* ------------------------------- Toasts -------------------------------- */

public enum ToastTone { Ok, Info, Warn, Bad }

public sealed record ToastItem(string Id, string Title, string? Message, ToastTone Tone);

public sealed class ToastService
{
    public event Action? OnChange;
    public IReadOnlyList<ToastItem> Items => _items;
    private readonly List<ToastItem> _items = new();

    public void Push(string title, string? message = null, ToastTone tone = ToastTone.Ok)
    {
        var item = new ToastItem(Guid.NewGuid().ToString("N"), title, message, tone);
        _items.Add(item);
        OnChange?.Invoke();
        _ = DismissLater(item.Id);
    }

    private async Task DismissLater(string id)
    {
        await Task.Delay(4200);
        Dismiss(id);
    }

    public void Dismiss(string id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item is null) return;
        _items.Remove(item);
        OnChange?.Invoke();
    }
}

/* ------------------------------- Auth ---------------------------------- */

/// <summary>
/// نشستِ کاربر رویِ API نسخهٔ ۱.
///
/// چرا عوض شد؟ پیش‌تر کاربر با هدرِ <c>X-User-Id</c> به پشتهٔ قدیمی معرفی می‌شد
/// و هیچ توکنی وجود نداشت. نسخهٔ ۱ با JWT کار می‌کند، پس:
///   · توکن پس از ورود/ثبت‌نام در localStorage ذخیره می‌شود؛
///   · در همان لحظه رویِ <see cref="V1.IV1Api"/> ست می‌شود تا همهٔ درخواست‌هایِ
///     بعدی خودکار با «Bearer» بروند؛
///   · با باز شدنِ دوبارهٔ برنامه، توکن بازیابی و با <c>GET /account/me</c> بررسی
///     می‌شود (توکنِ منقضی پاک می‌شود، نه اینکه صفحه‌ها را بشکند).
///
/// نشستِ قدیمی (کلیدِ <c>nw.user</c>) یک‌بار پاک می‌شود: شناسهٔ آن در نسخهٔ ۱
/// وجود ندارد و نگه‌داشتنش یعنی صفحه‌هایِ خالی.
/// </summary>
public sealed class AuthState
{
    private const string TokenKey = "nw.v1.token";
    private const string LegacyKey = "nw.user";

    private readonly V1.IV1Api _api;
    private readonly JsInterop _js;

    public event Action? OnChange;

    public UserDto? User { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsAuthenticated => User is not null;
    public bool IsAdmin => User?.Role == Role.Admin;

    /// <summary>پیامِ آخرین خطا — همان چیزی که سرور گفته («ایمیل یا رمزِ عبور درست نیست»).</summary>
    public string? LastError { get; private set; }

    public AuthState(V1.IV1Api api, JsInterop js)
    {
        _api = api;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        // نشستِ پشتهٔ قدیمی دیگر معتبر نیست.
        try
        {
            if (!string.IsNullOrWhiteSpace(await _js.Get(LegacyKey))) await _js.Remove(LegacyKey);
        }
        catch { /* localStorage در دسترس نیست — بی‌اهمیت */ }

        var token = await SafeGet(TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            _api.AccessToken = token;
            _api.MarkSessionResolved(); // دروازه باز می‌شود تا بررسیِ توکن خودش هم رد شود
            var me = await _api.GetAsync<V1.V1User>("api/v1/account/me");
            if (me is null)
            {
                // توکن منقضی یا نامعتبر است → دور ریخته می‌شود
                await ClearTokenAsync();
            }
            else
            {
                User = ToDto(me);
            }
        }

        IsReady = true;
        _api.MarkSessionResolved();
        OnChange?.Invoke();
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        LastError = null;
        try
        {
            var result = await _api.SendAsync<V1.V1AuthResult>("POST", "api/v1/auth/login",
                new { email, password });
            await ApplyAsync(result);
            return true;
        }
        catch (V1.V1ApiException ex) { LastError = ex.Message; return false; }
        catch (Exception ex)
        {
            LastError = "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.";
            Console.WriteLine("[auth] " + ex.Message);
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string fullName, string email, string password,
        string? company, string? country)
    {
        LastError = null;
        try
        {
            var result = await _api.SendAsync<V1.V1AuthResult>("POST", "api/v1/auth/register",
                new { fullName, email, password, company, country });
            await ApplyAsync(result);
            return true;
        }
        catch (V1.V1ApiException ex) { LastError = ex.Message; return false; }
        catch (Exception ex)
        {
            LastError = "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.";
            Console.WriteLine("[auth] " + ex.Message);
            return false;
        }
    }

    /// <summary>به‌روزرسانیِ کاربر در حافظه (پس از ذخیرهٔ پروفایل یا تغییرِ رمز).</summary>
    public async Task UpdateAsync(UserDto user)
    {
        User = user;
        OnChange?.Invoke();
        await Task.CompletedTask;
    }

    public async Task LogoutAsync()
    {
        User = null;
        LastError = null;
        await ClearTokenAsync();
        OnChange?.Invoke();
    }

    /// <summary>ورودِ سریع با حساب‌هایِ نمایشی.</summary>
    public Task<bool> LoginAsDemoAsync(string email) => LoginAsync(email, "Password123!");

    /* ------------------------------ داخلی ------------------------------ */

    private async Task ApplyAsync(V1.V1AuthResult result)
    {
        _api.AccessToken = result.Token;
        User = ToDto(result.User);
        try { await _js.Set(TokenKey, result.Token); } catch { /* بی‌اهمیت */ }
        OnChange?.Invoke();
    }

    private async Task ClearTokenAsync()
    {
        _api.AccessToken = null;
        try { await _js.Remove(TokenKey); } catch { /* بی‌اهمیت */ }
    }

    private async Task<string?> SafeGet(string key)
    {
        try { return await _js.Get(key); } catch { return null; }
    }

    private static UserDto ToDto(V1.V1User u) => new(
        u.Id.ToString(), u.Email, u.FullName, u.Role,
        u.Company, u.Phone, u.Country, u.CreatedAt, u.LastLoginAt);
}

/* -------------------------------- Cart --------------------------------- */

public sealed class CartState
{
    public event Action? OnChange;
    private readonly IStoreApi _api;
    private readonly JsInterop _js;

    public string SessionKey { get; private set; } = "demo";
    public CartDto? Cart { get; private set; }
    public int Count => Cart?.ItemCount ?? 0;
    public bool IsBusy { get; private set; }

    public CartState(IStoreApi api, JsInterop js)
    {
        _api = api;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var key = await _js.Get("nw.cart");
            SessionKey = string.IsNullOrWhiteSpace(key) ? "demo" : key;
            if (key is null) await _js.Set("nw.cart", SessionKey);
            await RefreshAsync();
        }
        catch (Exception ex) { Console.WriteLine("[cart] " + ex.Message); }
    }

    /// <summary>
    /// آخرین پیامِ خطایِ سبد — همان چیزی که سرور گفته («این کدِ تخفیف منقضی شده
    /// است»). صفحهٔ سبد همین را زیرِ فرم نشان می‌دهد تا کاربر بداند چه شد.
    /// </summary>
    public string? Error { get; private set; }

    public async Task RefreshAsync(string? discount = null)
    {
        try
        {
            Cart = await _api.GetCartAsync(SessionKey, discount);
            Error = null;
        }
        catch (V1.V1ApiException ex) { Error = ex.Message; }
        catch (Exception ex) { Error = "سبد بارگیری نشد."; Console.WriteLine("[cart] " + ex.Message); }
        OnChange?.Invoke();
    }

    /// <summary>اجرایِ یک تغییر رویِ سبد، با نگه‌داشتنِ پیامِ خطا برایِ نمایش.</summary>
    private async Task<bool> Run(Func<Task<CartDto>> action)
    {
        IsBusy = true;
        Error = null;
        OnChange?.Invoke();
        try
        {
            Cart = await action();
            return true;
        }
        catch (V1.V1ApiException ex) { Error = ex.Message; return false; }
        catch (Exception ex)
        {
            Error = "ارتباط با سرور برقرار نشد؛ دوباره تلاش کنید.";
            Console.WriteLine("[cart] " + ex.Message);
            return false;
        }
        finally { IsBusy = false; OnChange?.Invoke(); }
    }

    public void ClearError() { Error = null; OnChange?.Invoke(); }

    public Task<bool> AddAsync(string planId, int quantity = 1)
        => Run(() => _api.AddToCartAsync(SessionKey, planId, quantity));

    /// <summary>افزودنِ ماژولِ پیکربندی‌شده (کاربر، شرکتِ اضافه، اندروید/iOS).</summary>
    public Task<bool> AddConfiguredAsync(ModuleSelectionDto selection, int quantity = 1)
        => Run(() => _api.AddConfiguredToCartAsync(SessionKey, selection, quantity));

    public Task<bool> SetQuantityAsync(string itemId, int quantity)
        => Run(() => _api.UpdateCartItemAsync(SessionKey, itemId, quantity));

    public Task<bool> RemoveAsync(string itemId)
        => Run(() => _api.RemoveCartItemAsync(SessionKey, itemId));

    public Task<bool> ApplyDiscountAsync(string code)
        => Run(() => _api.ApplyDiscountAsync(SessionKey, code));

    public async Task ClearLocalAsync()
    {
        Cart = null;
        OnChange?.Invoke();
        await Task.CompletedTask;
    }
}

/* ------------------------------- Compare -------------------------------- */

public sealed class CompareState
{
    public event Action? OnChange;
    private readonly JsInterop _js;
    private readonly HashSet<string> _ids = new();
    private const int Max = 4;

    public IReadOnlyCollection<string> Ids => _ids;
    public int Count => _ids.Count;
    public bool IsFull => _ids.Count >= Max;

    public CompareState(JsInterop js) => _js = js;

    public async Task InitializeAsync()
    {
        var saved = await _js.Get("nw.compare");
        if (!string.IsNullOrWhiteSpace(saved))
            foreach (var id in saved.Split(',', StringSplitOptions.RemoveEmptyEntries)) _ids.Add(id);
        OnChange?.Invoke();
    }

    public bool Contains(string id) => _ids.Contains(id);

    public bool Toggle(string id)
    {
        if (!_ids.Add(id)) { _ids.Remove(id); Persist(); return true; }
        Persist();
        return false;
    }

    public void Remove(string id) { _ids.Remove(id); Persist(); }

    public void Clear() { _ids.Clear(); Persist(); }

    private void Persist()
    {
        _ = _js.Set("nw.compare", string.Join(",", _ids));
        OnChange?.Invoke();
    }
}
