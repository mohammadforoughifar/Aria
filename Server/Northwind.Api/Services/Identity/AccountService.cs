using Microsoft.EntityFrameworkCore;
using Northwind.Api.Mapping;
using Northwind.Api.Services.Common;
using Northwind.Api.Services.Security;
using Northwind.Api.ViewModels.Account;
using Northwind.Api.ViewModels.Commerce;
using Northwind.Api.ViewModels.Common;
using Northwind.Api.ViewModels.Content;
using Northwind.Domain.Entities.Identity;
using Northwind.Domain.Enums;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Identity;

public interface IAccountService
{
    Task<AuthResultViewModel> RegisterAsync(RegisterInput input, CancellationToken ct = default);
    Task<AuthResultViewModel> LoginAsync(LoginInput input, CancellationToken ct = default);

    Task<UserViewModel> MeAsync(Guid userId, CancellationToken ct = default);
    Task<UserViewModel> UpdateProfileAsync(Guid userId, UpdateProfileInput input, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordInput input, CancellationToken ct = default);

    Task<CustomerOverviewViewModel> OverviewAsync(Guid userId, CancellationToken ct = default);

    Task<PagedViewModel<CustomerViewModel>> ListCustomersAsync(ListQuery query, CancellationToken ct = default);
    Task<CustomerViewModel> UpdateCustomerAsync(Guid id, UpdateCustomerInput input, CancellationToken ct = default);
}

public sealed class AccountService : IAccountService
{
    private readonly NorthwindDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public AccountService(NorthwindDbContext db, IPasswordHasher hasher, ITokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    /* ------------------------------ احراز هویت ------------------------------ */

    public async Task<AuthResultViewModel> RegisterAsync(RegisterInput input, CancellationToken ct = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        if (existing is not null)
        {
            // حسابی که با خریدِ مهمان ساخته شده ولی هنوز کسی واردش نشده است،
            // «بی‌صاحب» است. اگر همان ایمیل ثبت‌نام کند، همین حساب به او داده
            // می‌شود تا سفارش‌ها، فاکتورها و لایسنس‌هایِ خریدِ مهمان از دست نرود.
            var claimable = existing.Role == Role.Customer && existing.LastLoginAt is null;

            if (claimable)
            {
                existing.FullName = input.FullName.Trim();
                existing.PasswordHash = _hasher.Hash(input.Password);
                existing.Phone = existing.Phone ?? input.Phone;
                existing.Company = existing.Company ?? input.Company;
                existing.Country = existing.Country ?? input.Country;
                existing.AccountType = input.AccountType;
                existing.EconomicCode = existing.EconomicCode ?? input.EconomicCode;
                existing.NationalId = existing.NationalId ?? input.NationalId;

                await _db.SaveChangesAsync(ct);

                var (claimToken, claimExpires) = _tokens.Create(existing);
                return new AuthResultViewModel(existing.ToViewModel(), claimToken, claimExpires);
            }

            throw new ConflictException("این ایمیل قبلاً ثبت شده است. وارد شوید یا رمزتان را بازیابی کنید.", "email_taken");
        }

        var user = new User
        {
            Email = email,
            FullName = input.FullName.Trim(),
            PasswordHash = _hasher.Hash(input.Password),
            Phone = input.Phone,
            Company = input.Company,
            Country = input.Country,
            AccountType = input.AccountType,
            EconomicCode = input.EconomicCode,
            NationalId = input.NationalId,
            Role = Role.Customer
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokens.Create(user);
        return new AuthResultViewModel(user.ToViewModel(), token, expiresAt);
    }

    public async Task<AuthResultViewModel> LoginAsync(LoginInput input, CancellationToken ct = default)
    {
        var email = input.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email, ct);

        // پیامِ یکسان برایِ «کاربر نیست» و «رمز غلط» — جلوگیری از شناساییِ ایمیل‌هایِ ثبت‌شده.
        if (user is null || !_hasher.Verify(input.Password, user.PasswordHash))
            throw new BusinessRuleException("ایمیل یا رمزِ عبور درست نیست.", "invalid_credentials");

        if (user.Locked)
            throw new ForbiddenException("حسابِ شما قفل شده است. با پشتیبانی تماس بگیرید.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var (token, expiresAt) = _tokens.Create(user);
        return new AuthResultViewModel(user.ToViewModel(), token, expiresAt);
    }

    /* ------------------------------ پروفایل ------------------------------ */

    public async Task<UserViewModel> MeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("کاربر", userId);

        return user.ToViewModel();
    }

    public async Task<UserViewModel> UpdateProfileAsync(Guid userId, UpdateProfileInput input, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("کاربر", userId);

        user.FullName = input.FullName.Trim();
        user.Phone = input.Phone;
        user.Company = input.Company;
        user.Country = input.Country;
        user.EconomicCode = input.EconomicCode;
        user.NationalId = input.NationalId;

        await _db.SaveChangesAsync(ct);
        return user.ToViewModel();
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordInput input, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("کاربر", userId);

        if (!_hasher.Verify(input.CurrentPassword, user.PasswordHash))
            throw new BusinessRuleException("رمزِ فعلی درست نیست.", "wrong_password");

        if (_hasher.Verify(input.NewPassword, user.PasswordHash))
            throw new BadRequestException("رمزِ جدید با رمزِ فعلی یکسان است.", "same_password");

        user.PasswordHash = _hasher.Hash(input.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    /* ------------------------------ داشبورد ------------------------------ */

    public async Task<CustomerOverviewViewModel> OverviewAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var inThirtyDays = now.AddDays(30);

        var orders = await _db.Orders.AsNoTracking()
            .Include(o => o.Items).Include(o => o.Payment).Include(o => o.Invoice)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync(ct);

        var licenses = await _db.Licenses.AsNoTracking()
            .Include(l => l.Product).Include(l => l.Plan).Include(l => l.User)
            .Where(l => l.UserId == userId)
            .ToListAsync(ct);

        var openTickets = await _db.Tickets.CountAsync(t => t.UserId == userId
            && (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress
                || t.Status == TicketStatus.WaitingCustomer), ct);

        var downloads = await _db.Downloads.CountAsync(d => d.UserId == userId, ct);

        var outstanding = await _db.Invoices.AsNoTracking()
            .Include(i => i.Order)
            .Where(i => i.Order!.UserId == userId && i.Status == InvoiceStatus.Issued)
            .SumAsync(i => (decimal?)i.TotalToman, ct) ?? 0m;

        var activeLicenses = licenses.Where(l => l.Status == LicenseStatus.Active).ToList();
        var expiring = activeLicenses
            .Where(l => l.ExpiresAt is not null && l.ExpiresAt <= inThirtyDays)
            .OrderBy(l => l.ExpiresAt)
            .ToList();

        // سریِ شش‌ماههٔ خرید — رویِ حجمِ این داده، محاسبه در حافظه ساده‌تر و قابلِ اتکاست.
        var spendSeries = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-5 + i))
            .Select(month => new RevenuePointViewModel(
                FormatMonth(month),
                orders.Where(o => o.PlacedAt.Year == month.Year && o.PlacedAt.Month == month.Month
                                  && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                      .Sum(o => o.TotalToman)))
            .ToList();

        return new CustomerOverviewViewModel(
            orders.Count,
            activeLicenses.Count,
            expiring.Count,
            openTickets,
            orders.Where(o => o.Status is OrderStatus.Paid or OrderStatus.Completed).Sum(o => o.TotalToman),
            downloads,
            outstanding,
            orders.Take(5).Select(o => o.ToViewModel()).ToList(),
            expiring.Take(5).Select(l => l.ToViewModel()).ToList(),
            spendSeries);
    }

    /* --------------------------- مدیریتِ مشتریان --------------------------- */

    public async Task<PagedViewModel<CustomerViewModel>> ListCustomersAsync(ListQuery query, CancellationToken ct = default)
    {
        query.Normalize();

        var q = _db.Users.AsNoTracking();

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(u => EF.Functions.Like(u.Email, $"%{s}%")
                          || EF.Functions.Like(u.FullName, $"%{s}%")
                          || EF.Functions.Like(u.Company!, $"%{s}%"));
        }

        q = q.OrderByDescending(u => u.CreatedAt);

        var total = await q.CountAsync(ct);
        var users = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);
        var ids = users.Select(u => u.Id).ToList();

        var orderStats = await _db.Orders.AsNoTracking()
            .Where(o => ids.Contains(o.UserId))
            .GroupBy(o => o.UserId)
            .Select(g => new
            {
                Id = g.Key,
                Count = g.Count(),
                Spent = g.Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed)
                         .Sum(o => o.TotalToman)
            })
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        var licenseCounts = await _db.Licenses.AsNoTracking()
            .Where(l => ids.Contains(l.UserId))
            .GroupBy(l => l.UserId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var items = users.Select(u =>
        {
            var stats = orderStats.GetValueOrDefault(u.Id);
            return u.ToCustomer(stats?.Count ?? 0, stats?.Spent ?? 0, licenseCounts.GetValueOrDefault(u.Id));
        }).ToList();

        var pages = (int)Math.Ceiling(total / (double)query.PageSize);
        return new PagedViewModel<CustomerViewModel>(items,
            new PagedMetaViewModel(query.Page, query.PageSize, total, pages == 0 ? 1 : pages));
    }

    public async Task<CustomerViewModel> UpdateCustomerAsync(Guid id, UpdateCustomerInput input, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundException("کاربر", id);

        // مدیرِ آخر نباید نقشش را از دست بدهد، وگرنه کسی برایِ مدیریت نمی‌ماند.
        if (user.Role == Role.Admin && input.Role != Role.Admin)
        {
            var admins = await _db.Users.CountAsync(u => u.Role == Role.Admin && !u.Locked, ct);
            if (admins <= 1)
                throw new ConflictException("این تنها مدیرِ فعالِ سامانه است و نقشش تغییر نمی‌کند.", "last_admin");
        }

        user.Role = input.Role;
        user.Locked = input.Locked;

        await _db.SaveChangesAsync(ct);

        var orders = await _db.Orders.Where(o => o.UserId == id).ToListAsync(ct);
        var licenseCount = await _db.Licenses.CountAsync(l => l.UserId == id, ct);

        return user.ToCustomer(
            orders.Count,
            orders.Where(o => o.Status is OrderStatus.Paid or OrderStatus.Completed).Sum(o => o.TotalToman),
            licenseCount);
    }

    private static string FormatMonth(DateTime date) => $"{date:yyyy-MM}";
}
