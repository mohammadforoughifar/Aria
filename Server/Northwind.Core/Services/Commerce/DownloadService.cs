using Microsoft.EntityFrameworkCore;
using Northwind.Core.Mapping;
using Northwind.Core.Services.Common;
using Northwind.Core.Models.Commerce;
using Northwind.Core.Models.Common;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Enums;
using Northwind.Data.Persistence;

namespace Northwind.Core.Services.Commerce;

public interface IDownloadService
{
    Task<IReadOnlyList<AvailableDownloadViewModel>> AvailableAsync(Guid? userId, CancellationToken ct = default);
    Task<PagedViewModel<DownloadViewModel>> HistoryAsync(Guid userId, ListQuery query, CancellationToken ct = default);
    Task<DownloadViewModel> RecordAsync(RecordDownloadInput input, Guid? userId, CancellationToken ct = default);
}

public sealed class DownloadService : IDownloadService
{
    private readonly NorthwindDbContext _db;

    public DownloadService(NorthwindDbContext db) => _db = db;

    public async Task<IReadOnlyList<AvailableDownloadViewModel>> AvailableAsync(Guid? userId, CancellationToken ct = default)
    {
        /* حقِ دانلود از دو جا می‌آید:
             ۱) سفارشِ پرداخت‌شده‌ای که این محصول در آن باشد — چون لایسنس
                دیگر در این سایت صادر نمی‌شود و خرید باید دسترسی بدهد.
             ۲) لایسنسِ فعالی که از سامانهٔ صدور برای این کاربر ثبت شده باشد. */
        HashSet<Guid> entitled;
        if (userId is null)
        {
            entitled = new HashSet<Guid>();
        }
        else
        {
            var purchased = await _db.OrderItems
                .Where(i => i.Order!.UserId == userId
                         && i.Order.Payment != null
                         && i.Order.Payment.Status == PaymentStatus.Succeeded)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync(ct);

            var licensed = await _db.Licenses
                .Where(l => l.UserId == userId && l.Status == LicenseStatus.Active)
                .Select(l => l.ProductId)
                .Distinct()
                .ToListAsync(ct);

            entitled = purchased.Concat(licensed).ToHashSet();
        }

        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Versions)
            .Where(p => p.IsActive && p.Versions.Any())
            .OrderByDescending(p => p.Popularity)
            .ToListAsync(ct);

        return products.Select(p => new AvailableDownloadViewModel(
            p.Id, p.Name, p.Slug, p.AccentColor,
            entitled.Contains(p.Id),
            p.Versions.OrderByDescending(v => v.ReleasedAt).Select(v => new AvailableVersionViewModel(
                v.Id, v.Version, v.ReleasedAt, v.Platform, v.SizeMb,
                $"{p.Slug}-{v.Version}-{v.Platform.ToLowerInvariant()}.exe",
                v.InstallerUrl, v.Notes)).ToList()))
            .ToList();
    }

    public async Task<PagedViewModel<DownloadViewModel>> HistoryAsync(Guid userId, ListQuery query, CancellationToken ct = default)
    {
        var q = _db.Downloads.AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DownloadedAt);

        return await q.ToPagedAsync(query, d => d.ToViewModel(), ct);
    }

    public async Task<DownloadViewModel> RecordAsync(RecordDownloadInput input, Guid? userId, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == input.ProductId, ct)
            ?? throw new NotFoundException("محصول", input.ProductId);

        var version = input.VersionId is null
            ? await _db.ProductVersions.Where(v => v.ProductId == product.Id)
                .OrderByDescending(v => v.ReleasedAt).FirstOrDefaultAsync(ct)
            : await _db.ProductVersions.FirstOrDefaultAsync(v => v.Id == input.VersionId, ct)
              ?? throw new NotFoundException("نسخه", input.VersionId);

        // همان قاعدهٔ AvailableAsync: خریدِ پرداخت‌شده یا لایسنسِ فعال.
        var licensed = userId is not null && await _db.Licenses
            .AnyAsync(l => l.UserId == userId && l.ProductId == product.Id && l.Status == LicenseStatus.Active, ct);

        if (!licensed && userId is not null)
        {
            licensed = await _db.OrderItems
                .AnyAsync(i => i.ProductId == product.Id
                            && i.Order!.UserId == userId
                            && i.Order.Payment != null
                            && i.Order.Payment.Status == PaymentStatus.Succeeded, ct);
        }

        var download = new Download
        {
            ProductId = product.Id,
            UserId = userId,
            VersionId = version?.Id,
            Version = version?.Version ?? product.Version,
            Kind = string.IsNullOrWhiteSpace(input.Kind) ? "installer" : input.Kind,
            FileName = version is null
                ? $"{product.Slug}-{product.Version}.exe"
                : $"{product.Slug}-{version.Version}-{version.Platform.ToLowerInvariant()}.exe",
            SizeMb = version?.SizeMb ?? 0,
            RequiresLicense = !licensed,
            DownloadedAt = DateTime.UtcNow
        };

        _db.Downloads.Add(download);
        await _db.SaveChangesAsync(ct);

        return await _db.Downloads.AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.Id == download.Id)
            .Select(d => d.ToViewModel())
            .FirstAsync(ct);
    }
}
