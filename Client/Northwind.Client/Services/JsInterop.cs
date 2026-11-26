// دسترسیِ کم‌حجم به localStorage و چند تعاملِ مرورگری.
// همهٔ تعامل‌ها اختیاری‌اند: اگر JS در دسترس نباشد (پیش‌رندر)، برنامه همچنان کار می‌کند.

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Northwind.Client.Contracts;

namespace Northwind.Client.Services;

public sealed class JsInterop
{
    private readonly IJSRuntime _js;

    public JsInterop(IJSRuntime js) => _js = js;

    public async Task<string?> Get(string key)
    {
        try { return await _js.InvokeAsync<string?>("nw.get", key); }
        catch { return null; }
    }

    public async Task Set(string key, string value)
    {
        try { await _js.InvokeVoidAsync("nw.set", key, value); }
        catch { /* نادیده گرفتن */ }
    }

    public async Task Remove(string key)
    {
        try { await _js.InvokeVoidAsync("nw.remove", key); }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>خواندنِ تمِ ذخیره‌شده (light/dark).</summary>
    public async Task<bool> GetThemeAsync()
    {
        try { return await _js.InvokeAsync<bool>("nw.isDark"); }
        catch { return false; }
    }

    /// <summary>تنظیم و ذخیرهٔ تم.</summary>
    public async Task SetThemeAsync(string theme)
    {
        try { await _js.InvokeVoidAsync("nw.setTheme", theme); }
        catch { /* نادیده گرفتن */ }
    }

    public async Task<bool> Copy(string text)
    {
        try { return await _js.InvokeAsync<bool>("nw.copy", text); }
        catch { return false; }
    }

    /// <summary>وقتی صفحه اسکرول می‌خورد، یک کلاس به هدر اضافه می‌شود.</summary>
    public async Task WatchSticky(string selector, string className)
    {
        try { await _js.InvokeVoidAsync("nw.watchScroll", selector, className); }
        catch { /* نادیده گرفتن */ }
    }

    public async Task ScrollTop()
    {
        try { await _js.InvokeVoidAsync("nw.scrollTop"); }
        catch { /* نادیده گرفتن */ }
    }

    /* ------------------- دیاگرامِ اکوسیستم ------------------- */

    /// <summary>آیا کاربر ترجیحِ «کاهشِ حرکت» را فعال کرده است؟</summary>
    public async Task<bool> EcoPrefersReducedMotion()
    {
        try { return await _js.InvokeAsync<bool>("nw.reduceMotion"); }
        catch { return false; }
    }

    /* ------------------- گرافیکِ سرصفحهٔ نرم‌افزارها ------------------- */

    private IJSObjectReference? _heroModule;

    private async Task<IJSObjectReference?> HeroModule()
    {
        try
        {
            _heroModule ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/hero-orbit.js");
            return _heroModule;
        }
        catch { return null; }
    }

    /// <summary>راه‌اندازیِ موتورِ گرافیک و شمارندهٔ آمار روی سرصفحه.</summary>
    public async Task HeroOrbitMount(ElementReference orb, ElementReference stats)
    {
        var mod = await HeroModule();
        if (mod is null) return;
        try
        {
            await mod.InvokeVoidAsync("init", orb);
            await mod.InvokeVoidAsync("initStats", stats);
        }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>رسیدنِ دادهٔ واقعی: از عددِ کنونی به عددِ تازه می‌رویم.</summary>
    public async Task HeroStatsUpdate(ElementReference stats)
    {
        var mod = await HeroModule();
        if (mod is null) return;
        try { await mod.InvokeVoidAsync("updateStats", stats); }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>توقفِ حلقه و آزادسازیِ دیده‌بان‌ها.</summary>
    public async Task HeroOrbitDispose()
    {
        try
        {
            if (_heroModule is not null) await _heroModule.InvokeVoidAsync("dispose");
        }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>ماژولِ ESِ اکوسیستم (یک‌بار بارگیری و نگه‌داری می‌شود).</summary>
    private IJSObjectReference? _ecoModule;
    // (ماژولِ سرصفحه در بالا تعریف شده)
    private IJSObjectReference? _ecoHandle;   // نمونهٔ زندهٔ دیاگرام

    private async Task<IJSObjectReference?> EcoModule()
    {
        try
        {
            _ecoModule ??= await _js.InvokeAsync<IJSObjectReference>("import", "./js/ecosystem.js");
            return _ecoModule;
        }
        catch { return null; }
    }

    /// <summary>راه‌اندازیِ دیده‌بان‌ها، هم‌ترازی و هندسهٔ اتصال‌ها.</summary>
    public async Task EcoMount(ElementReference el)
    {
        var mod = await EcoModule();
        if (mod is null) return;
        try
        {
            _ecoHandle?.DisposeAsync();
            _ecoHandle = await mod.InvokeAsync<IJSObjectReference>("create", el);
        }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>اندازه‌گیریِ دوباره — پس از تغییرِ سناریو یا اندازهٔ کارت‌ها.</summary>
    public async Task EcoUpdate()
    {
        if (_ecoHandle is null) return;
        try { await _ecoHandle.InvokeVoidAsync("update"); }
        catch { /* نادیده گرفتن */ }
    }

    /// <summary>آزادسازیِ همهٔ دیده‌بان‌ها هنگامِ از بین رفتنِ مؤلفه.</summary>
    public async Task EcoUnmount()
    {
        try
        {
            if (_ecoHandle is not null) { await _ecoHandle.InvokeVoidAsync("destroy"); await _ecoHandle.DisposeAsync(); }
        }
        catch { /* نادیده گرفتن */ }
        _ecoHandle = null;
    }

    /// <summary>تمرکز روی یک المان با شناسه (برای پیمایشِ صفحه‌کلیدیِ تب‌ها).</summary>
    public async Task FocusElementById(string id)
    {
        try { await _js.InvokeVoidAsync("nw.focusById", id); }
        catch { /* نادیده گرفتن */ }
    }
}
