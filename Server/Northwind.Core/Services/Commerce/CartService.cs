using Microsoft.EntityFrameworkCore;
using Northwind.Core.Models.Commerce;
using Northwind.Core.Models.Content;
using Northwind.Data.Entities.Cart;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Entities.Content;
using Northwind.Data.Enums;
using Northwind.Data.Persistence;
using Northwind.Core.Services.Common;

namespace Northwind.Core.Services.Commerce;

public interface ICartService
{
    Task<CartViewModel> GetAsync(string sessionKey, Guid? userId, string? discountCode = null, CancellationToken ct = default);
    Task<CartViewModel> AddAsync(AddToCartInput input, Guid? userId, CancellationToken ct = default);
    Task<CartViewModel> UpdateQuantityAsync(string sessionKey, Guid? userId, Guid itemId, int quantity, string? discountCode = null, CancellationToken ct = default);
    Task<CartViewModel> RemoveAsync(string sessionKey, Guid? userId, Guid itemId, CancellationToken ct = default);
    Task ClearAsync(string sessionKey, Guid? userId, CancellationToken ct = default);

    /// <summary>کدِ تخفیف را بررسی و مبالغِ سبد را با آن محاسبه می‌کند (بدونِ ذخیره).</summary>
    Task<CartViewModel> PreviewDiscountAsync(string sessionKey, Guid? userId, string code, CancellationToken ct = default);
}

public sealed class CartService : ICartService
{
    private readonly NorthwindDbContext _db;

    public CartService(NorthwindDbContext db) => _db = db;

    public async Task<CartViewModel> GetAsync(string sessionKey, Guid? userId, string? discountCode = null, CancellationToken ct = default)
    {
        var items = await LoadItemsAsync(sessionKey, userId, ct);
        return await BuildAsync(sessionKey, userId, items, discountCode, ct);
    }

    public async Task<CartViewModel> AddAsync(AddToCartInput input, Guid? userId, CancellationToken ct = default)
    {
        var sessionKey = ResolveSession(input.SessionKey, userId);

        var plan = await _db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Id == input.PlanId, ct)
            ?? throw new NotFoundException("طرح", input.PlanId);

        var existing = await _db.CartItems.FirstOrDefaultAsync(c =>
            c.PlanId == input.PlanId && c.SessionKey == sessionKey && c.UserId == userId, ct);

        if (existing is not null)
        {
            existing.Quantity = Math.Min(99, existing.Quantity + input.Quantity);
        }
        else
        {
            _db.CartItems.Add(new CartItem
            {
                SessionKey = sessionKey,
                UserId = userId,
                PlanId = input.PlanId,
                Quantity = Math.Clamp(input.Quantity, 1, 99)
            });
        }

        await _db.SaveChangesAsync(ct);
        var items = await LoadItemsAsync(sessionKey, userId, ct);
        return await BuildAsync(sessionKey, userId, items, null, ct);
    }

    public async Task<CartViewModel> UpdateQuantityAsync(string sessionKey, Guid? userId, Guid itemId, int quantity, string? discountCode = null, CancellationToken ct = default)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId, ct)
            ?? throw new NotFoundException("ردیفِ سبد", itemId);

        EnsureOwner(item, sessionKey, userId);

        if (quantity <= 0) _db.CartItems.Remove(item);
        else item.Quantity = Math.Min(99, quantity);

        await _db.SaveChangesAsync(ct);
        var items = await LoadItemsAsync(sessionKey, userId, ct);
        return await BuildAsync(sessionKey, userId, items, discountCode, ct);
    }

    public async Task<CartViewModel> RemoveAsync(string sessionKey, Guid? userId, Guid itemId, CancellationToken ct = default)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId, ct)
            ?? throw new NotFoundException("ردیفِ سبد", itemId);

        EnsureOwner(item, sessionKey, userId);
        _db.CartItems.Remove(item);
        await _db.SaveChangesAsync(ct);

        var items = await LoadItemsAsync(sessionKey, userId, ct);
        return await BuildAsync(sessionKey, userId, items, null, ct);
    }

    public async Task ClearAsync(string sessionKey, Guid? userId, CancellationToken ct = default)
    {
        var items = await LoadItemsAsync(sessionKey, userId, ct);
        if (items.Count == 0) return;

        _db.CartItems.RemoveRange(items);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CartViewModel> PreviewDiscountAsync(string sessionKey, Guid? userId, string code, CancellationToken ct = default)
    {
        var items = await LoadItemsAsync(sessionKey, userId, ct);
        var cart = await BuildAsync(sessionKey, userId, items, null, ct);

        var discount = await ValidateDiscountAsync(code, cart.SubtotalToman, ct);

        var discountToman = discount is null ? 0m : Money.Round(cart.SubtotalToman * (discount.PercentOff / 100m));
        var tax = await TaxAsync(cart.SubtotalToman - discountToman, ct);

        return cart with
        {
            DiscountToman = discountToman,
            TaxToman = tax,
            TotalToman = cart.SubtotalToman - discountToman + tax,
            DiscountCode = discount?.Code
        };
    }

    /* ------------------------------ درونی ------------------------------ */

    private static string ResolveSession(string? sessionKey, Guid? userId)
        => userId is not null ? $"user:{userId}" : (string.IsNullOrWhiteSpace(sessionKey) ? "guest" : sessionKey.Trim());

    private static void EnsureOwner(CartItem item, string sessionKey, Guid? userId)
    {
        var matches = item.UserId == userId && (item.SessionKey == sessionKey || item.UserId is not null);
        if (!matches) throw new ForbiddenException("این ردیف به سبدِ شما تعلق ندارد.");
    }

    private async Task<List<CartItem>> LoadItemsAsync(string sessionKey, Guid? userId, CancellationToken ct)
        => await _db.CartItems
            .Include(c => c.Plan).ThenInclude(p => p!.Product)
            .Where(c => userId != null
                ? c.UserId == userId || c.SessionKey == sessionKey
                : c.UserId == null && c.SessionKey == sessionKey)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);

    private async Task<CartViewModel> BuildAsync(string sessionKey, Guid? userId, List<CartItem> items, string? discountCode, CancellationToken ct)
    {
        var rows = items
            .Where(i => i.Plan?.Product is not null)
            .Select(i => new CartItemViewModel(
                i.Id, i.PlanId, i.Quantity, i.Plan!.ProductId,
                i.Plan.Product!.Name, i.Plan.Product.Slug, i.Plan.Name,
                i.Plan.Tier, i.Plan.BillingType, i.Plan.Product.AccentColor,
                i.Plan.PriceToman, i.Plan.PriceToman * i.Quantity))
            .ToList();

        var subtotal = rows.Sum(r => r.LineTotalToman);

        DiscountCode? discount = null;
        if (!string.IsNullOrWhiteSpace(discountCode))
            discount = await ValidateDiscountAsync(discountCode, subtotal, ct);

        var discountToman = discount is null ? 0m : Money.Round(subtotal * (discount.PercentOff / 100m));
        var tax = await TaxAsync(subtotal - discountToman, ct);

        return new CartViewModel(
            CartIdFor(sessionKey, userId),
            rows,
            rows.Sum(r => r.Quantity),
            subtotal,
            discountToman,
            tax,
            subtotal - discountToman + tax,
            discount?.Code,
            "IRT");
    }

    private async Task<DiscountCode?> ValidateDiscountAsync(string code, decimal subtotal, CancellationToken ct)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var discount = await _db.DiscountCodes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == normalized, ct)
            ?? throw new BusinessRuleException("این کدِ تخفیف وجود ندارد.", "discount_not_found");

        if (!discount.IsActive)
            throw new BusinessRuleException("این کدِ تخفیف غیرفعال شده است.", "discount_inactive");

        if (discount.ExpiresAt is not null && discount.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("این کدِ تخفیف منقضی شده است.", "discount_expired");

        if (discount.MaxRedemptions > 0 && discount.TimesRedeemed >= discount.MaxRedemptions)
            throw new BusinessRuleException("ظرفیتِ این کدِ تخفیف پر شده است.", "discount_exhausted");

        if (subtotal <= 0)
            throw new BusinessRuleException("برایِ اعمالِ کد، سبد نباید خالی باشد.", "cart_empty");

        return discount;
    }

    private async Task<decimal> TaxAsync(decimal taxableToman, CancellationToken ct)
    {
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var bps = settings?.TaxRateBps ?? 0;
        return Money.Round(taxableToman * (bps / 10_000m));
    }

    /// <summary>شناسهٔ نمایشیِ سبد — سبد موجودیتِ پایدار نیست، پس شناسه از کلیدِ نشست ساخته می‌شود.</summary>
    internal static Guid CartIdFor(string sessionKey, Guid? userId)
    {
        var seed = $"{sessionKey}|{userId}";
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(seed));
        return new Guid(bytes);
    }
}
