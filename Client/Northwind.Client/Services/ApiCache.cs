// حافظهٔ کوتاه‌مدت برای داده‌هایِ کم‌تغییر.
//
// چرا لازم است؟
// پیش از این هر مؤلفه داده‌هایِ مشترک را جداگانه می‌گرفت: تنها با رفتن به صفحهٔ
// اصلی، تنظیماتِ سایت چهار بار و فهرستِ محصولات سه بار از سرور درخواست می‌شد.
// حالا این داده‌ها یک‌بار گرفته و برایِ مدتِ کوتاهی نگه داشته می‌شوند.
//
// نکته: تنها پاسخ‌هایِ موفق ذخیره می‌شوند؛ در صورتِ خطا چیزی کش نمی‌شود تا
// برنامه بتواند بارِ بعد دوباره تلاش کند.

namespace Northwind.Client.Services;

public sealed class ApiCache
{
    private readonly Dictionary<string, Entry> _items = new(StringComparer.Ordinal);
    private readonly TimeSpan _defaultTtl;

    public ApiCache() : this(TimeSpan.FromSeconds(60)) { }

    public ApiCache(TimeSpan defaultTtl) => _defaultTtl = defaultTtl;

    private sealed record Entry(DateTimeOffset ExpiresAt, object? Value);

    public bool TryGet<T>(string key, out T? value)
    {
        value = default;
        if (!_items.TryGetValue(key, out var entry)) return false;
        if (DateTimeOffset.UtcNow >= entry.ExpiresAt)
        {
            _items.Remove(key);
            return false;
        }
        if (entry.Value is T typed) { value = typed; return true; }
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan? ttl = null)
        => _items[key] = new Entry(DateTimeOffset.UtcNow.Add(ttl ?? _defaultTtl), value);

    // درخواست‌هایی که هم‌اکنون در جریان‌اند.
    // چرا؟ چند مؤلفه هم‌زمان یک چیز را می‌خواهند (مثلاً تنظیماتِ سایت را سه بخش
    // با هم درخواست می‌کنند). اگر فقط نتیجه را کش کنیم، هر سه هنوز به کش
    // نرسیده و هر سه درخواست می‌فرستند. با نگه داشتنِ خودِ «وظیفه»،
    // همهٔ آن‌ها منتظرِ یک پاسخِ مشترک می‌مانند.
    private readonly Dictionary<string, object> _inFlight = new(StringComparer.Ordinal);

    /// <summary>
    /// نتیجهٔ کش‌شده را برمی‌گرداند، یا کارخانه را اجرا می‌کند.
    /// اگر درخواستِ همسانِ دیگری هم‌اکنون در جریان باشد، منتظرِ همان می‌ماند
    /// (یعنی فقط یک فراخوانیِ واقعی به سرور می‌رود).
    /// </summary>
    public async Task<T?> GetOrAddAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? ttl = null)
    {
        if (TryGet<T>(key, out var cached) && cached is not null) return cached;

        if (_inFlight.TryGetValue(key, out var pending) && pending is Task<T?> inflight)
            return await inflight;

        var task = factory();
        _inFlight[key] = task;
        try
        {
            var result = await task;
            // فقط پاسخ‌هایِ موفق کش می‌شوند تا در صورتِ خطا بتوان دوباره تلاش کرد
            if (result is not null) Set(key, result, ttl);
            return result;
        }
        finally
        {
            _inFlight.Remove(key);
        }
    }

    /// <summary>
    /// پاک کردنِ یک کلید، یا همهٔ کلیدهایی که با این پیشوند شروع می‌شوند.
    /// بعد از ویرایش در پنلِ مدیریت صدا زده می‌شود تا تغییر بی‌درنگ دیده شود.
    /// </summary>
    public void Invalidate(string? keyOrPrefix = null)
    {
        if (string.IsNullOrEmpty(keyOrPrefix)) { _items.Clear(); return; }
        _items.Remove(keyOrPrefix);
        foreach (var k in _items.Keys.Where(k => k.StartsWith(keyOrPrefix, StringComparison.Ordinal)).ToList())
            _items.Remove(k);
    }
}
