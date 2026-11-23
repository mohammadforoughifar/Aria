using Microsoft.EntityFrameworkCore;
using Northwind.Api.Mapping;
using Northwind.Api.Services.Common;
using Northwind.Api.ViewModels.Commerce;
using Northwind.Api.ViewModels.Common;
using Northwind.Domain.Entities.Commerce;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Commerce;

public interface IDiscountService
{
    Task<PagedViewModel<DiscountCodeViewModel>> ListAsync(ListQuery query, CancellationToken ct = default);
    Task<DiscountCodeViewModel> CreateAsync(DiscountCodeInput input, CancellationToken ct = default);
    Task<DiscountCodeViewModel> UpdateAsync(Guid id, DiscountCodeInput input, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>بررسیِ کد و برگرداندنِ درصدِ تخفیف برایِ مبلغی مشخص.</summary>
    Task<(string Code, int PercentOff, decimal DiscountToman)> ValidateAsync(string code, decimal subtotalToman, CancellationToken ct = default);
}

public sealed class DiscountService : IDiscountService
{
    private readonly NorthwindDbContext _db;

    public DiscountService(NorthwindDbContext db) => _db = db;

    public async Task<PagedViewModel<DiscountCodeViewModel>> ListAsync(ListQuery query, CancellationToken ct = default)
    {
        var q = _db.DiscountCodes.AsNoTracking().OrderByDescending(d => d.CreatedAt);
        return await q.ToPagedAsync(query, d => d.ToViewModel(), ct);
    }

    public async Task<DiscountCodeViewModel> CreateAsync(DiscountCodeInput input, CancellationToken ct = default)
    {
        var code = input.Code.Trim().ToUpperInvariant();

        if (await _db.DiscountCodes.AnyAsync(d => d.Code.ToUpper() == code, ct))
            throw new ConflictException("این کدِ تخفیف قبلاً ساخته شده است.", "discount_exists");

        var entity = new DiscountCode
        {
            Code = code,
            Description = input.Description,
            PercentOff = input.PercentOff,
            MaxRedemptions = input.MaxRedemptions,
            ExpiresAt = input.ExpiresAt,
            IsActive = input.IsActive
        };

        _db.DiscountCodes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.ToViewModel();
    }

    public async Task<DiscountCodeViewModel> UpdateAsync(Guid id, DiscountCodeInput input, CancellationToken ct = default)
    {
        var entity = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("کدِ تخفیف", id);

        var code = input.Code.Trim().ToUpperInvariant();
        if (await _db.DiscountCodes.AnyAsync(d => d.Code.ToUpper() == code && d.Id != id, ct))
            throw new ConflictException("کدِ دیگری با همین نام وجود دارد.", "discount_exists");

        entity.Code = code;
        entity.Description = input.Description;
        entity.PercentOff = input.PercentOff;
        entity.MaxRedemptions = input.MaxRedemptions;
        entity.ExpiresAt = input.ExpiresAt;
        entity.IsActive = input.IsActive;

        await _db.SaveChangesAsync(ct);
        return entity.ToViewModel();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("کدِ تخفیف", id);

        _db.DiscountCodes.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(string Code, int PercentOff, decimal DiscountToman)> ValidateAsync(string code, decimal subtotalToman, CancellationToken ct = default)
    {
        var normalized = code.Trim().ToUpperInvariant();

        var discount = await _db.DiscountCodes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code.ToUpper() == normalized, ct)
            ?? throw new BusinessRuleException("این کدِ تخفیف وجود ندارد.", "discount_not_found");

        if (!discount.IsActive)
            throw new BusinessRuleException("این کدِ تخفیف غیرفعال است.", "discount_inactive");

        if (discount.ExpiresAt is not null && discount.ExpiresAt < DateTime.UtcNow)
            throw new BusinessRuleException("این کدِ تخفیف منقضی شده است.", "discount_expired");

        if (discount.MaxRedemptions > 0 && discount.TimesRedeemed >= discount.MaxRedemptions)
            throw new BusinessRuleException("ظرفیتِ این کد پر شده است.", "discount_exhausted");

        var amount = Money.Round(subtotalToman * (discount.PercentOff / 100m));
        return (discount.Code, discount.PercentOff, amount);
    }
}
