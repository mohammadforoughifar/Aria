using Microsoft.EntityFrameworkCore;
using Northwind.Api.Mapping;
using Northwind.Api.ViewModels.Content;
using Northwind.Domain.Enums;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Admin;

public interface IAdminService
{
    /// <summary>شاخص‌هایِ پنلِ مدیر در یک درخواست.</summary>
    Task<AdminOverviewViewModel> OverviewAsync(CancellationToken ct = default);
}

public sealed class AdminService : IAdminService
{
    private readonly NorthwindDbContext _db;

    public AdminService(NorthwindDbContext db) => _db = db;

    public async Task<AdminOverviewViewModel> OverviewAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = monthStart.AddMonths(-1);
        var inThirtyDays = now.AddDays(30);

        var totalProducts = await _db.Products.CountAsync(p => p.IsActive, ct);
        var totalCustomers = await _db.Users.CountAsync(u => u.Role == Role.Customer, ct);

        var openTickets = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Open
            || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingCustomer, ct);

        var pendingReviews = await _db.Reviews.CountAsync(r => r.Status == ReviewStatus.Pending, ct);

        var activeLicenses = await _db.Licenses.CountAsync(l => l.Status == LicenseStatus.Active, ct);
        var expiringLicenses = await _db.Licenses.CountAsync(l => l.Status == LicenseStatus.Active
            && l.ExpiresAt != null && l.ExpiresAt <= inThirtyDays, ct);

        var revenueThisMonth = await _db.Orders
            .Where(o => o.PlacedAt >= monthStart && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
            .SumAsync(o => (decimal?)o.TotalToman, ct) ?? 0m;

        var revenueLastMonth = await _db.Orders
            .Where(o => o.PlacedAt >= lastMonthStart && o.PlacedAt < monthStart
                     && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
            .SumAsync(o => (decimal?)o.TotalToman, ct) ?? 0m;

        // ---- سریِ درآمدِ ۱۲ ماه ----
        var since = monthStart.AddMonths(-11);
        var orderRows = await _db.Orders.AsNoTracking()
            .Where(o => o.PlacedAt >= since && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
            .Select(o => new { o.PlacedAt, o.TotalToman })
            .ToListAsync(ct);

        var revenueSeries = Enumerable.Range(0, 12)
            .Select(i => monthStart.AddMonths(-11 + i))
            .Select(month => new RevenuePointViewModel(
                $"{month:yyyy-MM}",
                orderRows.Where(o => o.PlacedAt.Year == month.Year && o.PlacedAt.Month == month.Month)
                         .Sum(o => o.TotalToman)))
            .ToList();

        // ---- پرفروش‌ترین محصولات ----
        var topRows = await _db.OrderItems.AsNoTracking()
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.LineTotalToman) })
            .OrderByDescending(x => x.Revenue)
            .Take(5)
            .ToListAsync(ct);

        var productIds = topRows.Select(t => t.ProductId).ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Slug, p.AccentColor })
            .ToDictionaryAsync(p => p.Id, ct);

        var topProducts = topRows
            .Where(t => products.ContainsKey(t.ProductId))
            .Select(t =>
            {
                var p = products[t.ProductId];
                return new TopProductViewModel(p.Id, p.Name, p.Slug, t.Units, t.Revenue, p.AccentColor);
            })
            .ToList();

        // ---- آخرین سفارش‌ها و تیکت‌ها ----
        var recentOrders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items).Include(o => o.Payment).Include(o => o.Invoice).Include(o => o.User)
            .OrderByDescending(o => o.PlacedAt)
            .Take(5)
            .ToListAsync(ct);

        var recentTickets = await _db.Tickets.AsNoTracking()
            .Include(t => t.User).Include(t => t.Product).Include(t => t.Messages)
            .OrderByDescending(t => t.LastActivityAt)
            .Take(5)
            .ToListAsync(ct);

        return new AdminOverviewViewModel(
            totalProducts, totalCustomers, openTickets, pendingReviews,
            activeLicenses, expiringLicenses, revenueThisMonth, revenueLastMonth,
            revenueSeries, topProducts,
            recentOrders.Select(o => o.ToViewModel()).ToList(),
            recentTickets.Select(t => t.ToViewModel()).ToList());
    }
}
