// وضعیتِ سراسریِ سمت کلاینت: احراز هویت، سبد خرید، مقایسه و اعلان‌ها.

using Northwind.Client.Models;

using Northwind.Shared.Contracts;

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

public sealed class AuthState
{
    public event Action? OnChange;
    private readonly IAuthApi _api;
    private readonly JsInterop _js;

    public UserDto? User { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsAuthenticated => User is not null;
    public bool IsAdmin => User?.Role == Role.Admin;

    public AuthState(IAuthApi api, JsInterop js)
    {
        _api = api;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        // در نسخهٔ Mock، کاربرِ آخرین نشست را از localStorage برمی‌گردانیم.
        var saved = await _js.Get("nw.user");
        if (!string.IsNullOrWhiteSpace(saved))
        {
            var parts = saved.Split('|');
            if (parts.Length == 4 && Enum.TryParse<Role>(parts[2], out var role))
                User = new UserDto(parts[0], parts[1], parts[3], role, null, null, null, DateTime.UtcNow, null);
        }
        IsReady = true;
        OnChange?.Invoke();
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var user = await _api.LoginAsync(email, password);
        if (user is null) return false;
        User = user;
        await _js.Set("nw.user", $"{user.Id}|{user.Email}|{user.Role}|{user.FullName}");
        OnChange?.Invoke();
        return true;
    }

    public async Task<bool> RegisterAsync(string fullName, string email, string password, string? company, string? country)
    {
        var user = await _api.RegisterAsync(fullName, email, password, company, country);
        if (user is null) return false;
        User = user;
        await _js.Set("nw.user", $"{user.Id}|{user.Email}|{user.Role}|{user.FullName}");
        OnChange?.Invoke();
        return true;
    }

    public async Task UpdateAsync(UserDto user)
    {
        User = user;
        await _js.Set("nw.user", $"{user.Id}|{user.Email}|{user.Role}|{user.FullName}");
        OnChange?.Invoke();
    }

    public async Task LogoutAsync()
    {
        User = null;
        await _js.Remove("nw.user");
        OnChange?.Invoke();
    }

    /// <summary>ورودِ سریع با یکی از حساب‌های دمو (فقط در حالت Mock).</summary>
    public Task<bool> LoginAsDemoAsync(string email) => LoginAsync(email, "Password123!");
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

    public async Task RefreshAsync(string? discount = null)
    {
        try { Cart = await _api.GetCartAsync(SessionKey, discount); }
        catch (Exception ex) { Console.WriteLine("[cart] " + ex.Message); }
        OnChange?.Invoke();
    }

    private async Task Run(Func<Task<CartDto>> action)
    {
        IsBusy = true;
        OnChange?.Invoke();
        try { Cart = await action(); }
        catch (Exception ex) { Console.WriteLine("[cart] " + ex.Message); }
        finally { IsBusy = false; OnChange?.Invoke(); }
    }

    public Task AddAsync(string planId, int quantity = 1)
        => Run(() => _api.AddToCartAsync(SessionKey, planId, quantity));

    public Task SetQuantityAsync(string itemId, int quantity)
        => Run(() => _api.UpdateCartItemAsync(SessionKey, itemId, quantity));

    public Task RemoveAsync(string itemId)
        => Run(() => _api.RemoveCartItemAsync(SessionKey, itemId));

    public Task ApplyDiscountAsync(string code)
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
