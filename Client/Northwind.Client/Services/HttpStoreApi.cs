// پیاده‌سازیِ واقعیِ IStoreApi روی HttpClient.
// تمامِ صفحات فقط IStoreApi را می‌شناسند؛ تعویضِ MockStoreApi با این کلاس
// یک خط در Program.cs است و هیچ صفحه‌ای تغییر نمی‌کند.

using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Northwind.Shared.Contracts;

namespace Northwind.Client.Services;

public sealed class HttpStoreApi : IStoreApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;
    private readonly ApiHealth _health;
    private readonly ApiCache _cache;

    /// <summary>تنظیماتِ یکسان با سرور (از ApiJson می‌آید تا با AuthApi مشترک باشد).</summary>
    private static JsonSerializerOptions Json => ApiJson.Options;

    private static StringContent Body(object? value) => ApiJson.Body(value);

    public HttpStoreApi(HttpClient http, AuthState auth, ApiHealth health, ApiCache cache)
    {
        _http = http;
        _auth = auth;
        _health = health;
        _cache = cache;
    }

    /// <summary>هدرِ شناسهٔ کاربر — سرور با آن تشخیص می‌دهد چه کسی درخواست می‌دهد.</summary>
    private void ApplyUser()
    {
        var uid = _auth.User?.Id ?? "";
        // هدر باید با هر تغییرِ کاربر به‌روزرسانی شود؛ HttpClient در WASM
        // برای کلِ عمرِ برنامه زنده است و مقدارِ قبلی را نگه می‌دارد.
        if (_http.DefaultRequestHeaders.TryGetValues("X-User-Id", out var current))
        {
            if (current.FirstOrDefault() == uid) return;
            _http.DefaultRequestHeaders.Remove("X-User-Id");
        }
        if (!string.IsNullOrEmpty(uid)) _http.DefaultRequestHeaders.Add("X-User-Id", uid);
    }

    /// <summary>هر خطایِ ارتباطی یا تجزیه‌ای را مهار می‌کند تا یک درخواستِ ناموفق کلِ برنامه را از کار نیندازد.</summary>
    private async Task<T?> Get<T>(string url)
    {
        try
        {
            ApplyUser();
            var res = await _http.GetAsync(url);
            if (!res.IsSuccessStatusCode) { _health.ReportFailure(url); return default; }
            var value = await res.Content.ReadFromJsonAsync<T>(Json);
            _health.ReportSuccess();
            return value;
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); return default; }
    }

    private async Task<T?> Post<T>(string url, object? body)
    {
        try
        {
            ApplyUser();
            var res = await _http.PostAsync(url, Body(body));
            if (!res.IsSuccessStatusCode) { _health.ReportFailure(url); return default; }
            var value = await res.Content.ReadFromJsonAsync<T>(Json);
            _health.ReportSuccess();
            return value;
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); return default; }
    }

    private async Task<T?> Put<T>(string url, object? body)
    {
        try
        {
            ApplyUser();
            var res = await _http.PutAsync(url, Body(body));
            if (!res.IsSuccessStatusCode) { _health.ReportFailure(url); return default; }
            var value = await res.Content.ReadFromJsonAsync<T>(Json);
            _health.ReportSuccess();
            return value;
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); return default; }
    }

    private static string Q(params (string, object?)[] parts) => ApiJson.Q(parts);

    /// <summary>
    /// ارسال بدونِ انتظارِ پاسخ — برای مسیرهایی که بدنه ندارند (مثلِ Ok خالی).
    /// اگر بدنه را بخوانیم، تجزیهٔ JSON روی پاسخِ تهی خطا می‌دهد در حالی که
    /// کار در سرور با موفقیت انجام شده است.
    /// </summary>
    private async Task<bool> Send(string url, object? body)
    {
        try
        {
            ApplyUser();
            var res = await _http.PostAsync(url, Body(body));
            if (!res.IsSuccessStatusCode) { _health.ReportFailure(url); return false; }
            _health.ReportSuccess();
            return true;
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); return false; }
    }

    /// <summary>پیامی کوتاه و قابل‌فهم از خطا می‌سازد.</summary>
    private static string Describe(Exception ex, string url) => ex switch
    {
        System.Text.Json.JsonException
            => "پاسخِ سرور قابلِ خواندن نیست — احتمالاً نشانیِ API اشتباه است.",
        HttpRequestException
            => "ارتباط با سرور برقرار نشد.",
        _ => ex.Message,
    };

    /// <summary>حذفِ بی‌خطر — خطا را مهار می‌کند و فقط گزارش می‌دهد.</summary>
    private async Task SafeDelete(string url)
    {
        try
        {
            ApplyUser();
            var res = await _http.DeleteAsync(url);
            if (res.IsSuccessStatusCode) _health.ReportSuccess(); else _health.ReportFailure(url);
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); }
    }

    private static AdminOverviewDto FallbackOverview()
        => new(0, 0, 0, 0, 0, 0, 0, 0,
               Array.Empty<RevenuePointDto>(), Array.Empty<TopProductDto>(),
               Array.Empty<OrderDto>(), Array.Empty<TicketDto>());

    private static CustomerOverviewDto FallbackCustomerOverview()
        => new(0, 0, 0, 0, 0, 0, 0,
               Array.Empty<OrderDto>(), Array.Empty<LicenseDto>(), Array.Empty<RevenuePointDto>());

    /* ------------------------------ تنظیمات ------------------------------ */

    public async Task<SiteSettingsDto> GetSettingsAsync()
    {
        // چند بخش هم‌زمان تنظیمات را می‌خواهند؛ فقط یکی به سرور می‌رود
        var dto = await _cache.GetOrAddAsync("settings", () => Get<SiteSettingsDto>("api/settings"));
        return dto ?? DefaultSettings();
    }

    private async Task<SiteSettingsDto> GetSettingsUncachedAsync()
        // اگر سرور در دسترس نباشد، مقدارِ پیش‌فرض برمی‌گردد تا صفحه سفید نشود
        => await Get<SiteSettingsDto>("api/settings") ?? DefaultSettings();

    private static SiteSettingsDto DefaultSettings()
        => new("فروغ آریا پارسیان", "", "", "",
               "مجموعهٔ نرم‌افزار یکپارچه برای کسب‌وکار شما",
               "ماژول‌های ERP، برنامه‌های دسکتاپ و افزونه‌ها — با پشتیبانی، بروزرسانی و نصبِ رایگان.",
               900, 14, false, "");

    public async Task<SiteSettingsDto> UpdateSettingsAsync(SiteSettingsDto settings)
    {
        var saved = await Put<SiteSettingsDto>("api/settings", settings);
        _cache.Invalidate("settings");      // تغییر باید بی‌درنگ دیده شود
        return saved ?? settings;
    }

    /* ------------------------------ کاتالوگ ------------------------------ */

    public async Task<Paged<ProductListItemDto>> GetProductsAsync(ProductQuery q)
        => await Get<Paged<ProductListItemDto>>("api/products" + Q(
               ("page", q.Page), ("pageSize", q.PageSize), ("search", q.Search),
               ("sortBy", q.SortBy), ("sortDir", q.SortDir), ("category", q.Category),
               ("type", q.Type?.ToString()), ("featured", q.Featured),
               ("includeInactive", q.IncludeInactive ? true : null)))
           ?? Paged<ProductListItemDto>.Empty(q.PageSize);

    public Task<ProductDetailDto?> GetProductBySlugAsync(string slug) => Get<ProductDetailDto?>($"api/products/slug/{slug}");

    public Task<ProductDetailDto?> GetProductByIdAsync(string id) => Get<ProductDetailDto?>($"api/products/{id}");

    public async Task<IReadOnlyList<ProductDetailDto>> GetProductsByIdsAsync(IEnumerable<string> ids)
    {
        // یک درخواست برایِ همه — پیش از این به‌ازایِ هر شناسه یک درخواست جدا می‌رفت
        var joined = string.Join(",", ids);
        if (string.IsNullOrEmpty(joined)) return Array.Empty<ProductDetailDto>();

        var list = await Get<IReadOnlyList<ProductDetailDto>>("api/products/bulk" + Q(("ids", joined)));
        return list ?? Array.Empty<ProductDetailDto>();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive = false)
    {
        // فهرستِ مدیریت (با غیرفعال‌ها) همیشه تازه است و کش نمی‌شود
        if (includeInactive)
            return await Get<IReadOnlyList<CategoryDto>>("api/categories" + Q(("includeInactive", true)))
                   ?? Array.Empty<CategoryDto>();

        var list = await _cache.GetOrAddAsync("categories",
            () => Get<IReadOnlyList<CategoryDto>>("api/categories"));
        return list ?? Array.Empty<CategoryDto>();
    }

    private async Task<IReadOnlyList<CategoryDto>> GetCategoriesUncachedAsync(bool includeInactive = false)
        => await Get<IReadOnlyList<CategoryDto>>($"api/categories{Q(("includeInactive", includeInactive ? true : null))}") ?? Array.Empty<CategoryDto>();

    public async Task<CategoryDto> UpsertCategoryAsync(CategoryDto c)
    {
        var saved = await Post<CategoryDto>("api/categories", c);
        _cache.Invalidate("categories");
        return saved ?? c;
    }

    public async Task DeleteCategoryAsync(string id)
    {
        await SafeDelete($"api/categories/{id}");
        _cache.Invalidate("categories");
    }

    public async Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync()
    {
        var list = await _cache.GetOrAddAsync("testimonials",
            () => Get<IReadOnlyList<TestimonialDto>>("api/testimonials"));
        return list ?? Array.Empty<TestimonialDto>();
    }

    private async Task<IReadOnlyList<TestimonialDto>> GetTestimonialsUncachedAsync()
        => await Get<IReadOnlyList<TestimonialDto>>("api/testimonials") ?? Array.Empty<TestimonialDto>();

    public async Task<IReadOnlyList<FaqDto>> GetFaqsAsync()
    {
        var list = await _cache.GetOrAddAsync("faqs", () => Get<IReadOnlyList<FaqDto>>("api/faqs"));
        return list ?? Array.Empty<FaqDto>();
    }

    private async Task<IReadOnlyList<FaqDto>> GetFaqsUncachedAsync()
        => await Get<IReadOnlyList<FaqDto>>("api/faqs") ?? Array.Empty<FaqDto>();

    /* -------------------------------- سبد -------------------------------- */

    public Task<CartDto> GetCartAsync(string sessionKey, string? discountCode = null)
        => Get<CartDto>($"api/cart{Q(("sessionKey", sessionKey), ("discountCode", discountCode))}")!;

    public Task<CartDto> AddToCartAsync(string sessionKey, string planId, int quantity = 1)
        => Post<CartDto>($"api/cart/items{Q(("sessionKey", sessionKey))}", new { planId, quantity })!;

    public Task<CartDto> UpdateCartItemAsync(string sessionKey, string itemId, int quantity)
        => Put<CartDto>($"api/cart/items/{itemId}{Q(("sessionKey", sessionKey))}", new { quantity })!;

    public Task<CartDto> RemoveCartItemAsync(string sessionKey, string itemId)
        => _delete<CartDto>($"api/cart/items/{itemId}{Q(("sessionKey", sessionKey))}");

    public Task<CartDto> ApplyDiscountAsync(string sessionKey, string code)
        => Post<CartDto>($"api/cart/discount{Q(("sessionKey", sessionKey))}", new { code })!;

    private async Task<T?> _delete<T>(string url)
    {
        ApplyUser();
        var res = await _http.DeleteAsync(url);
        return res.IsSuccessStatusCode ? await res.Content.ReadFromJsonAsync<T>(Json) : default;
    }

    /* ------------------------------- سفارش‌ها ------------------------------ */

    public async Task<Paged<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
        => await Get<Paged<OrderDto>>($"api/orders{Q(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}")
           ?? Paged<OrderDto>.Empty(pageSize);

    public Task<OrderDto?> GetOrderAsync(string id) => Get<OrderDto?>($"api/orders/{id}");

    public async Task<OrderDto> CheckoutAsync(CheckoutInput input)
    {
        var dto = new CheckoutDto(input.SessionKey, input.DiscountCode, input.PaymentMethod,
            input.CardName, input.CardNumber, input.BillingName, input.BillingCompany,
            input.BillingEmail, input.BillingCountry, input.VatId, input.Notes,
            AccountType.Individual, null, null, input.BillingCompany);
        return await Post<OrderDto>("api/checkout", dto) ?? throw new InvalidOperationException("ثبتِ سفارش ناموفق بود.");
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(string id, OrderStatus status)
        => await _patch<OrderDto>($"api/orders/{id}/status", new { status = status.ToString() })!;

    private async Task<T?> _patch<T>(string url, object body)
    {
        try
        {
            ApplyUser();
            var res = await _http.PatchAsync(url, Body(body));
            if (!res.IsSuccessStatusCode) { _health.ReportFailure(url); return default; }
            var value = await res.Content.ReadFromJsonAsync<T>(Json);
            _health.ReportSuccess();
            return value;
        }
        catch (Exception ex) { _health.ReportFailure(Describe(ex, url)); return default; }
    }

    /* ------------------------------- فاکتورها ----------------------------- */

    public async Task<Paged<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 10)
        => await Get<Paged<InvoiceDto>>($"api/invoices{Q(("page", page), ("pageSize", pageSize))}")
           ?? Paged<InvoiceDto>.Empty(pageSize);

    public Task<InvoiceDto?> GetInvoiceAsync(string id) => Get<InvoiceDto?>($"api/invoices/{id}");

    /* ------------------------------- لایسنس ------------------------------- */

    public async Task<Paged<LicenseDto>> GetLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
        => await Get<Paged<LicenseDto>>($"api/licenses{Q(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}")
           ?? Paged<LicenseDto>.Empty(pageSize);

    public async Task<LicenseDto> LicenseActionAsync(string id, string action, int? days = null, string? machineName = null)
        => await Post<LicenseDto>($"api/licenses/{id}/action", new { action, days, machineName })!;

    public async Task<LicenseDto> GenerateLicenseAsync(string userId, string productId, string? planId, int seats, int? validDays)
        => await Post<LicenseDto>("api/licenses/generate", new { userId, productId, planId, seats, validDays })!;

    /* ------------------------------- دانلود ------------------------------- */

    public async Task<IReadOnlyList<AvailableDownloadDto>> GetAvailableDownloadsAsync()
        => await Get<IReadOnlyList<AvailableDownloadDto>>("api/downloads/available") ?? Array.Empty<AvailableDownloadDto>();

    public async Task<IReadOnlyList<DownloadDto>> GetDownloadHistoryAsync()
        => await Get<IReadOnlyList<DownloadDto>>("api/downloads/history") ?? Array.Empty<DownloadDto>();

    public async Task<DownloadDto> RecordDownloadAsync(string productId, string? versionId, string kind, string fileName, int sizeMb)
        => await Post<DownloadDto>("api/downloads", new { productId, versionId, kind, fileName, sizeMb, requiresLicense = true })!;

    /* ------------------------------- دیدگاه‌ها ---------------------------- */

    public async Task<Paged<ReviewDto>> GetReviewsAsync(int page = 1, int pageSize = 10, string? productId = null, ReviewStatus? status = null)
        => await Get<Paged<ReviewDto>>($"api/reviews{Q(("page", page), ("pageSize", pageSize), ("productId", productId), ("status", status?.ToString()))}")
           ?? Paged<ReviewDto>.Empty(pageSize);

    public async Task<ReviewDto> CreateReviewAsync(NewReviewInput input)
        => await Post<ReviewDto>("api/reviews", input)!;

    public async Task<ReviewDto> ModerateReviewAsync(string id, ReviewStatus status)
        => await _patch<ReviewDto>($"api/reviews/{id}/status", new { status = status.ToString() })!;

    public async Task DeleteReviewAsync(string id) => await SafeDelete($"api/reviews/{id}");

    /* -------------------------------- تیکت‌ها ----------------------------- */

    public async Task<Paged<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
        => await Get<Paged<TicketDto>>($"api/tickets{Q(("page", page), ("pageSize", pageSize), ("status", status), ("search", search))}")
           ?? Paged<TicketDto>.Empty(pageSize);

    public Task<TicketDetailDto?> GetTicketAsync(string id) => Get<TicketDetailDto?>($"api/tickets/{id}");

    public async Task<TicketDto> CreateTicketAsync(NewTicketInput input)
        => await Post<TicketDto>("api/tickets", input)!;

    public async Task<TicketDetailDto> AddTicketMessageAsync(string id, string body, TicketStatus? status = null)
        => await Post<TicketDetailDto>($"api/tickets/{id}/messages", new { body, close = status == TicketStatus.Closed })!;

    /* ------------------------------ احراز هویت ---------------------------- */

    public Task<UserDto?> LoginAsync(string email, string password)
        => Post<UserDto?>("api/auth/login", new { email, password });

    public Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country)
        => Post<UserDto?>("api/auth/register", new { fullName, email, password, company, country });

    public async Task<UserDto> UpdateProfileAsync(string fullName, string? company, string? phone, string? country)
        => await Put<UserDto>("api/account/profile", new { fullName, company, phone, country })!;

    public async Task<bool> ChangePasswordAsync(string current, string next)
    {
        ApplyUser();
        var res = await _http.PostAsync("api/account/password", Body(new { current, next }));
        return res.IsSuccessStatusCode;
    }

    /* ----------------------------- داشبوردها ------------------------------ */

    public async Task<AdminOverviewDto> GetAdminOverviewAsync()
        => await Get<AdminOverviewDto>("api/admin/overview") ?? FallbackOverview();

    public async Task<CustomerOverviewDto> GetCustomerOverviewAsync()
        => await Get<CustomerOverviewDto>("api/admin/customer-overview") ?? FallbackCustomerOverview();

    /* --------------------------- مدیریت: مشتریان -------------------------- */

    public async Task<Paged<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null)
        => await Get<Paged<CustomerDto>>($"api/admin/customers{Q(("page", page), ("pageSize", pageSize), ("search", search))}")
           ?? Paged<CustomerDto>.Empty(pageSize);

    public async Task<CustomerDto> UpdateCustomerAsync(string id, string fullName, string? company, Role role, bool locked)
        => await Put<CustomerDto>($"api/admin/customers/{id}", new { fullName, company, role, locked })!;

    /* --------------------------- مدیریت: تخفیف ---------------------------- */

    public async Task<IReadOnlyList<DiscountCodeDto>> GetDiscountCodesAsync()
        => await Get<IReadOnlyList<DiscountCodeDto>>("api/admin/discount-codes") ?? Array.Empty<DiscountCodeDto>();

    public async Task<DiscountCodeDto> UpsertDiscountAsync(DiscountCodeDto code)
        => await Post<DiscountCodeDto>("api/admin/discount-codes", code) ?? code;

    public async Task DeleteDiscountAsync(string id) => await SafeDelete($"api/admin/discount-codes/{id}");

    /* -------------------------- مدیریت: محصولات --------------------------- */

    public async Task<ProductDetailDto> UpsertProductAsync(ProductDetailDto product)
        => await Post<ProductDetailDto>("api/admin/products", product) ?? product;

    public async Task DeleteProductAsync(string id)
    {
        await SafeDelete($"api/admin/products/{id}");
        _cache.Invalidate("categories");   // شمارشِ دسته‌بندی‌ها عوض می‌شود
    }

    /* --------------------------- فرم‌های محتوایی -------------------------- */


    // ------------------------------- وبلاگ -------------------------------

    public async Task<Paged<ArticleDto>> GetArticlesAsync(int page = 1, int pageSize = 9, bool publishedOnly = true)
        => await Get<Paged<ArticleDto>>($"api/articles{Q(("page", page), ("pageSize", pageSize), ("publishedOnly", publishedOnly ? true : null))}")
           ?? new Paged<ArticleDto>(Array.Empty<ArticleDto>(), new PagedMeta(page, pageSize, 0, 0));

    public Task<ArticleDto?> GetArticleBySlugAsync(string slug)
        => Get<ArticleDto?>($"api/articles/{Uri.EscapeDataString(slug)}");

    public async Task<ArticleDto> UpsertArticleAsync(ArticleInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Id))
            return await Post<ArticleDto>("api/admin/articles", input) ?? FallbackArticle(input);
        return await Put<ArticleDto>($"api/admin/articles/{input.Id}", input) ?? FallbackArticle(input);
    }

    private static ArticleDto FallbackArticle(ArticleInput input)
        => new(input.Id ?? "a-" + Guid.NewGuid().ToString("N")[..8], input.Slug, input.Title,
               input.Excerpt, input.Body, input.Category, input.IsPublished,
               DateTime.UtcNow, input.ReadMinutes <= 0 ? 4 : input.ReadMinutes);

    public async Task DeleteArticleAsync(string id)
        => await SafeDelete($"api/admin/articles/{id}");

    // -------------------------- درخواست‌های دمو --------------------------

    public async Task<IReadOnlyList<DemoRequestDto>> GetDemoRequestsAsync(string? status = null)
        => await Get<IReadOnlyList<DemoRequestDto>>($"api/admin/demo-requests{Q(("status", status))}")
           ?? Array.Empty<DemoRequestDto>();

    public async Task UpdateDemoRequestStatusAsync(string id, DemoRequestStatus status)
        => await _patch<DemoRequestDto>($"api/admin/demo-requests/{id}/status", new { status = status.ToString() });

    public async Task SubmitContactAsync(string name, string email, string? company, string topic, string message)
        => await Send("api/contact", new { name, email, company, topic, message });

    public async Task SubmitDemoRequestAsync(string? productId, string name, string email, string? company, string? companySize, string? message)
        => await Send("api/demo-requests", new { productId, name, email, company, companySize, message });

    /* ----------------------------- اعلان‌ها ------------------------------- */

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync()
        => await Get<IReadOnlyList<NotificationDto>>("api/notifications") ?? Array.Empty<NotificationDto>();

    public async Task MarkNotificationReadAsync(string id)
    {
        ApplyUser();
        await _http.PostAsync($"api/notifications/{id}/read", null);
    }
}
