using Microsoft.EntityFrameworkCore;
using Northwind.Core.Mapping;
using Northwind.Core.Services.Common;
using Northwind.Core.Services.Security;
using Northwind.Data.Entities.Identity;
using Northwind.Core.Models.Commerce;
using Northwind.Core.Models.Common;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Entities.Support;
using Northwind.Data.Enums;
using Northwind.Data.Persistence;

namespace Northwind.Core.Services.Commerce;

public interface IOrderService
{
    /// <summary>تسویه‌حساب: سبد → سفارش + پرداخت + فاکتور + لایسنس.</summary>
    /// <summary>
    /// تسویه‌حساب. <paramref name="userId"/> تهی یعنی «خریدِ مهمان»: سفارش به
    /// حسابی با ایمیلِ صورتحساب وصل می‌شود و اگر چنین حسابی نباشد ساخته می‌شود.
    /// </summary>
    Task<CheckoutResultViewModel> CheckoutAsync(CheckoutInput input, Guid? userId, CancellationToken ct = default);

    Task<PagedViewModel<OrderViewModel>> ListMineAsync(Guid userId, ListQuery query, OrderStatus? status = null, CancellationToken ct = default);
    Task<OrderViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);

    Task<PagedViewModel<OrderViewModel>> AdminListAsync(ListQuery query, OrderStatus? status, CancellationToken ct = default);
    Task<OrderViewModel> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken ct = default);

    Task<PagedViewModel<InvoiceViewModel>> InvoicesMineAsync(Guid userId, ListQuery query, CancellationToken ct = default);
    Task<InvoiceViewModel> GetInvoiceAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);
}

public sealed class OrderService : IOrderService
{
    private readonly NorthwindDbContext _db;
    private readonly IPasswordHasher _hasher;

    public OrderService(NorthwindDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>
    /// خریدار را مشخص می‌کند.
    ///
    /// اگر کاربر وارد شده باشد، همان است. اگر نه (خریدِ مهمان)، حسابِ موجود با
    /// ایمیلِ صورتحساب پیدا می‌شود و اگر نبود یک حسابِ مشتری ساخته می‌شود.
    /// چرا حساب بسازیم؟ چون سفارش، فاکتور و لایسنس باید صاحب داشته باشند و
    /// کاربر بعداً با همین ایمیل آن‌ها را در پنل ببیند. رمزِ چنین حسابی تصادفی
    /// است، پس تا وقتی خودش رمز تعیین نکند کسی نمی‌تواند با آن وارد شود.
    /// </summary>
    private async Task<User> ResolveBuyerAsync(CheckoutInput input, Guid? userId, CancellationToken ct)
    {
        if (userId is { } id && id != Guid.Empty)
            return await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw new NotFoundException("کاربر", id);

        var email = input.BillingEmail.Trim().ToLowerInvariant();

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);
        if (existing is not null) return existing;

        var buyer = new User
        {
            Email = email,
            FullName = string.IsNullOrWhiteSpace(input.BillingName) ? email : input.BillingName.Trim(),
            PasswordHash = _hasher.Hash(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")),
            Role = Role.Customer,
            AccountType = input.AccountType,
            Company = input.BillingCompany ?? input.CompanyName,
            Country = input.BillingCountry,
            EconomicCode = input.EconomicCode,
            NationalId = input.NationalId
        };

        _db.Users.Add(buyer);
        return buyer;
    }

    public async Task<CheckoutResultViewModel> CheckoutAsync(CheckoutInput input, Guid? userId, CancellationToken ct = default)
    {
        var user = await ResolveBuyerAsync(input, userId, ct);

        if (user.Locked)
            throw new ForbiddenException("حسابِ شما قفل است.");

        var buyerId = user.Id;

        var cartItems = await _db.CartItems
            .Include(c => c.Plan).ThenInclude(p => p!.Product)
            .Where(c => c.UserId == buyerId || c.SessionKey == input.SessionKey)
            .ToListAsync(ct);

        if (cartItems.Count == 0)
            throw new BusinessRuleException("سبدِ خرید خالی است.", "cart_empty");

        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var taxBps = settings?.TaxRateBps ?? 0;

        // ---- مبالغ فقط این‌جا و از رویِ خودِ داده حساب می‌شوند، نه از ورودیِ کلاینت ----
        var lines = cartItems
            .Where(c => c.Plan?.Product is not null)
            .Select(c => new
            {
                Item = c,
                Plan = c.Plan!,
                Product = c.Plan!.Product!,
                LineTotal = c.Plan.PriceToman * c.Quantity
            })
            .ToList();

        var subtotal = lines.Sum(l => l.LineTotal);

        DiscountCode? discount = null;
        if (!string.IsNullOrWhiteSpace(input.DiscountCode))
        {
            var normalized = input.DiscountCode.Trim().ToUpperInvariant();
            discount = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Code.ToUpper() == normalized, ct);

            if (discount is null)
                throw new BusinessRuleException("کدِ تخفیف معتبر نیست.", "discount_not_found");
            if (!discount.IsActive)
                throw new BusinessRuleException("کدِ تخفیف غیرفعال است.", "discount_inactive");
            if (discount.ExpiresAt is not null && discount.ExpiresAt < DateTime.UtcNow)
                throw new BusinessRuleException("کدِ تخفیف منقضی شده است.", "discount_expired");
            if (discount.MaxRedemptions > 0 && discount.TimesRedeemed >= discount.MaxRedemptions)
                throw new BusinessRuleException("ظرفیتِ کدِ تخفیف پر شده است.", "discount_exhausted");
        }

        var discountToman = discount is null ? 0m : Money.Round(subtotal * (discount.PercentOff / 100m));
        var tax = Money.Round((subtotal - discountToman) * (taxBps / 10_000m));
        var total = subtotal - discountToman + tax;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;

            var order = new Order
            {
                OrderNumber = await NextOrderNumberAsync(ct),
                UserId = buyerId,
                Status = OrderStatus.Paid,
                SubtotalToman = subtotal,
                DiscountToman = discountToman,
                TaxToman = tax,
                TotalToman = total,
                Currency = "IRT",
                DiscountCode = discount?.Code,
                PlacedAt = now,
                AccountType = input.AccountType,
                BillingName = input.BillingName,
                BillingCompany = input.BillingCompany ?? input.CompanyName,
                BillingEmail = input.BillingEmail,
                BillingCountry = input.BillingCountry,
                EconomicCode = input.EconomicCode,
                NationalId = input.NationalId,
                Notes = input.Notes
            };

            foreach (var line in lines)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = line.Product.Id,
                    PlanId = line.Plan.Id,
                    ProductName = line.Product.Name,
                    PlanName = line.Plan.Name,
                    BillingType = line.Plan.BillingType,
                    UnitPriceToman = line.Plan.PriceToman,
                    Quantity = line.Item.Quantity,
                    LineTotalToman = line.LineTotal
                });
            }

            order.Payment = new Payment
            {
                Provider = "mock",
                Method = string.IsNullOrWhiteSpace(input.PaymentMethod) ? "card" : input.PaymentMethod,
                AmountToman = total,
                Status = PaymentStatus.Succeeded,
                TransactionRef = $"MOCK-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28],
                PaidAt = now
            };

            order.Invoice = new Invoice
            {
                InvoiceNumber = await NextInvoiceNumberAsync(ct),
                Status = InvoiceStatus.Paid,
                IssuedAt = now,
                TotalToman = total,
                TaxToman = tax
            };

            _db.Orders.Add(order);

            /* ---- لایسنس این‌جا ساخته نمی‌شود ----
               کلیدِ لایسنس در سامانهٔ جداگانهٔ صدور صادر می‌شود و این سایت
               فقط فروشگاه است؛ پس پرداخت، سفارش و فاکتور می‌سازد و حقِ
               دانلود را باز می‌کند (DownloadService بر پایهٔ سفارش‌هایِ
               پرداخت‌شده). کلیدها اگر از بیرون ثبت شوند، همان‌جا در پنل
               دیده و مدیریت می‌شوند.                                        */

            if (discount is not null) discount.TimesRedeemed += 1;

            _db.CartItems.RemoveRange(cartItems);

            _db.Notifications.Add(new Notification
            {
                UserId = buyerId,
                Title = "سفارشِ شما ثبت شد",
                Body = $"سفارش {order.OrderNumber} با موفقیت پرداخت شد و فاکتور آن صادر شد.",
                Kind = "success",
                Link = "/dashboard/orders"
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // بارگذاریِ روابط برایِ نگاشتِ پاسخ
            await _db.Entry(order).Reference(o => o.User).LoadAsync(ct);

            var vm = order.ToViewModel();
            return new CheckoutResultViewModel(vm, true, "پرداخت با موفقیت انجام شد.");
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<PagedViewModel<OrderViewModel>> ListMineAsync(Guid userId, ListQuery query,
        OrderStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Where(o => o.UserId == userId);

        if (status is not null) q = q.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(o => EF.Functions.Like(o.OrderNumber, $"%{s}%")
                          || EF.Functions.Like(o.BillingName ?? "", $"%{s}%"));
        }

        return await q.OrderByDescending(o => o.PlacedAt)
                      .ToPagedAsync(query, o => o.ToViewModel(), ct);
    }

    public async Task<OrderViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("سفارش", id);

        if (!isAdmin && order.UserId != userId)
            throw new ForbiddenException("این سفارش به شما تعلق ندارد.");

        return order.ToViewModel();
    }

    public async Task<PagedViewModel<OrderViewModel>> AdminListAsync(ListQuery query, OrderStatus? status, CancellationToken ct = default)
    {
        var q = _db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Include(o => o.User)
            .AsQueryable();

        if (status is not null) q = q.Where(o => o.Status == status);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(o => EF.Functions.Like(o.OrderNumber, $"%{s}%")
                          || EF.Functions.Like(o.BillingEmail, $"%{s}%"));
        }

        q = q.OrderByDescending(o => o.PlacedAt);
        return await q.ToPagedAsync(query, o => o.ToViewModel(), ct);
    }

    public async Task<OrderViewModel> UpdateStatusAsync(Guid id, OrderStatus status, CancellationToken ct = default)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.Payment)
            .Include(o => o.Invoice)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException("سفارش", id);

        if (order.Status is OrderStatus.Refunded or OrderStatus.Cancelled && status == OrderStatus.Paid)
            throw new ConflictException("سفارشِ بازپرداخت‌شده را نمی‌توان دوباره «پرداخت‌شده» کرد.", "invalid_transition");

        order.Status = status;
        await _db.SaveChangesAsync(ct);
        return order.ToViewModel();
    }

    public async Task<PagedViewModel<InvoiceViewModel>> InvoicesMineAsync(Guid userId, ListQuery query, CancellationToken ct = default)
    {
        var q = _db.Invoices.AsNoTracking()
            .Include(i => i.Order)
            .Where(i => i.Order!.UserId == userId)
            .OrderByDescending(i => i.IssuedAt);

        return await q.ToPagedAsync(query, i => i.ToViewModel(), ct);
    }

    public async Task<InvoiceViewModel> GetInvoiceAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.AsNoTracking()
            .Include(i => i.Order).ThenInclude(o => o!.Items)
            .Include(i => i.Order).ThenInclude(o => o!.Payment)
            .Include(i => i.Order).ThenInclude(o => o!.User)
            .FirstOrDefaultAsync(i => i.Id == id, ct)
            ?? throw new NotFoundException("فاکتور", id);

        if (!isAdmin && invoice.Order?.UserId != userId)
            throw new ForbiddenException("این فاکتور به شما تعلق ندارد.");

        return invoice.ToViewModel() with { Order = invoice.Order?.ToViewModel() };
    }

    /* ------------------------------ شماره‌گذاری ------------------------------ */

    private async Task<string> NextOrderNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Orders.CountAsync(ct) + 1;

        for (var i = 0; i < 1000; i++)
        {
            var candidate = $"NW-{year}-{count + i:D4}";
            if (!await _db.Orders.AnyAsync(o => o.OrderNumber == candidate, ct)) return candidate;
        }

        return $"NW-{year}-{Guid.NewGuid():N}"[..16];
    }

    private async Task<string> NextInvoiceNumberAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Invoices.CountAsync(ct) + 1;

        for (var i = 0; i < 1000; i++)
        {
            var candidate = $"INV-{year}-{count + i:D4}";
            if (!await _db.Invoices.AnyAsync(x => x.InvoiceNumber == candidate, ct)) return candidate;
        }

        return $"INV-{year}-{Guid.NewGuid():N}"[..16];
    }

}

/// <summary>سازندهٔ کلیدِ لایسنس به شکلِ <c>XXXX-XXXX-XXXX-XXXX</c>.</summary>
internal static class LicenseKeyGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // بدونِ I/O/0/1 برایِ خوانایی

    public static string New()
    {
        var chars = new char[19];
        var pos = 0;
        for (var group = 0; group < 4; group++)
        {
            for (var i = 0; i < 4; i++)
                chars[pos++] = Alphabet[Random.Shared.Next(Alphabet.Length)];
            if (group < 3) chars[pos++] = '-';
        }
        return new string(chars);
    }
}
