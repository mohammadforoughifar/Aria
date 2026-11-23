// مسیریابِ لایهٔ داده.
//
// صفحه‌ها فقط IStoreApi را می‌شناسند. این کلاس تصمیم می‌گیرد هر فراخوانی از
// کدام پشته پاسخ بگیرد:
//
//   ۱) اگر آن بخش به API نسخهٔ ۱ وصل شده باشد → V1Api (REST واقعی، مبلغ decimal،
//      پیامِ خطایِ فارسی از خودِ سرور).
//   ۲) اگر نه → همان HttpStoreApi قدیمی، بدونِ هیچ تغییری.
//
// چرا این‌طور؟ چون مهاجرت «صفحه‌به‌صفحه» است و هیچ لحظه‌ای نباید فروشگاه
// نیمه‌کاره بماند. هر موج، چند متد را از شاخهٔ ۲ به شاخهٔ ۱ منتقل می‌کند و
// بقیه دست‌نخورده کار می‌کنند. وقتی همه منتقل شد، کلاسِ قدیمی حذف می‌شود.
//
// موجِ ۱ (همین حالا): کاتالوگ، دسته‌ها، جزئیاتِ محصول، دیدگاه‌ها، سبدِ مهمان،
// تسویه‌حساب، محتوایِ عمومی (تنظیمات، نظراتِ مشتریان، پرسش‌ها، وبلاگ)، فرمِ
// تماس و درخواستِ دمو.

using Northwind.Client.Services.V1;
using Northwind.Shared.Contracts;

namespace Northwind.Client.Services;

public sealed class StoreApiRouter : IStoreApi
{
    private readonly IStoreApi _legacy;
    private readonly IV1Api _v1;

    public StoreApiRouter(IStoreApi legacy, IV1Api v1)
    {
        _legacy = legacy;
        _v1 = v1;
    }

    /* ============================== تنظیمات ============================== */

    public async Task<SiteSettingsDto> GetSettingsAsync()
    {
        var s = await _v1.GetAsync<V1Settings>("api/v1/settings");
        if (s is null) return await _legacy.GetSettingsAsync();
        return new SiteSettingsDto(
            s.SiteName, s.SupportEmail, s.SalesEmail, s.SupportPhone,
            s.HeroHeadline, s.HeroSubheading, s.TaxRateBps, s.TrialDays,
            s.MaintenanceMode, s.Announcement);
    }

    // نوشتنِ تنظیمات هنوز در پنلِ مدیریت رویِ پشتهٔ قدیمی است.
    public Task<SiteSettingsDto> UpdateSettingsAsync(SiteSettingsDto settings)
        => _legacy.UpdateSettingsAsync(settings);

    /* =============================== کاتالوگ ============================== */

    public async Task<Paged<ProductListItemDto>> GetProductsAsync(ProductQuery query)
    {
        var url = "api/v1/products" + V1Query.Of(
            ("page", query.Page),
            ("pageSize", query.PageSize),
            ("search", query.Search),
            ("sort", SortKey(query.SortBy)),
            ("category", query.Category),
            ("featured", query.Featured == true ? "true" : null));

        var page = await _v1.GetAsync<V1Paged<V1ProductListItem>>(url);
        if (page is null) return await _legacy.GetProductsAsync(query);

        return new Paged<ProductListItemDto>(
            page.Items.Select(ToListItem).ToList(),
            new PagedMeta(page.Meta.Page, page.Meta.PageSize, page.Meta.TotalCount, page.Meta.TotalPages));
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
    {
        var d = await _v1.GetAsync<V1ProductDetail>($"api/v1/products/slug/{Uri.EscapeDataString(slug)}");
        if (d is not null) return ToDetail(d);

        // پیوندهایِ قدیمی (مثلاً از پنلِ مدیریت) هنوز به اسلاگ‌هایِ پایگاه‌دادهٔ قبلی
        // اشاره می‌کنند؛ تا مهاجرتِ کامل، صفحهٔ محصول به‌جایِ «پیدا نشد» همان محصول
        // را از مسیرِ قدیمی نشان می‌دهد.
        return await _legacy.GetProductBySlugAsync(slug);
    }

    public async Task<ProductDetailDto?> GetProductByIdAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return await _legacy.GetProductByIdAsync(id);
        var d = await _v1.GetAsync<V1ProductDetail>($"api/v1/products/{guid}");
        return d is null ? await _legacy.GetProductByIdAsync(id) : ToDetail(d);
    }

    /// <summary>
    /// نسخهٔ ۱ مسیرِ دسته‌ای (batch) ندارد؛ جزئیاتِ چند محصول موازی گرفته
    /// می‌شود (سقفِ ۱۲ درخواستِ هم‌زمان) تا کارت‌ها یکی‌یکی پر نشوند.
    /// </summary>
    public async Task<IReadOnlyList<ProductDetailDto>> GetProductsByIdsAsync(IEnumerable<string> ids)
    {
        var guids = ids.Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)
                       .Where(g => g != Guid.Empty)
                       .Distinct()
                       .Take(24)
                       .ToList();

        if (guids.Count == 0) return Array.Empty<ProductDetailDto>();

        using var gate = new SemaphoreSlim(6);
        var tasks = guids.Select(async g =>
        {
            await gate.WaitAsync();
            try { return await _v1.GetAsync<V1ProductDetail>($"api/v1/products/{g}"); }
            finally { gate.Release(); }
        });

        var found = await Task.WhenAll(tasks);
        return found.Where(d => d is not null).Select(d => ToDetail(d!)).ToList();
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive = false)
    {
        var list = await _v1.GetAsync<List<V1Category>>("api/v1/categories");
        if (list is null) return await _legacy.GetCategoriesAsync(includeInactive);

        return list.Select(c => new CategoryDto(
            c.Id.ToString(), c.Name, c.Slug, c.Description, c.Icon, c.ProductCount)).ToList();
    }

    public Task<CategoryDto> UpsertCategoryAsync(CategoryDto category)
        => _legacy.UpsertCategoryAsync(category);

    public Task DeleteCategoryAsync(string id) => _legacy.DeleteCategoryAsync(id);

    public async Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync()
    {
        var list = await _v1.GetAsync<List<V1Testimonial>>("api/v1/testimonials");
        if (list is null) return await _legacy.GetTestimonialsAsync();

        return list.Select(t => new TestimonialDto(
            t.Id.ToString(), t.AuthorName, t.AuthorRole, t.Company, t.Quote, t.Rating)).ToList();
    }

    public async Task<IReadOnlyList<FaqDto>> GetFaqsAsync()
    {
        var list = await _v1.GetAsync<List<V1Faq>>("api/v1/faqs");
        if (list is null) return await _legacy.GetFaqsAsync();

        return list.Select(f => new FaqDto(f.Id.ToString(), f.Question, f.Answer)).ToList();
    }

    /* ================================ سبد ================================ */

    public async Task<CartDto> GetCartAsync(string sessionKey, string? discountCode = null)
    {
        _v1.SessionKey = sessionKey;

        // کدِ تخفیف در قالبِ کوئری به سبد داده می‌شود تا پیش‌نمایش همان لحظه بیاید.
        // کنترلرِ سبد، کلیدِ مهمان را از query می‌خواند (نه از هدر).
        var key = Uri.EscapeDataString(sessionKey);
        var cart = string.IsNullOrWhiteSpace(discountCode)
            ? await _v1.GetAsync<V1Cart>($"api/v1/cart?sessionKey={key}")
            : await _v1.SendAsync<V1Cart>("POST", "api/v1/cart/discount",
                new { code = discountCode, sessionKey });

        if (cart is null) return await _legacy.GetCartAsync(sessionKey, discountCode);
        return ToCart(cart);
    }

    public async Task<CartDto> AddToCartAsync(string sessionKey, string planId, int quantity = 1)
    {
        _v1.SessionKey = sessionKey;

        if (!Guid.TryParse(planId, out var plan))
            return await _legacy.AddToCartAsync(sessionKey, planId, quantity);

        var cart = await _v1.SendAsync<V1Cart>("POST", "api/v1/cart/items",
            new { planId = plan, quantity, sessionKey });
        return ToCart(cart);
    }

    public async Task<CartDto> UpdateCartItemAsync(string sessionKey, string itemId, int quantity)
    {
        _v1.SessionKey = sessionKey;

        if (!Guid.TryParse(itemId, out var id))
            return await _legacy.UpdateCartItemAsync(sessionKey, itemId, quantity);

        var key = Uri.EscapeDataString(sessionKey);
        var cart = await _v1.SendAsync<V1Cart>("PUT", $"api/v1/cart/items/{id}?sessionKey={key}", new { quantity });
        return ToCart(cart);
    }

    public async Task<CartDto> RemoveCartItemAsync(string sessionKey, string itemId)
    {
        _v1.SessionKey = sessionKey;

        if (!Guid.TryParse(itemId, out var id))
            return await _legacy.RemoveCartItemAsync(sessionKey, itemId);

        var key = Uri.EscapeDataString(sessionKey);
        var cart = await _v1.SendAsync<V1Cart>("DELETE", $"api/v1/cart/items/{id}?sessionKey={key}");
        return ToCart(cart);
    }

    public async Task<CartDto> ApplyDiscountAsync(string sessionKey, string code)
    {
        _v1.SessionKey = sessionKey;
        var cart = await _v1.SendAsync<V1Cart>("POST", "api/v1/cart/discount",
            new { code, sessionKey });
        return ToCart(cart);
    }

    /* ============================== سفارش‌ها ============================= */

    public async Task<OrderDto> CheckoutAsync(CheckoutInput input)
    {
        _v1.SessionKey = input.SessionKey;

        var result = await _v1.SendAsync<V1CheckoutResult>("POST", "api/v1/checkout", new
        {
            sessionKey = input.SessionKey,
            discountCode = input.DiscountCode,
            paymentMethod = input.PaymentMethod,
            cardName = input.CardName,
            cardNumber = input.CardNumber,
            billingName = input.BillingName,
            billingCompany = input.BillingCompany,
            billingEmail = input.BillingEmail,
            billingCountry = input.BillingCountry,
            vatId = input.VatId,
            notes = input.Notes,
            accountType = input.AccountType.ToString(),
            economicCode = input.EconomicCode,
            nationalId = input.NationalId,
            companyName = input.CompanyName,
        });

        if (!result.PaymentSucceeded)
            throw new V1ApiException(result.Message ?? "پرداخت انجام نشد.", "payment_failed", 409);

        return ToOrder(result.Order);
    }

    // بقیهٔ سفارش‌ها هنوز رویِ پشتهٔ قدیمی است.
    public Task<Paged<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
        => _legacy.GetOrdersAsync(page, pageSize, status, search);

    public Task<OrderDto?> GetOrderAsync(string id) => _legacy.GetOrderAsync(id);

    public Task<OrderDto> UpdateOrderStatusAsync(string id, OrderStatus status)
        => _legacy.UpdateOrderStatusAsync(id, status);

    /* ============================== فاکتورها ============================= */

    public Task<Paged<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 10)
        => _legacy.GetInvoicesAsync(page, pageSize);

    public Task<InvoiceDto?> GetInvoiceAsync(string id) => _legacy.GetInvoiceAsync(id);

    /* =============================== لایسنس ============================== */

    public Task<Paged<LicenseDto>> GetLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
        => _legacy.GetLicensesAsync(page, pageSize, status, search);

    public Task<LicenseDto> LicenseActionAsync(string id, string action, int? days = null, string? machineName = null)
        => _legacy.LicenseActionAsync(id, action, days, machineName);

    public Task<LicenseDto> GenerateLicenseAsync(string userId, string productId, string? planId, int seats, int? validDays)
        => _legacy.GenerateLicenseAsync(userId, productId, planId, seats, validDays);

    /* ============================== دانلودها ============================= */

    public Task<IReadOnlyList<AvailableDownloadDto>> GetAvailableDownloadsAsync()
        => _legacy.GetAvailableDownloadsAsync();

    public Task<IReadOnlyList<DownloadDto>> GetDownloadHistoryAsync()
        => _legacy.GetDownloadHistoryAsync();

    public Task<DownloadDto> RecordDownloadAsync(string productId, string? versionId, string kind, string fileName, int sizeMb)
        => _legacy.RecordDownloadAsync(productId, versionId, kind, fileName, sizeMb);

    /* =============================== نظرات ============================== */

    public async Task<Paged<ReviewDto>> GetReviewsAsync(int page = 1, int pageSize = 10, string? productId = null, ReviewStatus? status = null)
    {
        // دیدگاه‌هایِ یک محصول: مسیرِ عمومیِ نسخهٔ ۱.
        if (!string.IsNullOrWhiteSpace(productId) && Guid.TryParse(productId, out var pid))
        {
            var list = await _v1.GetAsync<List<V1Review>>($"api/v1/products/{pid}/reviews");
            if (list is not null)
            {
                var items = list.Select(ToReview).ToList();
                var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                var total = items.Count;
                return new Paged<ReviewDto>(slice,
                    new PagedMeta(page, pageSize, total, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))));
            }
        }

        // فهرستِ کامل/مدیریتی: پشتهٔ قدیمی.
        return await _legacy.GetReviewsAsync(page, pageSize, productId, status);
    }

    public Task<ReviewDto> CreateReviewAsync(NewReviewInput input) => _legacy.CreateReviewAsync(input);
    public Task<ReviewDto> ModerateReviewAsync(string id, ReviewStatus status) => _legacy.ModerateReviewAsync(id, status);
    public Task DeleteReviewAsync(string id) => _legacy.DeleteReviewAsync(id);

    /* =============================== تیکت‌ها ============================= */

    public Task<Paged<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
        => _legacy.GetTicketsAsync(page, pageSize, status, search);

    public Task<TicketDetailDto?> GetTicketAsync(string id) => _legacy.GetTicketAsync(id);

    public Task<TicketDto> CreateTicketAsync(NewTicketInput input) => _legacy.CreateTicketAsync(input);

    public Task<TicketDetailDto> AddTicketMessageAsync(string id, string body, TicketStatus? status = null)
        => _legacy.AddTicketMessageAsync(id, body, status);

    /* ================================ احراز ============================== */

    // احرازِ هویت در موجِ ۲ به توکنِ JWT منتقل می‌شود؛ تا آن‌موقع پشتهٔ قدیمی.
    public Task<UserDto?> LoginAsync(string email, string password) => _legacy.LoginAsync(email, password);

    public Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country)
        => _legacy.RegisterAsync(fullName, email, password, company, country);

    public Task<UserDto> UpdateProfileAsync(string fullName, string? company, string? phone, string? country)
        => _legacy.UpdateProfileAsync(fullName, company, phone, country);

    public Task<bool> ChangePasswordAsync(string current, string next) => _legacy.ChangePasswordAsync(current, next);

    /* ============================== داشبورد ============================== */

    public Task<AdminOverviewDto> GetAdminOverviewAsync() => _legacy.GetAdminOverviewAsync();
    public Task<CustomerOverviewDto> GetCustomerOverviewAsync() => _legacy.GetCustomerOverviewAsync();

    public Task<Paged<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null)
        => _legacy.GetCustomersAsync(page, pageSize, search);

    public Task<CustomerDto> UpdateCustomerAsync(string id, string fullName, string? company, Role role, bool locked)
        => _legacy.UpdateCustomerAsync(id, fullName, company, role, locked);

    /* ============================== تخفیف‌ها ============================= */

    public Task<IReadOnlyList<DiscountCodeDto>> GetDiscountCodesAsync() => _legacy.GetDiscountCodesAsync();
    public Task<DiscountCodeDto> UpsertDiscountAsync(DiscountCodeDto code) => _legacy.UpsertDiscountAsync(code);
    public Task DeleteDiscountAsync(string id) => _legacy.DeleteDiscountAsync(id);

    /* =========================== کاتالوگ (مدیر) ========================= */

    public Task<ProductDetailDto> UpsertProductAsync(ProductDetailDto product) => _legacy.UpsertProductAsync(product);
    public Task DeleteProductAsync(string id) => _legacy.DeleteProductAsync(id);

    /* ============================== اعلان‌ها ============================= */

    public Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync() => _legacy.GetNotificationsAsync();
    public Task MarkNotificationReadAsync(string id) => _legacy.MarkNotificationReadAsync(id);

    /* =============================== محتوا ============================== */

    public async Task<Paged<ArticleDto>> GetArticlesAsync(int page = 1, int pageSize = 9, bool publishedOnly = true)
    {
        var url = "api/v1/articles" + V1Query.Of(("page", page), ("pageSize", pageSize));
        var result = await _v1.GetAsync<V1Paged<V1Article>>(url);
        if (result is null) return await _legacy.GetArticlesAsync(page, pageSize, publishedOnly);

        return new Paged<ArticleDto>(
            result.Items.Select(ToArticle).ToList(),
            new PagedMeta(result.Meta.Page, result.Meta.PageSize, result.Meta.TotalCount, result.Meta.TotalPages));
    }

    public async Task<ArticleDto?> GetArticleBySlugAsync(string slug)
    {
        var a = await _v1.GetAsync<V1Article>($"api/v1/articles/{Uri.EscapeDataString(slug)}");
        return a is null ? null : ToArticle(a);
    }

    public Task<ArticleDto> UpsertArticleAsync(ArticleInput input) => _legacy.UpsertArticleAsync(input);
    public Task DeleteArticleAsync(string id) => _legacy.DeleteArticleAsync(id);

    public Task<IReadOnlyList<DemoRequestDto>> GetDemoRequestsAsync(string? status = null)
        => _legacy.GetDemoRequestsAsync(status);

    public Task UpdateDemoRequestStatusAsync(string id, DemoRequestStatus status)
        => _legacy.UpdateDemoRequestStatusAsync(id, status);

    /* ============================ فرم‌هایِ عمومی ========================== */

    public Task SubmitContactAsync(string name, string email, string? company, string topic, string message)
        => _v1.SendAsync("POST", "api/v1/contact", new { name, email, company, topic, message });

    public Task SubmitDemoRequestAsync(string? productId, string name, string email, string? company, string? companySize, string? message)
        => _v1.SendAsync("POST", "api/v1/demo-requests", new
        {
            productId = Guid.TryParse(productId, out var pid) ? pid : (Guid?)null,
            name, email, company, companySize, message
        });

    /* ============================== نگاشت‌ها ============================== */

    private static string? SortKey(string? clientSort) => clientSort switch
    {
        "price-asc" => "price_asc",
        "price-desc" => "price_desc",
        "popular" => "popular",
        "newest" => "newest",
        "oldest" => "oldest",
        "name" => "name",
        "rating" => "popular",   // نسخهٔ ۱ مرتب‌سازی بر پایهٔ امتیاز ندارد
        _ => null,
    };

    private static ProductListItemDto ToListItem(V1ProductListItem p) => new(
        p.Id.ToString(), p.Slug, p.Name, p.Tagline, p.Type, p.Platform, p.Version,
        p.BasePriceToman, p.RatingAvg, p.RatingCount, p.Popularity, p.HeroImage, p.AccentColor,
        p.IsFeatured, p.IsActive, p.CreatedAt, p.CategoryName, p.CategorySlug, p.PlanCount);

    private static ProductDetailDto ToDetail(V1ProductDetail d) => new(
        d.Id.ToString(), d.Slug, d.Name, d.Tagline, d.Description, d.Type, d.Platform, d.Version,
        d.BasePriceToman, d.RatingAvg, d.RatingCount, d.Popularity, d.AccentColor, d.HeroImage,
        d.IsActive, d.IsFeatured, d.TrialDays, d.CreatedAt,
        d.CategoryId.ToString(), d.CategoryName, d.CategorySlug,
        d.Features, d.SystemRequirements, d.Gallery, d.DemoVideoUrl,
        d.Plans.Select(ToPlan).ToList(),
        d.Versions.Select(v => new ProductVersionDto(
            v.Id.ToString(), v.Version, v.ReleasedAt, v.Notes, v.InstallerUrl, v.SizeMb, v.Platform)).ToList(),
        d.Reviews.Select(ToReview).ToList());

    private static PlanDto ToPlan(V1Plan p) => new(
        p.Id.ToString(), p.Name, p.Tier, p.BillingType, p.PriceToman, p.Seats,
        p.Description, p.IsPopular, p.LicenseMonths);

    private static ReviewDto ToReview(V1Review r) => new(
        r.Id.ToString(), r.ProductId.ToString(), r.ProductName, r.AuthorName, r.AuthorCompany,
        r.Rating, r.Title, r.Body, r.Status, r.CreatedAt);

    private static ArticleDto ToArticle(V1Article a) => new(
        a.Id.ToString(), a.Slug, a.Title, a.Excerpt, a.Body, a.Category,
        a.IsPublished, a.PublishedAt, a.ReadMinutes);

    private static CartDto ToCart(V1Cart c) => new(
        c.Id.ToString(),
        c.Items.Select(i => new CartItemDto(
            i.Id.ToString(), i.PlanId.ToString(), i.Quantity, i.ProductId.ToString(),
            i.ProductName, i.ProductSlug, i.PlanName, i.Tier, i.BillingType,
            i.AccentColor, i.UnitPriceToman, i.LineTotalToman)).ToList(),
        c.ItemCount, c.SubtotalToman, c.DiscountToman, c.TaxToman, c.TotalToman,
        c.DiscountCode, c.Currency);

    private static OrderDto ToOrder(V1Order o) => new(
        o.Id.ToString(), o.OrderNumber, o.UserId.ToString(), o.CustomerName, o.CustomerEmail,
        o.CustomerCompany, o.Status, o.SubtotalToman, o.DiscountToman, o.TaxToman, o.TotalToman,
        o.Currency, o.DiscountCode, o.PlacedAt, o.BillingName, o.BillingCompany, o.BillingEmail,
        o.BillingCountry, o.Notes,
        o.Items.Select(i => new OrderItemDto(
            i.Id.ToString(), i.ProductId.ToString(), i.PlanId.ToString(), i.ProductName,
            i.PlanName, i.BillingType, i.UnitPriceToman, i.Quantity, i.LineTotalToman)).ToList(),
        o.Payment is null ? null : new PaymentDto(
            o.Payment.Id.ToString(), o.Payment.OrderId.ToString(), o.Payment.Provider,
            o.Payment.Method, o.Payment.AmountToman, o.Payment.Status,
            o.Payment.TransactionRef, o.Payment.PaidAt),
        o.Invoice is null ? null : new InvoiceDto(
            o.Invoice.Id.ToString(), o.Invoice.InvoiceNumber, o.Invoice.OrderId.ToString(),
            o.Invoice.Status, o.Invoice.IssuedAt, o.Invoice.DueAt,
            o.Invoice.TotalToman, o.Invoice.TaxToman, null));
}
