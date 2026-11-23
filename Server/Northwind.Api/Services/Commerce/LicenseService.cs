using Microsoft.EntityFrameworkCore;
using Northwind.Api.Mapping;
using Northwind.Api.Services.Common;
using Northwind.Api.ViewModels.Commerce;
using Northwind.Api.ViewModels.Common;
using Northwind.Domain.Entities.Commerce;
using Northwind.Domain.Enums;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Commerce;

public interface ILicenseService
{
    Task<PagedViewModel<LicenseViewModel>> ListMineAsync(Guid userId, ListQuery query, LicenseStatus? status = null, CancellationToken ct = default);
    Task<PagedViewModel<LicenseViewModel>> AdminListAsync(ListQuery query, LicenseStatus? status, CancellationToken ct = default);
    Task<LicenseViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<LicenseViewModel> IssueAsync(IssueLicenseInput input, CancellationToken ct = default);
    Task<LicenseViewModel> ActionAsync(Guid id, LicenseActionInput input, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<int> ExpiringCountAsync(Guid? userId, int days, CancellationToken ct = default);
}

public sealed class LicenseService : ILicenseService
{
    private readonly NorthwindDbContext _db;

    public LicenseService(NorthwindDbContext db) => _db = db;

    public async Task<PagedViewModel<LicenseViewModel>> ListMineAsync(Guid userId, ListQuery query,
        LicenseStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.Licenses.AsNoTracking()
            .Include(l => l.Product).Include(l => l.Plan).Include(l => l.User)
            .Where(l => l.UserId == userId);

        if (status is not null) q = q.Where(l => l.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(l => EF.Functions.Like(l.LicenseKey, $"%{s}%")
                          || EF.Functions.Like(l.Product!.Name, $"%{s}%"));
        }

        return await q.OrderByDescending(l => l.IssuedAt)
                      .ToPagedAsync(query, l => l.ToViewModel(), ct);
    }

    public async Task<PagedViewModel<LicenseViewModel>> AdminListAsync(ListQuery query, LicenseStatus? status, CancellationToken ct = default)
    {
        var q = _db.Licenses.AsNoTracking()
            .Include(l => l.Product).Include(l => l.Plan).Include(l => l.User)
            .AsQueryable();

        if (status is not null) q = q.Where(l => l.Status == status);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(l => EF.Functions.Like(l.LicenseKey, $"%{s}%")
                          || EF.Functions.Like(l.User!.Email, $"%{s}%")
                          || EF.Functions.Like(l.Product!.Name, $"%{s}%"));
        }

        q = q.OrderByDescending(l => l.IssuedAt);
        return await q.ToPagedAsync(query, l => l.ToViewModel(), ct);
    }

    public async Task<LicenseViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var license = await _db.Licenses.AsNoTracking()
            .Include(l => l.Product).Include(l => l.Plan).Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("لایسنس", id);

        if (!isAdmin && license.UserId != userId)
            throw new ForbiddenException("این لایسنس به شما تعلق ندارد.");

        return license.ToViewModel();
    }

    public async Task<LicenseViewModel> IssueAsync(IssueLicenseInput input, CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == input.UserId, ct))
            throw new NotFoundException("کاربر", input.UserId);

        if (!await _db.Products.AnyAsync(p => p.Id == input.ProductId, ct))
            throw new NotFoundException("محصول", input.ProductId);

        if (input.PlanId is not null && !await _db.Plans.AnyAsync(p => p.Id == input.PlanId && p.ProductId == input.ProductId, ct))
            throw new BadRequestException("طرح به این محصول تعلق ندارد.", "plan_mismatch");

        var now = DateTime.UtcNow;
        var license = new License
        {
            LicenseKey = await NextKeyAsync(ct),
            UserId = input.UserId,
            ProductId = input.ProductId,
            PlanId = input.PlanId,
            Seats = input.Seats,
            Status = LicenseStatus.Active,
            IssuedAt = now,
            ExpiresAt = input.Months <= 0 ? null : now.AddMonths(input.Months)
        };

        _db.Licenses.Add(license);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(license.Id, license.UserId, true, ct);
    }

    public async Task<LicenseViewModel> ActionAsync(Guid id, LicenseActionInput input, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var license = await _db.Licenses
            .Include(l => l.Product).Include(l => l.Plan).Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new NotFoundException("لایسنس", id);

        if (!isAdmin && license.UserId != userId)
            throw new ForbiddenException("این لایسنس به شما تعلق ندارد.");

        switch (input.Action)
        {
            case "renew":
                if (!isAdmin)
                    throw new ForbiddenException("تمدیدِ لایسنس فقط از سویِ پشتیبانی انجام می‌شود.");

                var from = license.ExpiresAt is not null && license.ExpiresAt > DateTime.UtcNow
                    ? license.ExpiresAt.Value
                    : DateTime.UtcNow;

                license.ExpiresAt = input.Months <= 0 ? null : from.AddMonths(input.Months);
                license.Status = LicenseStatus.Active;
                break;

            case "revoke":
                if (!isAdmin) throw new ForbiddenException("ابطالِ لایسنس فقط از سویِ پشتیبانی انجام می‌شود.");
                license.Status = LicenseStatus.Revoked;
                break;

            case "activate":
                if (license.Status == LicenseStatus.Revoked)
                    throw new ConflictException("لایسنسِ ابطال‌شده فعال نمی‌شود.", "license_revoked");

                if (license.Status == LicenseStatus.Expired ||
                    (license.ExpiresAt is not null && license.ExpiresAt < DateTime.UtcNow))
                    throw new BusinessRuleException("این لایسنس منقضی شده است؛ ابتدا آن را تمدید کنید.", "license_expired");

                if (license.ActivationsUsed >= license.Seats)
                    throw new BusinessRuleException("ظرفیتِ فعال‌سازیِ این لایسنس پر است.", "seats_exhausted");

                license.ActivationsUsed += 1;
                break;

            default:
                throw new BadRequestException("عملیاتِ ناشناخته.", "unknown_action");
        }

        await _db.SaveChangesAsync(ct);
        return license.ToViewModel();
    }

    public async Task<int> ExpiringCountAsync(Guid? userId, int days, CancellationToken ct = default)
    {
        var threshold = DateTime.UtcNow.AddDays(days);
        var q = _db.Licenses.Where(l => l.Status == LicenseStatus.Active
                                     && l.ExpiresAt != null && l.ExpiresAt <= threshold);

        if (userId is not null) q = q.Where(l => l.UserId == userId);
        return await q.CountAsync(ct);
    }

    private async Task<string> NextKeyAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var key = LicenseKeyGenerator.New();
            if (!await _db.Licenses.AnyAsync(l => l.LicenseKey == key, ct)) return key;
        }
        return LicenseKeyGenerator.New();
    }
}
