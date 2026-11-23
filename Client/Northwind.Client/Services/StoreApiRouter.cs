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
        return ToSettings(s);
    }

    public async Task<SiteSettingsDto> UpdateSettingsAsync(SiteSettingsDto settings)
    {
        var saved = await _v1.SendAsync<V1Settings>("PUT", "api/v1/admin/settings", new
        {
            siteName = settings.SiteName,
            supportEmail = settings.SupportEmail,
            salesEmail = settings.SalesEmail,
            supportPhone = settings.SupportPhone,
            heroHeadline = settings.HeroHeadline,
            heroSubheading = settings.HeroSubheading,
            taxRateBps = settings.TaxRateBps,
            trialDays = settings.TrialDays,
            maintenanceMode = settings.MaintenanceMode,
            announcement = settings.Announcement
        });
        return ToSettings(saved);
    }

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

    public async Task<CategoryDto> UpsertCategoryAsync(CategoryDto category)
    {
        var input = new { name = category.Name, slug = category.Slug, description = category.Description, icon = category.Icon };

        var saved = Guid.TryParse(category.Id, out var gid) && gid != Guid.Empty
            ? await _v1.SendAsync<V1Category>("PUT", $"api/v1/categories/{gid}", input)
            : await _v1.SendAsync<V1Category>("POST", "api/v1/categories", input);

        return ToCategory(saved);
    }

    public Task DeleteCategoryAsync(string id)
        => _v1.SendAsync("DELETE", $"api/v1/categories/{RequireGuid(id, "دسته")}");

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

    public async Task<Paged<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
    {
        var url = "api/v1/orders" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("search", search), ("status", StatusFilter(status)));

        var result = await _v1.GetAsync<V1Paged<V1Order>>(url);
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetOrdersAsync(page, pageSize, status, search);
            MarkUnauthorized("سفارش‌ها");
            return Paged<OrderDto>.Empty(pageSize);
        }

        return new Paged<OrderDto>(result.Items.Select(ToOrder).ToList(), Meta(result.Meta));
    }

    public async Task<OrderDto?> GetOrderAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return await _legacy.GetOrderAsync(id);
        var order = await _v1.GetAsync<V1Order>($"api/v1/orders/{guid}");
        return order is null ? null : ToOrder(order);
    }

    /// <summary>سفارش‌هایِ همهٔ کاربران — فقط مدیر.</summary>
    public async Task<Paged<OrderDto>> GetAdminOrdersAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
    {
        var url = "api/v1/admin/orders" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", StatusFilter(status)), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1Order>>(url);
        if (result is null)
        {
            MarkUnauthorized("سفارش‌ها");
            return Paged<OrderDto>.Empty(pageSize);
        }

        return new Paged<OrderDto>(result.Items.Select(ToOrder).ToList(), Meta(result.Meta));
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(string id, OrderStatus status)
    {
        var order = await _v1.SendAsync<V1Order>("PATCH",
            $"api/v1/admin/orders/{RequireGuid(id, "سفارش")}/status",
            new { status = status.ToString() });

        return ToOrder(order);
    }

    /* ============================== فاکتورها ============================= */

    public async Task<Paged<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 10)
    {
        var url = "api/v1/invoices" + V1Query.Of(("page", page), ("pageSize", pageSize));
        var result = await _v1.GetAsync<V1Paged<V1Invoice>>(url);
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetInvoicesAsync(page, pageSize);
            MarkUnauthorized("فاکتورها");
            return Paged<InvoiceDto>.Empty(pageSize);
        }

        return new Paged<InvoiceDto>(result.Items.Select(ToInvoice).ToList(), Meta(result.Meta));
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return await _legacy.GetInvoiceAsync(id);
        var invoice = await _v1.GetAsync<V1Invoice>($"api/v1/invoices/{guid}");
        return invoice is null ? null : ToInvoice(invoice);
    }

    /* =============================== لایسنس ============================== */

    public async Task<Paged<LicenseDto>> GetLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
    {
        var url = "api/v1/licenses" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", StatusFilter(status)), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1License>>(url);
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetLicensesAsync(page, pageSize, status, search);
            MarkUnauthorized("لایسنس‌ها");
            return Paged<LicenseDto>.Empty(pageSize);
        }

        return new Paged<LicenseDto>(result.Items.Select(ToLicense).ToList(), Meta(result.Meta));
    }

    /// <summary>
    /// عملیاتِ لایسنس: <c>activate</c> / <c>renew</c> / <c>revoke</c>.
    /// صفحه با نام‌هایِ قدیمی صدا می‌زند (<c>extend</c>)، نسخهٔ ۱ با
    /// <c>renew</c> کار می‌کند؛ ترجمه همین‌جا انجام می‌شود.
    /// </summary>
    public async Task<LicenseDto> LicenseActionAsync(string id, string action, int? days = null, string? machineName = null)
    {
        if (!Guid.TryParse(id, out var guid))
            return await _legacy.LicenseActionAsync(id, action, days, machineName);

        var mapped = action switch
        {
            "extend" => "renew",
            "activate" => "activate",
            _ => "revoke",
        };

        var license = await _v1.SendAsync<V1License>("POST", $"api/v1/licenses/{guid}/action",
            new { action = mapped, days, machineName });
        return ToLicense(license);
    }

    /// <summary>لایسنس‌هایِ همهٔ کاربران — فقط مدیر.</summary>
    public async Task<Paged<LicenseDto>> GetAdminLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
    {
        var url = "api/v1/admin/licenses" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", StatusFilter(status)), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1License>>(url);
        if (result is null)
        {
            MarkUnauthorized("لایسنس‌ها");
            return Paged<LicenseDto>.Empty(pageSize);
        }

        return new Paged<LicenseDto>(result.Items.Select(ToLicense).ToList(), Meta(result.Meta));
    }

    /// <summary>
    /// صدورِ لایسنس از سویِ پشتیبانی. رابط با «روز» کار می‌کند و نسخهٔ ۱ با
    /// «ماه»؛ تقسیم به بالا انجام می‌شود تا ۳۶۵ روز به ۱۳ ماه تبدیل نشود.
    /// </summary>
    public async Task<LicenseDto> GenerateLicenseAsync(string userId, string productId, string? planId, int seats, int? validDays)
    {
        var months = validDays is > 0 ? Math.Max(1, (int)Math.Ceiling(validDays.Value / 30d)) : 12;

        var license = await _v1.SendAsync<V1License>("POST", "api/v1/admin/licenses", new
        {
            userId = RequireGuid(userId, "کاربر"),
            productId = RequireGuid(productId, "محصول"),
            planId = Guid.TryParse(planId, out var pl) ? pl : (Guid?)null,
            seats = Math.Max(1, seats),
            months
        });

        return ToLicense(license);
    }

    /* ============================== دانلودها ============================= */

    public async Task<IReadOnlyList<AvailableDownloadDto>> GetAvailableDownloadsAsync()
    {
        var list = await _v1.GetAsync<List<V1AvailableDownload>>("api/v1/downloads/available");
        if (list is null) return await _legacy.GetAvailableDownloadsAsync();

        return list.Select(d => new AvailableDownloadDto(
            d.ProductId.ToString(), d.ProductName, d.ProductSlug, d.AccentColor, d.Licensed,
            d.Versions.Select(v => new AvailableVersionDto(
                v.Id.ToString(), v.Version, v.ReleasedAt, v.Platform, v.SizeMb,
                v.FileName, v.InstallerUrl, v.Notes)).ToList())).ToList();
    }

    public async Task<IReadOnlyList<DownloadDto>> GetDownloadHistoryAsync()
    {
        var result = await _v1.GetAsync<V1Paged<V1Download>>("api/v1/downloads?pageSize=50");
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetDownloadHistoryAsync();
            MarkUnauthorized("تاریخچهٔ دانلودها");
            return Array.Empty<DownloadDto>();
        }
        return result.Items.Select(ToDownload).ToList();
    }

    public async Task<DownloadDto> RecordDownloadAsync(string productId, string? versionId, string kind, string fileName, int sizeMb)
    {
        if (!Guid.TryParse(productId, out var pid))
            return await _legacy.RecordDownloadAsync(productId, versionId, kind, fileName, sizeMb);

        var download = await _v1.SendAsync<V1Download>("POST", "api/v1/downloads", new
        {
            productId = pid,
            versionId = Guid.TryParse(versionId, out var vid) ? vid : (Guid?)null,
            kind, fileName, sizeMb
        });
        return ToDownload(download);
    }

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

    public async Task<ReviewDto> ModerateReviewAsync(string id, ReviewStatus status)
    {
        var review = await _v1.SendAsync<V1Review>("PATCH",
            $"api/v1/admin/reviews/{RequireGuid(id, "دیدگاه")}/status",
            new { status = status.ToString() });

        return ToReview(review);
    }

    public Task DeleteReviewAsync(string id)
        => _v1.SendAsync("DELETE", $"api/v1/admin/reviews/{RequireGuid(id, "دیدگاه")}");

    /// <summary>فهرستِ مدیریتیِ دیدگاه‌ها — همهٔ کاربران، همهٔ وضعیت‌ها.</summary>
    public async Task<Paged<ReviewDto>> GetAdminReviewsAsync(int page = 1, int pageSize = 10, ReviewStatus? status = null)
    {
        var url = "api/v1/admin/reviews" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", status?.ToString()));

        var result = await _v1.GetAsync<V1Paged<V1Review>>(url);
        if (result is null)
        {
            MarkUnauthorized("دیدگاه‌ها");
            return Paged<ReviewDto>.Empty(pageSize);
        }

        return new Paged<ReviewDto>(result.Items.Select(ToReview).ToList(), Meta(result.Meta));
    }

    /* =============================== تیکت‌ها ============================= */

    public async Task<Paged<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
    {
        var url = "api/v1/tickets" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", StatusFilter(status)), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1Ticket>>(url);
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetTicketsAsync(page, pageSize, status, search);
            MarkUnauthorized("تیکت‌ها");
            return Paged<TicketDto>.Empty(pageSize);
        }

        return new Paged<TicketDto>(result.Items.Select(ToTicket).ToList(), Meta(result.Meta));
    }

    public async Task<TicketDetailDto?> GetTicketAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return await _legacy.GetTicketAsync(id);
        var ticket = await _v1.GetAsync<V1TicketDetail>($"api/v1/tickets/{guid}");
        return ticket is null ? null : ToTicketDetail(ticket);
    }

    public async Task<TicketDto> CreateTicketAsync(NewTicketInput input)
    {
        var detail = await _v1.SendAsync<V1TicketDetail>("POST", "api/v1/tickets", new
        {
            productId = Guid.TryParse(input.ProductId, out var pid) ? pid : (Guid?)null,
            subject = input.Subject,
            category = input.Category,
            priority = input.Priority.ToString(),
            environment = input.Environment,
            message = input.Message
        });
        return ToTicketSummary(detail);
    }

    /// <summary>تیکت‌هایِ همهٔ کاربران — فقط مدیر.</summary>
    public async Task<Paged<TicketDto>> GetAdminTicketsAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
    {
        var url = "api/v1/admin/tickets" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("status", status), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1Ticket>>(url);
        if (result is null)
        {
            MarkUnauthorized("تیکت‌ها");
            return Paged<TicketDto>.Empty(pageSize);
        }

        return new Paged<TicketDto>(result.Items.Select(ToTicket).ToList(), Meta(result.Meta));
    }

    /// <summary>
    /// تغییرِ وضعیتِ تیکت از سویِ پشتیبانی، با پاسخِ اختیاری در همان درخواست.
    /// </summary>
    public async Task<TicketDetailDto> UpdateAdminTicketStatusAsync(string id, TicketStatus status, string? reply = null)
    {
        var detail = await _v1.SendAsync<V1TicketDetail>("PATCH",
            $"api/v1/admin/tickets/{RequireGuid(id, "تیکت")}/status",
            new { status = status.ToString(), reply });

        return ToTicketDetail(detail);
    }

    public async Task<TicketDetailDto> AddTicketMessageAsync(string id, string body, TicketStatus? status = null)
    {
        if (!Guid.TryParse(id, out var guid))
            return await _legacy.AddTicketMessageAsync(id, body, status);

        var detail = await _v1.SendAsync<V1TicketDetail>("POST", $"api/v1/tickets/{guid}/messages",
            new { body });
        return ToTicketDetail(detail);
    }

    /* ================================ احراز ============================== */

    // ورود و ثبت‌نام از این موج به بعد در AuthState و رویِ نسخهٔ ۱ انجام می‌شود؛
    // این دو متد فقط برایِ سازگاری مانده‌اند.
    public Task<UserDto?> LoginAsync(string email, string password) => _legacy.LoginAsync(email, password);

    public Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country)
        => _legacy.RegisterAsync(fullName, email, password, company, country);

    public async Task<UserDto> UpdateProfileAsync(string fullName, string? company, string? phone, string? country)
    {
        var user = await _v1.SendAsync<V1User>("PUT", "api/v1/account/me",
            new { fullName, company, phone, country });
        return ToUser(user);
    }

    /// <summary>تغییرِ رمز؛ اگر رمزِ فعلی غلط باشد پیامِ سرور برمی‌گردد.</summary>
    public async Task<bool> ChangePasswordAsync(string current, string next)
    {
        try
        {
            await _v1.SendAsync("POST", "api/v1/account/password",
                new { currentPassword = current, newPassword = next });
            return true;
        }
        catch (V1ApiException ex)
        {
            LastError = ex.Message;
            return false;
        }
    }

    private void ClearError() => LastError = null;

    /// <summary>
    /// آیا اجازه داریم بعد از شکستِ نسخهٔ ۱ سراغِ API قدیمی برویم؟
    /// فقط وقتی نشستی وجود ندارد. با توکن، بازگشت به قدیمی یعنی نشان‌دادنِ دادهٔ
    /// کاربرِ دیگر با ۲۰۰ — که یک نشتیِ واقعی بود؛ دیگر هرگز.
    /// </summary>
    private bool CanFallBack => string.IsNullOrWhiteSpace(_v1.AccessToken);

    /// <summary>
    /// زبانهٔ «همه» در رابط یعنی «بی‌فیلتر». نسخهٔ ۱ مقدار را به enum تبدیل می‌کند و
    /// «all» را نمی‌شناسد (۴۰۰ می‌داد)؛ پس همان‌جا به null تبدیل می‌شود.
    /// </summary>
    private static string? StatusFilter(string? status)
        => string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase)
            ? null
            : status;

    /// <summary>خطایِ مشترکِ «نشست داریم ولی نسخهٔ ۱ جواب نداد».</summary>
    private void MarkUnauthorized(string what)
        => LastError = _v1.LastStatus is 401 or 403
            ? "نشستِ شما معتبر نیست؛ لطفاً دوباره وارد شوید."
            : $"دریافتِ {what} از سرور ممکن نشد.";

    /* ============================== داشبورد ============================== */

    public async Task<AdminOverviewDto> GetAdminOverviewAsync()
    {
        var o = await _v1.GetAsync<V1AdminOverview>("api/v1/admin/overview");
        if (o is null)
        {
            MarkUnauthorized("نمایِ کلیِ مدیریت");
            return new AdminOverviewDto(
                0, 0, 0, 0, 0, 0, 0, 0,
                Array.Empty<RevenuePointDto>(), Array.Empty<TopProductDto>(),
                Array.Empty<OrderDto>(), Array.Empty<TicketDto>());
        }

        return new AdminOverviewDto(
            o.TotalProducts, o.TotalCustomers, o.OpenTickets, o.PendingReviews,
            o.ActiveLicenses, o.ExpiringLicenses,
            o.RevenueThisMonthToman, o.RevenueLastMonthToman,
            o.RevenueSeries.Select(r => new RevenuePointDto(r.Label, r.TotalToman)).ToList(),
            o.TopProducts.Select(t => new TopProductDto(
                t.ProductId.ToString(), t.Name, t.Slug, t.UnitsSold, t.RevenueToman, t.AccentColor)).ToList(),
            o.RecentOrders.Select(ToOrder).ToList(),
            o.RecentTickets.Select(ToTicket).ToList());
    }
    public async Task<CustomerOverviewDto> GetCustomerOverviewAsync()
    {
        var o = await _v1.GetAsync<V1CustomerOverview>("api/v1/account/overview");
        if (o is null)
        {
            if (CanFallBack) return await _legacy.GetCustomerOverviewAsync();
            MarkUnauthorized("نمایِ کلیِ حساب");
            return new CustomerOverviewDto(
                0, 0, 0, 0, 0, 0, 0,
                Array.Empty<OrderDto>(), Array.Empty<LicenseDto>(), Array.Empty<RevenuePointDto>());
        }

        return new CustomerOverviewDto(
            o.OrderCount, o.ActiveLicenses, o.ExpiringLicenses, o.OpenTickets,
            o.TotalSpentToman, o.AvailableDownloads, o.OutstandingInvoicesToman,
            o.RecentOrders.Select(ToOrder).ToList(),
            o.ExpiringLicensesList.Select(ToLicense).ToList(),
            o.SpendSeries.Select(p => new RevenuePointDto(p.Label, p.TotalToman)).ToList());
    }

    public async Task<Paged<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        var url = "api/v1/admin/customers" + V1Query.Of(
            ("page", page), ("pageSize", pageSize), ("search", search));

        var result = await _v1.GetAsync<V1Paged<V1Customer>>(url);
        if (result is null)
        {
            MarkUnauthorized("کاربران");
            return Paged<CustomerDto>.Empty(pageSize);
        }

        return new Paged<CustomerDto>(result.Items.Select(ToCustomer).ToList(), Meta(result.Meta));
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string id, string fullName, string? company, Role role, bool locked)
    {
        var user = await _v1.SendAsync<V1Customer>("PUT",
            $"api/v1/admin/customers/{RequireGuid(id, "کاربر")}",
            new { role = role.ToString(), locked, fullName, company });

        return ToCustomer(user);
    }

    /* ============================== تخفیف‌ها ============================= */

    /// <summary>نسخهٔ ۱ فهرست را صفحه‌بندی می‌کند؛ پنل همه را یک‌جا می‌خواهد.</summary>
    public async Task<IReadOnlyList<DiscountCodeDto>> GetDiscountCodesAsync()
    {
        var result = await _v1.GetAsync<V1Paged<V1Discount>>("api/v1/admin/discounts?pageSize=100");
        if (result is null)
        {
            MarkUnauthorized("کدهایِ تخفیف");
            return Array.Empty<DiscountCodeDto>();
        }

        return result.Items.Select(ToDiscount).ToList();
    }

    public async Task<DiscountCodeDto> UpsertDiscountAsync(DiscountCodeDto code)
    {
        var input = new
        {
            code = code.Code,
            description = code.Description,
            percentOff = code.PercentOff,
            maxRedemptions = code.MaxRedemptions,
            expiresAt = code.ExpiresAt,
            isActive = code.IsActive
        };

        var saved = Guid.TryParse(code.Id, out var gid) && gid != Guid.Empty
            ? await _v1.SendAsync<V1Discount>("PUT", $"api/v1/admin/discounts/{gid}", input)
            : await _v1.SendAsync<V1Discount>("POST", "api/v1/admin/discounts", input);

        return ToDiscount(saved);
    }

    public Task DeleteDiscountAsync(string id)
        => _v1.SendAsync("DELETE", $"api/v1/admin/discounts/{RequireGuid(id, "کدِ تخفیف")}");

    /* =========================== کاتالوگ (مدیر) ========================= */

    public async Task<ProductDetailDto> UpsertProductAsync(ProductDetailDto product)
    {
        var input = new
        {
            name = product.Name,
            slug = product.Slug,
            tagline = product.Tagline,
            description = product.Description,
            type = product.Type.ToString(),
            platform = product.Platform,
            version = product.Version,
            basePriceToman = product.BasePriceToman,
            accentColor = product.AccentColor,
            heroImage = product.HeroImage,
            isFeatured = product.IsFeatured,
            isActive = product.IsActive,
            trialDays = product.TrialDays,
            popularity = product.Popularity,
            features = product.Features,
            systemRequirements = product.SystemRequirements,
            gallery = product.Gallery,
            demoVideoUrl = product.DemoVideoUrl,
            categoryId = RequireGuid(product.CategoryId, "دسته")
        };

        var saved = Guid.TryParse(product.Id, out var gid) && gid != Guid.Empty
            ? await _v1.SendAsync<V1ProductDetail>("PUT", $"api/v1/products/{gid}", input)
            : await _v1.SendAsync<V1ProductDetail>("POST", "api/v1/products", input);

        return ToDetail(saved);
    }

    public Task DeleteProductAsync(string id)
        => _v1.SendAsync("DELETE", $"api/v1/products/{RequireGuid(id, "محصول")}");

    /* ============================== اعلان‌ها ============================= */

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync()
    {
        var result = await _v1.GetAsync<V1Paged<V1Notification>>("api/v1/notifications?pageSize=50");
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetNotificationsAsync();
            MarkUnauthorized("اعلان‌ها");
            return Array.Empty<NotificationDto>();
        }

        return result.Items.Select(n => new NotificationDto(
            n.Id.ToString(), n.UserId.ToString(), n.Title, n.Body,
            n.Kind, n.IsRead, n.CreatedAt, n.Link)).ToList();
    }

    public async Task MarkNotificationReadAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid))
        {
            await _legacy.MarkNotificationReadAsync(id);
            return;
        }
        await _v1.SendAsync("POST", $"api/v1/notifications/{guid}/read");
    }

    /* =============================== محتوا ============================== */

    public async Task<Paged<ArticleDto>> GetArticlesAsync(int page = 1, int pageSize = 9, bool publishedOnly = true)
    {
        // پنلِ مدیریت پیش‌نویس‌ها را هم می‌خواهد؛ مسیرِ مدیریتی جداگانه است.
        var url = (publishedOnly ? "api/v1/articles" : "api/v1/admin/articles")
                + V1Query.Of(("page", page), ("pageSize", pageSize));
        var result = await _v1.GetAsync<V1Paged<V1Article>>(url);
        if (result is null)
        {
            if (CanFallBack) return await _legacy.GetArticlesAsync(page, pageSize, publishedOnly);
            MarkUnauthorized("مقاله‌ها");
            return Paged<ArticleDto>.Empty(pageSize);
        }

        return new Paged<ArticleDto>(
            result.Items.Select(ToArticle).ToList(),
            new PagedMeta(result.Meta.Page, result.Meta.PageSize, result.Meta.TotalCount, result.Meta.TotalPages));
    }

    public async Task<ArticleDto?> GetArticleBySlugAsync(string slug)
    {
        var a = await _v1.GetAsync<V1Article>($"api/v1/articles/{Uri.EscapeDataString(slug)}");
        return a is null ? null : ToArticle(a);
    }

    public async Task<ArticleDto> UpsertArticleAsync(ArticleInput input)
    {
        var body = new
        {
            title = input.Title,
            slug = input.Slug,
            excerpt = input.Excerpt,
            body = input.Body,
            category = input.Category,
            isPublished = input.IsPublished,
            readMinutes = input.ReadMinutes
        };

        var saved = Guid.TryParse(input.Id, out var gid) && gid != Guid.Empty
            ? await _v1.SendAsync<V1Article>("PUT", $"api/v1/admin/articles/{gid}", body)
            : await _v1.SendAsync<V1Article>("POST", "api/v1/admin/articles", body);

        return ToArticle(saved);
    }

    public Task DeleteArticleAsync(string id)
        => _v1.SendAsync("DELETE", $"api/v1/admin/articles/{RequireGuid(id, "مقاله")}");

    public async Task<IReadOnlyList<DemoRequestDto>> GetDemoRequestsAsync(string? status = null)
    {
        var url = "api/v1/admin/demo-requests" + V1Query.Of(("pageSize", 100), ("status", StatusFilter(status)));
        var result = await _v1.GetAsync<V1Paged<V1DemoRequest>>(url);
        if (result is null)
        {
            MarkUnauthorized("درخواست‌هایِ دمو");
            return Array.Empty<DemoRequestDto>();
        }

        return result.Items.Select(d => new DemoRequestDto(
            d.Id.ToString(), null, null, d.Name, d.Email, d.Company,
            null, null, null, d.Status, d.CreatedAt)).ToList();
    }

    public Task UpdateDemoRequestStatusAsync(string id, DemoRequestStatus status)
        => _v1.SendAsync("PATCH", $"api/v1/admin/demo-requests/{RequireGuid(id, "درخواستِ دمو")}/status",
            new { status = status.ToString() });

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

    /// <summary>شناسهٔ متنیِ رابط را به Guid تبدیل می‌کند؛ پیامِ فارسی می‌دهد.</summary>
    private static Guid RequireGuid(string? id, string what)
        => Guid.TryParse(id, out var g) && g != Guid.Empty
            ? g
            : throw new InvalidOperationException($"شناسهٔ {what} معتبر نیست.");

    private static SiteSettingsDto ToSettings(V1Settings s) => new(
        s.SiteName, s.SupportEmail, s.SalesEmail, s.SupportPhone,
        s.HeroHeadline, s.HeroSubheading, s.TaxRateBps, s.TrialDays,
        s.MaintenanceMode, s.Announcement);

    private static CategoryDto ToCategory(V1Category c) => new(
        c.Id.ToString(), c.Name, c.Slug, c.Description, c.Icon, c.ProductCount);

    private static CustomerDto ToCustomer(V1Customer c) => new(
        c.Id.ToString(), c.Email, c.FullName, c.Role, c.Company,
        null, c.Country, c.CreatedAt, null,
        c.OrderCount, c.TotalSpentToman, c.LicenseCount);

    private static DiscountCodeDto ToDiscount(V1Discount d) => new(
        d.Id.ToString(), d.Code, d.Description, d.PercentOff, d.MaxRedemptions,
        d.TimesRedeemed, d.ExpiresAt, d.IsActive, d.CreatedAt);

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

    /// <summary>خطایِ آخرین عملیات (مثلاً رمزِ فعلیِ نادرست).</summary>
    public string? LastError { get; private set; }


    private static PagedMeta Meta(V1PageMeta m) =>
        new(m.Page, m.PageSize, m.TotalCount, m.TotalPages);

    private static UserDto ToUser(V1User u) => new(
        u.Id.ToString(), u.Email, u.FullName, u.Role,
        u.Company, u.Phone, u.Country, u.CreatedAt, u.LastLoginAt);

    private static LicenseDto ToLicense(V1License l) => new(
        l.Id.ToString(), l.LicenseKey, l.UserId.ToString(), l.UserName, l.UserEmail,
        l.ProductId.ToString(), l.ProductName, l.ProductSlug, l.PlanName,
        l.Seats, l.ActivationsUsed, l.Status, l.IssuedAt, l.ExpiresAt);

    private static InvoiceDto ToInvoice(V1Invoice i) => new(
        i.Id.ToString(), i.InvoiceNumber, i.OrderId.ToString(), i.Status,
        i.IssuedAt, i.DueAt, i.TotalToman, i.TaxToman, null);

    private static DownloadDto ToDownload(V1Download d) => new(
        d.Id.ToString(), d.ProductId.ToString(), d.ProductName, d.ProductSlug,
        d.VersionId?.ToString(), d.Version, d.Kind, d.FileName, d.SizeMb,
        d.DownloadedAt, d.RequiresLicense);

    private static TicketDto ToTicket(V1Ticket t) => ToTicketSummary(t);

    private static TicketDto ToTicketSummary(V1TicketDetail t) => new(
        t.Id.ToString(), t.Reference, t.UserId.ToString(), t.UserName, t.ProductName,
        t.Subject, t.Category, t.Priority, t.Status, t.Environment,
        t.CreatedAt, t.UpdatedAt, t.Messages.Count);

    private static TicketDto ToTicketSummary(V1Ticket t) => new(
        t.Id.ToString(), t.Reference, t.UserId.ToString(), t.UserName, t.ProductName,
        t.Subject, t.Category, t.Priority, t.Status, t.Environment,
        t.CreatedAt, t.UpdatedAt, t.MessageCount);

    private static TicketDetailDto ToTicketDetail(V1TicketDetail t) => new(
        t.Id.ToString(), t.Reference, t.UserId.ToString(), t.UserName,
        t.ProductId?.ToString(), t.ProductName,
        t.Subject, t.Category, t.Priority, t.Status, t.Environment,
        t.CreatedAt, t.UpdatedAt,
        t.Messages.Select(m => new TicketMessageDto(
            m.Id.ToString(), m.TicketId.ToString(), m.AuthorName, m.IsStaff,
            m.Body, m.CreatedAt)).ToList());

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
