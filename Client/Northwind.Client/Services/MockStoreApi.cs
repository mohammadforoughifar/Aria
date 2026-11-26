// پیاده‌سازیِ درون‌حافظه‌ایِ IStoreApi.
// تمام محاسباتِ تجاری (سبد، تخفیف، مالیات، تسویه، صدور لایسنس) اینجا زنده‌اند
// تا قالب با رفتارِ واقعی آزمایش شود؛ بعداً تنها منبعِ داده عوض می‌شود.

using Northwind.Client.Models;

using Northwind.Client.Contracts;

namespace Northwind.Client.Services;

public sealed class MockStoreApi : IStoreApi
{
    public string? LastError => null;

    private readonly StoreState _s;

    public MockStoreApi(StoreState state) => _s = state;

    private UserDto Me => _s.CurrentUser ?? _s.Users.First(u => u.Role == Role.Customer);
    private bool IsAdmin => Me.Role == Role.Admin;

    /* ------------------------------ Settings ----------------------------- */
    public Task<SiteSettingsDto> GetSettingsAsync() => Task.FromResult(_s.Settings);

    public Task<SiteSettingsDto> UpdateSettingsAsync(SiteSettingsDto settings)
    {
        _s.Settings = settings;
        return Task.FromResult(settings);
    }

    /* ------------------------------ Catalog ------------------------------ */

    public Task<Paged<ProductListItemDto>> GetProductsAsync(ProductQuery q)
    {
        IEnumerable<ProductDetailDto> items = _s.Products;

        if (!q.IncludeInactive) items = items.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(q.Category) && q.Category != "all")
            items = items.Where(p => p.CategorySlug == q.Category);
        if (!string.IsNullOrWhiteSpace(q.Type) && q.Type != "all")
            items = items.Where(p => p.Type.ToString() == q.Type);
        if (q.Featured == true) items = items.Where(p => p.IsFeatured);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            items = items.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Tagline.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.CategoryName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var desc = q.SortDir != "asc";
        items = q.SortBy switch
        {
            "oldest"     => items.OrderBy(p => p.CreatedAt),
            "price-asc"  => items.OrderBy(p => p.BasePriceToman),
            "price-desc" => items.OrderByDescending(p => p.BasePriceToman),
            "popular"    => items.OrderByDescending(p => p.Popularity),
            "rating"     => items.OrderByDescending(p => p.RatingAvg).ThenByDescending(p => p.RatingCount),
            "name"       => desc ? items.OrderByDescending(p => p.Name) : items.OrderBy(p => p.Name),
            _            => desc ? items.OrderByDescending(p => p.CreatedAt) : items.OrderBy(p => p.CreatedAt),
        };

        var total = items.Count();
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 100);

        var slice = items.Skip((page - 1) * size).Take(size)
            .Select(p => new ProductListItemDto(
                p.Id, p.Slug, p.Name, p.Tagline, p.Type, p.Platform, p.Version, p.BasePriceToman,
                p.RatingAvg, p.RatingCount, p.Popularity, p.HeroImage, p.AccentColor,
                p.IsFeatured, p.IsActive, p.CreatedAt, p.CategoryName, p.CategorySlug, p.Plans.Count))
            .ToList();

        return Task.FromResult(new Paged<ProductListItemDto>(slice,
            new PagedMeta(page, size, total, Math.Max(1, (int)Math.Ceiling(total / (double)size)))));
    }

    public Task<ProductDetailDto?> GetProductBySlugAsync(string slug)
        => Task.FromResult(_s.Products.FirstOrDefault(p => p.Slug == slug) is { } p ? WithReviews(p) : null);

    public Task<ProductDetailDto?> GetProductByIdAsync(string id)
        => Task.FromResult(_s.Products.FirstOrDefault(p => p.Id == id) is { } p ? WithReviews(p) : null);

    public Task<IReadOnlyList<ProductDetailDto>> GetProductsByIdsAsync(IEnumerable<string> ids)
    {
        var set = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<ProductDetailDto>>(
            _s.Products.Where(p => set.Contains(p.Id)).Select(WithReviews).ToList());
    }

    private ProductDetailDto WithReviews(ProductDetailDto p) =>
        p with { Reviews = _s.Reviews.Where(r => r.ProductId == p.Id && r.Status == ReviewStatus.Approved)
                                     .OrderByDescending(r => r.CreatedAt).ToList() };

    public Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(bool includeInactive = false)
        => Task.FromResult<IReadOnlyList<CategoryDto>>(_s.Categories.ToList());

    public Task<CategoryDto> UpsertCategoryAsync(CategoryDto category)
    {
        var existing = _s.Categories.FirstOrDefault(c => c.Id == category.Id);
        if (existing is not null) _s.Categories[_s.Categories.IndexOf(existing)] = category;
        else _s.Categories.Add(category);
        return Task.FromResult(category);
    }

    public Task DeleteCategoryAsync(string id)
    {
        var c = _s.Categories.FirstOrDefault(x => x.Id == id);
        if (c is not null) _s.Categories.Remove(c);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TestimonialDto>> GetTestimonialsAsync()
        => Task.FromResult<IReadOnlyList<TestimonialDto>>(_s.Testimonials.ToList());

    public Task<IReadOnlyList<FaqDto>> GetFaqsAsync()
        => Task.FromResult<IReadOnlyList<FaqDto>>(_s.Faqs.ToList());

    /* ------------------------------- Cart -------------------------------- */

    public Task<CartDto> GetCartAsync(string sessionKey, string? discountCode = null)
        => Task.FromResult(BuildCart(sessionKey, discountCode));

    private CartDto BuildCart(string sessionKey, string? discountCode)
    {
        var lines = _s.CartLines.Where(c => c.SessionKey == sessionKey).ToList();

        var items = new List<CartItemDto>();
        foreach (var line in lines)
        {
            var product = _s.ProductOfPlan(line.PlanId);
            var plan = _s.FindPlan(line.PlanId);
            if (product is null || plan is null) continue;

            items.Add(new CartItemDto(
                line.Id, line.PlanId, line.Quantity, product.Id, product.Name, product.Slug,
                plan.Name, plan.Tier, plan.BillingType, product.AccentColor,
                plan.PriceToman, plan.PriceToman * line.Quantity));
        }

        var subtotal = items.Sum(i => i.LineTotalToman);
        var discount = 0;
        string? applied = null;

        if (!string.IsNullOrWhiteSpace(discountCode))
        {
            var code = _s.Discounts.FirstOrDefault(d =>
                d.Code.Equals(discountCode, StringComparison.OrdinalIgnoreCase) && d.IsActive &&
                (d.ExpiresAt is null || d.ExpiresAt > DateTime.UtcNow));
            if (code is not null)
            {
                discount = (int)(subtotal * code.PercentOff / 100);
                applied = code.Code;
            }
        }

        var taxable = Math.Max(0, subtotal - discount);
        var tax = (int)Math.Round(taxable * _s.Settings.TaxRateBps / 10_000m);

        return new CartDto(sessionKey, items, items.Sum(i => i.Quantity),
            subtotal, discount, tax, taxable + tax, applied, "USD");
    }

    public Task<CartDto> AddToCartAsync(string sessionKey, string planId, int quantity = 1)
    {
        var existing = _s.CartLines.FirstOrDefault(c => c.SessionKey == sessionKey && c.PlanId == planId);
        if (existing is not null) existing.Quantity += quantity;
        else _s.CartLines.Add(new CartLine { SessionKey = sessionKey, PlanId = planId, Quantity = quantity });
        return Task.FromResult(BuildCart(sessionKey, null));
    }

    public Task<CartDto> UpdateCartItemAsync(string sessionKey, string itemId, int quantity)
    {
        var line = _s.CartLines.FirstOrDefault(c => c.SessionKey == sessionKey && c.Id == itemId);
        if (line is not null)
        {
            if (quantity <= 0) _s.CartLines.Remove(line);
            else line.Quantity = Math.Min(quantity, 99);
        }
        return Task.FromResult(BuildCart(sessionKey, null));
    }

    public Task<CartDto> RemoveCartItemAsync(string sessionKey, string itemId)
    {
        var line = _s.CartLines.FirstOrDefault(c => c.SessionKey == sessionKey && c.Id == itemId);
        if (line is not null) _s.CartLines.Remove(line);
        return Task.FromResult(BuildCart(sessionKey, null));
    }

    public Task<CartDto> ApplyDiscountAsync(string sessionKey, string code)
        => Task.FromResult(BuildCart(sessionKey, code));

    /* ------------------------------ Orders ------------------------------- */

    public Task<Paged<OrderDto>> GetAdminOrdersAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
        => GetOrdersAsync(page, pageSize, status, search);

    public Task<Paged<LicenseDto>> GetAdminLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
        => GetLicensesAsync(page, pageSize, status, search);

    public Task<Paged<TicketDto>> GetAdminTicketsAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
        => GetTicketsAsync(page, pageSize, status, search);

    public Task<TicketDetailDto> UpdateAdminTicketStatusAsync(string id, TicketStatus status, string? reply = null)
        => AddTicketMessageAsync(id, string.IsNullOrWhiteSpace(reply) ? "وضعیت به‌روزرسانی شد." : reply, status);

    public Task<Paged<ReviewDto>> GetAdminReviewsAsync(int page = 1, int pageSize = 10, ReviewStatus? status = null)
        => GetReviewsAsync(page, pageSize, null, status);

    public Task<Paged<OrderDto>> GetOrdersAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
    {
        IEnumerable<OrderDto> items = IsAdmin
            ? _s.Orders
            : _s.Orders.Where(o => o.UserId == Me.Id);

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            items = items.Where(o => o.Status.ToString() == status);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(o =>
                o.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (o.CustomerName?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        items = items.OrderByDescending(o => o.PlacedAt);
        return Task.FromResult(Page(items.ToList(), page, pageSize));
    }

    public Task<OrderDto?> GetOrderAsync(string id)
    {
        var order = _s.Orders.FirstOrDefault(o => o.Id == id);
        if (order is null) return Task.FromResult<OrderDto?>(null);
        if (!IsAdmin && order.UserId != Me.Id) return Task.FromResult<OrderDto?>(null);
        return Task.FromResult<OrderDto?>(order);
    }

    public Task<OrderDto> CheckoutAsync(CheckoutInput input)
    {
        var cart = BuildCart(input.SessionKey, input.DiscountCode);
        if (cart.Items.Count == 0)
            throw new InvalidOperationException("سبد خرید خالی است.");

        var orderId = $"o-{_s.Orders.Count + 1}-{Guid.NewGuid().ToString("N")[..6]}";
        var order = new OrderDto(
            orderId, _s.NextOrderNumber(), Me.Id, input.BillingName, input.BillingEmail, input.BillingCompany,
            OrderStatus.Paid, cart.SubtotalToman, cart.DiscountToman, cart.TaxToman, cart.TotalToman,
            "USD", cart.DiscountCode, DateTime.UtcNow,
            input.BillingName, input.BillingCompany, input.BillingEmail, input.BillingCountry, input.Notes,
            cart.Items.Select(i => new OrderItemDto(
                $"oi-{Guid.NewGuid().ToString("N")[..8]}", i.ProductId, i.PlanId, i.ProductName, i.PlanName,
                i.BillingType, i.UnitPriceToman, i.Quantity, i.LineTotalToman)).ToList(),
            new PaymentDto($"pay-{Guid.NewGuid().ToString("N")[..8]}", orderId, "MockGateway",
                input.PaymentMethod, cart.TotalToman, PaymentStatus.Succeeded,
                $"txn_{Guid.NewGuid().ToString("N")[..12]}", DateTime.UtcNow),
            null);

        var invoice = new InvoiceDto(
            $"inv-{_s.Invoices.Count + 1}-{Guid.NewGuid().ToString("N")[..6]}", _s.NextInvoiceNumber(),
            orderId, InvoiceStatus.Paid, DateTime.UtcNow, DateTime.UtcNow.AddDays(14),
            cart.TotalToman, cart.TaxToman, null);

        order = order with { Invoice = invoice };
        _s.Invoices.Add(invoice);

        foreach (var item in cart.Items)
        {
            var plan = _s.FindPlan(item.PlanId);
            var product = _s.ProductOfPlan(item.PlanId);
            if (plan is null || product is null) continue;

            for (var i = 0; i < item.Quantity; i++)
            {
                _s.Licenses.Add(new LicenseDto(
                    $"lic-{Guid.NewGuid().ToString("N")[..10]}", LicenseKey(), Me.Id, Me.FullName, Me.Email,
                    product.Id, product.Name, product.Slug, plan.Name, plan.Seats, 0,
                    LicenseStatus.Active, DateTime.UtcNow,
                    plan.BillingType == BillingType.Annual ? DateTime.UtcNow.AddYears(1) : null));
            }
        }

        if (cart.DiscountCode is not null)
        {
            var code = _s.Discounts.FirstOrDefault(d => d.Code == cart.DiscountCode);
            if (code is not null) _s.Discounts[_s.Discounts.IndexOf(code)] = code with { TimesRedeemed = code.TimesRedeemed + 1 };
        }

        _s.Orders.Add(order);
        _s.CartLines.RemoveAll(c => c.SessionKey == input.SessionKey);
        return Task.FromResult(order);
    }

    public Task<OrderDto> UpdateOrderStatusAsync(string id, OrderStatus status)
    {
        var order = _s.Orders.First(o => o.Id == id);
        var updated = order with { Status = status };
        _s.Orders[_s.Orders.IndexOf(order)] = updated;
        return Task.FromResult(updated);
    }

    /* ----------------------------- Invoices ------------------------------ */

    public Task<Paged<InvoiceDto>> GetInvoicesAsync(int page = 1, int pageSize = 10)
    {
        var items = (IsAdmin ? _s.Invoices : _s.Invoices.Where(i => OrderOf(i.OrderId)?.UserId == Me.Id))
            .OrderByDescending(i => i.IssuedAt)
            .Select(i => i with { Order = OrderOf(i.OrderId) })
            .ToList();
        return Task.FromResult(Page(items, page, pageSize));
    }

    public Task<InvoiceDto?> GetInvoiceAsync(string id)
    {
        var inv = _s.Invoices.FirstOrDefault(i => i.Id == id) is { } i2 ? i2 with { Order = OrderOf(i2.OrderId) } : null;
        return Task.FromResult(inv);
    }

    private OrderDto? OrderOf(string orderId) => _s.Orders.FirstOrDefault(o => o.Id == orderId);

    /* ----------------------------- Licenses ------------------------------ */

    public Task<Paged<LicenseDto>> GetLicensesAsync(int page = 1, int pageSize = 12, string? status = null, string? search = null)
    {
        IEnumerable<LicenseDto> items = IsAdmin ? _s.Licenses : _s.Licenses.Where(l => l.UserId == Me.Id);

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            items = items.Where(l => l.Status.ToString() == status);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(l => l.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     (l.LicenseKey.Contains(search, StringComparison.OrdinalIgnoreCase)));

        items = items.OrderByDescending(l => l.IssuedAt);
        return Task.FromResult(Page(items.ToList(), page, pageSize));
    }

    public Task<LicenseDto> LicenseActionAsync(string id, string action, int? days = null, string? machineName = null)
    {
        var lic = _s.Licenses.First(l => l.Id == id);
        var updated = action switch
        {
            "revoke"     => lic with { Status = LicenseStatus.Revoked },
            "reactivate" => lic with { Status = LicenseStatus.Active },
            "extend"     => lic with { Status = LicenseStatus.Active,
                                       ExpiresAt = (lic.ExpiresAt is { } e && e > DateTime.UtcNow ? e : DateTime.UtcNow).AddDays(days ?? 365) },
            "activate"   => lic with { ActivationsUsed = Math.Min(lic.Seats, lic.ActivationsUsed + 1), Status = LicenseStatus.Active },
            _            => lic,
        };
        _s.Licenses[_s.Licenses.IndexOf(lic)] = updated;
        return Task.FromResult(updated);
    }

    public Task<LicenseDto> GenerateLicenseAsync(string userId, string productId, string? planId, int seats, int? validDays)
    {
        var product = _s.Products.First(p => p.Id == productId);
        var user = _s.Users.First(u => u.Id == userId);
        var plan = planId is null ? null : _s.FindPlan(planId);

        var lic = new LicenseDto(
            $"lic-{Guid.NewGuid().ToString("N")[..10]}", LicenseKey(), user.Id, user.FullName, user.Email,
            product.Id, product.Name, product.Slug, plan?.Name, seats, 0, LicenseStatus.Active,
            DateTime.UtcNow, validDays is null ? null : DateTime.UtcNow.AddDays(validDays.Value));

        _s.Licenses.Add(lic);
        return Task.FromResult(lic);
    }

    /* ----------------------------- Downloads ----------------------------- */

    public Task<IReadOnlyList<AvailableDownloadDto>> GetAvailableDownloadsAsync()
    {
        var owned = _s.Licenses.Where(l => l.UserId == Me.Id && l.Status == LicenseStatus.Active)
                               .Select(l => l.ProductId).ToHashSet();

        var result = _s.Products.Select(p => new AvailableDownloadDto(
            p.Id, p.Name, p.Slug, p.AccentColor, owned.Contains(p.Id),
            p.Versions.Select(v => new AvailableVersionDto(
                v.Id, v.Version, v.ReleasedAt, v.Platform, v.SizeMb,
                $"{p.Slug}-{v.Version}-setup.exe", v.InstallerUrl, v.Notes)).ToList())).ToList();

        return Task.FromResult<IReadOnlyList<AvailableDownloadDto>>(result);
    }

    public Task<IReadOnlyList<DownloadDto>> GetDownloadHistoryAsync()
        => Task.FromResult<IReadOnlyList<DownloadDto>>(
            _s.Downloads.Where(d => d.UserId == Me.Id).OrderByDescending(d => d.DownloadedAt).Take(50).ToList());

    public Task<DownloadDto> RecordDownloadAsync(string productId, string? versionId, string kind, string fileName, int sizeMb)
    {
        var product = _s.Products.First(p => p.Id == productId);
        var version = product.Versions.FirstOrDefault(v => v.Id == versionId);
        var row = new DownloadDto(
            $"dl-{Guid.NewGuid().ToString("N")[..8]}", product.Id, product.Name, product.Slug, versionId,
            version?.Version ?? "latest", kind, fileName, sizeMb > 0 ? sizeMb : version?.SizeMb ?? 0,
            DateTime.UtcNow, kind == "installer", Me.Id);
        _s.Downloads.Insert(0, row);
        return Task.FromResult(row);
    }

    /* ------------------------------ Reviews ------------------------------ */

    public Task<Paged<ReviewDto>> GetReviewsAsync(int page = 1, int pageSize = 10, string? productId = null, ReviewStatus? status = null)
    {
        IEnumerable<ReviewDto> items = _s.Reviews;
        if (!string.IsNullOrWhiteSpace(productId)) items = items.Where(r => r.ProductId == productId);
        if (status is not null) items = items.Where(r => r.Status == status);
        items = items.OrderByDescending(r => r.CreatedAt);
        return Task.FromResult(Page(items.ToList(), page, pageSize));
    }

    public Task<ReviewDto> CreateReviewAsync(NewReviewInput input)
    {
        var product = _s.Products.First(p => p.Id == input.ProductId);
        var review = new ReviewDto($"r-{Guid.NewGuid().ToString("N")[..8]}", product.Id, product.Name,
            Me.FullName, Me.Company, input.Rating, input.Title, input.Body,
            ReviewStatus.Pending, DateTime.UtcNow);
        _s.Reviews.Insert(0, review);
        return Task.FromResult(review);
    }

    public Task<ReviewDto> ModerateReviewAsync(string id, ReviewStatus status)
    {
        var review = _s.Reviews.First(r => r.Id == id);
        var updated = review with { Status = status };
        _s.Reviews[_s.Reviews.IndexOf(review)] = updated;
        return Task.FromResult(updated);
    }

    public Task DeleteReviewAsync(string id)
    {
        var review = _s.Reviews.FirstOrDefault(r => r.Id == id);
        if (review is not null) _s.Reviews.Remove(review);
        return Task.CompletedTask;
    }

    /* ------------------------------ Tickets ------------------------------ */

    public Task<Paged<TicketDto>> GetTicketsAsync(int page = 1, int pageSize = 10, string? status = null, string? search = null)
    {
        IEnumerable<TicketDetailDto> items = IsAdmin ? _s.Tickets : _s.Tickets.Where(t => t.UserId == Me.Id);
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
            items = items.Where(t => t.Status.ToString() == status);
        if (!string.IsNullOrWhiteSpace(search))
            items = items.Where(t => t.Subject.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     t.Reference.Contains(search, StringComparison.OrdinalIgnoreCase));

        var list = items.OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketDto(t.Id, t.Reference, t.UserId, t.UserName, t.ProductName, t.Subject,
                t.Category, t.Priority, t.Status, t.Environment, t.CreatedAt, t.UpdatedAt, t.Messages.Count))
            .ToList();
        return Task.FromResult(Page(list, page, pageSize));
    }

    public Task<TicketDetailDto?> GetTicketAsync(string id)
    {
        var t = _s.Tickets.FirstOrDefault(x => x.Id == id);
        if (t is null) return Task.FromResult<TicketDetailDto?>(null);
        if (!IsAdmin && t.UserId != Me.Id) return Task.FromResult<TicketDetailDto?>(null);
        return Task.FromResult<TicketDetailDto?>(t);
    }

    public Task<TicketDto> CreateTicketAsync(NewTicketInput input)
    {
        var product = input.ProductId is null ? null : _s.Products.FirstOrDefault(p => p.Id == input.ProductId);
        var id = $"tk-{_s.Tickets.Count + 1}-{Guid.NewGuid().ToString("N")[..6]}";
        var detail = new TicketDetailDto(id, $"TK-{1050 + _s.Tickets.Count}", Me.Id, Me.FullName,
            product?.Id, product?.Name, input.Subject, input.Category, input.Priority,
            TicketStatus.Open, input.Environment, DateTime.UtcNow, DateTime.UtcNow,
            new[] { new TicketMessageDto($"m-{Guid.NewGuid().ToString("N")[..8]}", id, Me.FullName, false, input.Message, DateTime.UtcNow) });

        _s.Tickets.Insert(0, detail);
        return Task.FromResult(ToDto(detail));
    }

    public Task<TicketDetailDto> AddTicketMessageAsync(string id, string body, TicketStatus? status = null)
    {
        var t = _s.Tickets.First(x => x.Id == id);
        var messages = t.Messages.Append(new TicketMessageDto(
            $"m-{Guid.NewGuid().ToString("N")[..8]}", id, Me.FullName, IsAdmin, body, DateTime.UtcNow)).ToList();

        var updated = t with
        {
            Messages = messages,
            UpdatedAt = DateTime.UtcNow,
            Status = status ?? (IsAdmin ? TicketStatus.WaitingCustomer : TicketStatus.InProgress),
        };
        _s.Tickets[_s.Tickets.IndexOf(t)] = updated;
        return Task.FromResult(updated);
    }

    private static TicketDto ToDto(TicketDetailDto t) => new(t.Id, t.Reference, t.UserId, t.UserName, t.ProductName,
        t.Subject, t.Category, t.Priority, t.Status, t.Environment, t.CreatedAt, t.UpdatedAt, t.Messages.Count);

    /* ------------------------------- Auth -------------------------------- */

    public Task<UserDto?> LoginAsync(string email, string password)
    {
        var user = _s.Users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        if (user is null || !_s.Passwords.TryGetValue(user.Email, out var pw) || pw != password)
            return Task.FromResult<UserDto?>(null);

        _s.CurrentUserId = user.Id;
        return Task.FromResult<UserDto?>(user);
    }

    public Task<UserDto?> RegisterAsync(string fullName, string email, string password, string? company, string? country)
    {
        if (_s.Users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult<UserDto?>(null);

        var user = new UserDto($"u-{Guid.NewGuid().ToString("N")[..8]}", email, fullName, Role.Customer,
            company, null, country, DateTime.UtcNow, DateTime.UtcNow);

        _s.Users.Add(user);
        _s.Passwords[email] = password;
        _s.CurrentUserId = user.Id;
        return Task.FromResult<UserDto?>(user);
    }

    public Task<UserDto> UpdateProfileAsync(string fullName, string? company, string? phone, string? country)
    {
        var user = Me with { FullName = fullName, Company = company, Phone = phone, Country = country };
        var idx = _s.Users.FindIndex(u => u.Id == user.Id);
        _s.Users[idx] = user;
        return Task.FromResult(user);
    }

    public Task<bool> ChangePasswordAsync(string current, string next)
    {
        if (!_s.Passwords.TryGetValue(Me.Email, out var pw) || pw != current) return Task.FromResult(false);
        _s.Passwords[Me.Email] = next;
        return Task.FromResult(true);
    }

    /* ----------------------------- Dashboard ----------------------------- */

    public Task<AdminOverviewDto> GetAdminOverviewAsync()
    {
        var now = DateTime.UtcNow;
        var thisMonth = _s.Orders.Where(o => o.PlacedAt.Month == now.Month && o.PlacedAt.Year == now.Year && o.Status != OrderStatus.Refunded);
        var lastMonth = _s.Orders.Where(o => o.PlacedAt.Month == now.AddMonths(-1).Month && o.PlacedAt.Year == now.AddMonths(-1).Year && o.Status != OrderStatus.Refunded);

        var series = Enumerable.Range(0, 12).Select(i =>
        {
            var m = now.AddMonths(i - 11);
            var label = m.ToString("MMM", System.Globalization.CultureInfo.InvariantCulture);
            var total = _s.Orders.Where(o => o.PlacedAt.Month == m.Month && o.PlacedAt.Year == m.Year && o.Status != OrderStatus.Refunded)
                                 .Sum(o => o.TotalToman);
            // برچسبِ ماه را به فارسی برمی‌گردانیم
            var fa = PersianMonthName(m.Month);
            return new RevenuePointDto(fa, total > 0 ? total : RandomRevenueFor(m));
        }).ToList();

        var top = _s.Orders.Where(o => o.Status != OrderStatus.Refunded)
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g =>
            {
                var p = _s.Products.FirstOrDefault(x => x.Id == g.Key);
                return new TopProductDto(g.Key, p?.Name ?? g.Key, p?.Slug ?? "", g.Sum(i => i.Quantity),
                    g.Sum(i => i.LineTotalToman), p?.AccentColor ?? "#4f46e5");
            })
            .OrderByDescending(x => x.RevenueToman)
            .Take(5).ToList();

        return Task.FromResult(new AdminOverviewDto(
            _s.Products.Count,
            _s.Users.Count(u => u.Role == Role.Customer),
            _s.Tickets.Count(t => t.Status is TicketStatus.Open or TicketStatus.InProgress),
            _s.Reviews.Count(r => r.Status == ReviewStatus.Pending),
            _s.Licenses.Count(l => l.Status == LicenseStatus.Active),
            _s.Licenses.Count(l => l.Status == LicenseStatus.Active && l.ExpiresAt is not null &&
                                   l.ExpiresAt < now.AddDays(45)),
            thisMonth.Sum(o => o.TotalToman),
            lastMonth.Sum(o => o.TotalToman),
            series, top,
            _s.Orders.OrderByDescending(o => o.PlacedAt).Take(6).ToList(),
            _s.Tickets.OrderByDescending(t => t.UpdatedAt).Take(5).Select(ToDto).ToList()));
    }

    public Task<CustomerOverviewDto> GetCustomerOverviewAsync()
    {
        var mine = _s.Orders.Where(o => o.UserId == Me.Id).ToList();
        var myLicenses = _s.Licenses.Where(l => l.UserId == Me.Id).ToList();
        var now = DateTime.UtcNow;

        var series = Enumerable.Range(0, 6).Select(i =>
        {
            var m = now.AddMonths(i - 5);
            return new RevenuePointDto(PersianMonthName(m.Month),
                mine.Where(o => o.PlacedAt.Month == m.Month && o.PlacedAt.Year == m.Year && o.Status != OrderStatus.Refunded)
                    .Sum(o => o.TotalToman));
        }).ToList();

        return Task.FromResult(new CustomerOverviewDto(
            mine.Count,
            myLicenses.Count(l => l.Status == LicenseStatus.Active),
            myLicenses.Count(l => l.Status == LicenseStatus.Active && l.ExpiresAt is not null && l.ExpiresAt < now.AddDays(45)),
            _s.Tickets.Count(t => t.UserId == Me.Id && t.Status is TicketStatus.Open or TicketStatus.InProgress),
            mine.Where(o => o.Status != OrderStatus.Refunded).Sum(o => o.TotalToman),
            _s.Products.Count,
            mine.Where(o => o.Status == OrderStatus.Pending).Sum(o => o.TotalToman),
            mine.OrderByDescending(o => o.PlacedAt).Take(5).ToList(),
            myLicenses.Where(l => l.ExpiresAt is not null).OrderBy(l => l.ExpiresAt).Take(4).ToList(),
            series));
    }

    /* -------------------------- Admin: customers ------------------------- */

    public Task<Paged<CustomerDto>> GetCustomersAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        IEnumerable<UserDto> users = _s.Users;
        if (!string.IsNullOrWhiteSpace(search))
            users = users.Where(u => u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     u.Email.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     (u.Company?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));

        var list = users.Select(u =>
        {
            var orders = _s.Orders.Where(o => o.UserId == u.Id && o.Status != OrderStatus.Refunded).ToList();
            return new CustomerDto(u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country,
                u.CreatedAt, u.LastLoginAt, orders.Count, orders.Sum(o => o.TotalToman),
                _s.Licenses.Count(l => l.UserId == u.Id));
        }).OrderByDescending(c => c.TotalSpentToman).ToList();

        return Task.FromResult(Page(list, page, pageSize));
    }

    public Task<CustomerDto> UpdateCustomerAsync(string id, string fullName, string? company, Role role, bool locked)
    {
        var user = _s.Users.First(u => u.Id == id);
        var updated = user with { FullName = fullName, Company = company, Role = role };
        _s.Users[_s.Users.IndexOf(user)] = updated;
        return Task.FromResult(new CustomerDto(updated.Id, updated.Email, updated.FullName, updated.Role,
            updated.Company, updated.Phone, updated.Country, updated.CreatedAt, updated.LastLoginAt, 0, 0, 0));
    }

    /* ------------------------ Admin: discounts --------------------------- */

    public Task<IReadOnlyList<DiscountCodeDto>> GetDiscountCodesAsync()
        => Task.FromResult<IReadOnlyList<DiscountCodeDto>>(_s.Discounts.ToList());

    public Task<DiscountCodeDto> UpsertDiscountAsync(DiscountCodeDto code)
    {
        var existing = _s.Discounts.FirstOrDefault(d => d.Id == code.Id);
        if (existing is not null) _s.Discounts[_s.Discounts.IndexOf(existing)] = code;
        else _s.Discounts.Insert(0, code);
        return Task.FromResult(code);
    }

    public Task DeleteDiscountAsync(string id)
    {
        var d = _s.Discounts.FirstOrDefault(x => x.Id == id);
        if (d is not null) _s.Discounts.Remove(d);
        return Task.CompletedTask;
    }

    /* --------------------- Admin: catalog mutations ---------------------- */

    public Task<ProductDetailDto> UpsertProductAsync(ProductDetailDto product)
    {
        var existing = _s.Products.FirstOrDefault(p => p.Id == product.Id);
        if (existing is not null) _s.Products[_s.Products.IndexOf(existing)] = product;
        else _s.Products.Insert(0, product);
        return Task.FromResult(product);
    }

    public Task DeleteProductAsync(string id)
    {
        var p = _s.Products.FirstOrDefault(x => x.Id == id);
        if (p is not null) _s.Products.Remove(p);
        return Task.CompletedTask;
    }

    /* --------------------------- Content forms --------------------------- */

    public Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync()
        => Task.FromResult<IReadOnlyList<NotificationDto>>(_s.Notifications
            .Where(n => n.UserId == _s.CurrentUserId).OrderByDescending(n => n.CreatedAt).ToList());

    public Task MarkNotificationReadAsync(string id)
    {
        var n = _s.Notifications.FirstOrDefault(x => x.Id == id);
        if (n is not null)
        {
            _s.Notifications[_s.Notifications.IndexOf(n)] = n with { IsRead = true };
        }
        return Task.CompletedTask;
    }

    public Task SubmitContactAsync(string name, string email, string? company, string topic, string message)
        => Task.CompletedTask;

    public Task SubmitDemoRequestAsync(string? productId, string name, string email, string? company,
        string? companySize, string? message) => Task.CompletedTask;

    /* ------------------------------ helpers ------------------------------ */

    private static Paged<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = items.Count;
        var slice = items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new Paged<T>(slice, new PagedMeta(page, pageSize, total, Math.Max(1, (int)Math.Ceiling(total / (double)pageSize))));
    }

    private static string LicenseKey()
    {
        const string charset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rnd = Random.Shared;
        string Group(int n) => new(Enumerable.Range(0, n).Select(_ => charset[rnd.Next(charset.Length)]).ToArray());
        return $"NW-{Group(5)}-{Group(5)}-{Group(5)}-{Group(5)}";
    }

    private static readonly string[] PersianMonths =
        { "", "دی", "بهمن", "اسفند", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر" };

    /// <summary>نام ماهِ شمسیِ متناظر با یک تاریخ میلادی (تقریبی، برای برچسبِ نمودار).</summary>
    private static string PersianMonthName(int gregorianMonth) => PersianMonths[gregorianMonth];

    private static int RandomRevenueFor(DateTime month)
    {
        // دادهٔ تاریخیِ نمایشی برای ماه‌های بدون سفارشِ واقعی
        var seed = month.Year * 100 + month.Month;
        return 180_000 + (seed * 7919 % 420_000);
    }

    /* ------------------- وبلاگ و درخواست‌های دمو (حافظه‌ای) ------------------ */
    // این پیاده‌سازی فقط برای توسعهٔ آفلاین است؛ مقدارِ پیش‌فرضِ DI بر Api متکی است.

    private static readonly List<ArticleDto> _articles = new()
    {
        new ArticleDto("a-mock-1", "erp-vs-accounting",
            "تفاوتِ ERP با نرم‌افزار حسابداری چیست؟",
            "حسابداری یک بخش است؛ ERP کلِ سازمان را در یک پایگاه‌دادهٔ واحد به هم می‌دوزد.",
            "نرم‌افزار حسابداری فقط رویدادهای مالی را ثبت می‌کند. ERP اما انبار، خرید، تولید و فروش را هم به همان داده وصل می‌کند؛ در نتیجه گزارشِ بهای تمام‌شده در لحظه به‌روز می‌شود.",
            "راهنما", true, DateTime.UtcNow.AddDays(-12), 6),
        new ArticleDto("a-mock-2", "license-models",
            "کدام مدلِ لایسنس برای شما به‌صرفه‌تر است؟",
            "یک‌باره در برابر اشتراک سالانه؛ محاسبهٔ نقطهٔ سر‌به‌سر.",
            "اگر افقِ استفادهٔ شما زیرِ سه سال است، مدلِ یک‌باره ارزان‌تر تمام می‌شود؛ بالاتر از آن، اشتراکِ سالانه به‌دلیلِ به‌روزرسانی‌های پیوسته مقرون‌به‌صرفه‌تر است.",
            "قیمت‌گذاری", true, DateTime.UtcNow.AddDays(-30), 5),
    };

    public Task<Paged<ArticleDto>> GetArticlesAsync(int page = 1, int pageSize = 9, bool publishedOnly = true)
    {
        var q = _articles.Where(a => !publishedOnly || a.IsPublished).ToList();
        var take = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new Paged<ArticleDto>(take,
            new PagedMeta(page, pageSize, q.Count, (int)Math.Ceiling(q.Count / (double)pageSize))));
    }

    public Task<ArticleDto?> GetArticleBySlugAsync(string slug)
        => Task.FromResult(_articles.FirstOrDefault(a => a.Slug == slug));

    public Task<ArticleDto> UpsertArticleAsync(ArticleInput input)
    {
        var dto = new ArticleDto(input.Id ?? "a-" + Guid.NewGuid().ToString("N")[..8], input.Slug,
            input.Title, input.Excerpt, input.Body, input.Category, input.IsPublished,
            DateTime.UtcNow, input.ReadMinutes <= 0 ? 4 : input.ReadMinutes);
        var i = _articles.FindIndex(a => a.Id == dto.Id);
        if (i >= 0) _articles[i] = dto; else _articles.Insert(0, dto);
        return Task.FromResult(dto);
    }

    public Task DeleteArticleAsync(string id)
    {
        _articles.RemoveAll(a => a.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DemoRequestDto>> GetDemoRequestsAsync(string? status = null)
        => Task.FromResult<IReadOnlyList<DemoRequestDto>>(Array.Empty<DemoRequestDto>());

    public Task UpdateDemoRequestStatusAsync(string id, DemoRequestStatus status)
        => Task.CompletedTask;
}
