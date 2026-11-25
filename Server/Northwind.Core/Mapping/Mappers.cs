using Microsoft.EntityFrameworkCore;
using Northwind.Core.Models.Account;
using Northwind.Core.Models.Catalog;
using Northwind.Core.Models.Commerce;
using Northwind.Core.Models.Common;
using Northwind.Core.Models.Content;
using Northwind.Core.Models.Support;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Entities.Content;
using Northwind.Data.Entities.Identity;
using Northwind.Data.Entities.Support;

namespace Northwind.Core.Mapping;

/// <summary>
/// نگاشتِ موجودیت به ویو‌مدل. تنها جایِ مجازِ این تبدیل است تا سرویس‌ها
/// فقط منطقِ کسب‌وکار داشته باشند.
/// </summary>
public static class Mappers
{
    /* ------------------------------ مشترک ------------------------------ */

    /// <summary>صفحه‌بندی + نگاشت در یک قدم.</summary>
    public static async Task<PagedViewModel<TVm>> ToPagedAsync<TEntity, TVm>(
        this IQueryable<TEntity> query, ListQuery q, Func<TEntity, TVm> map, CancellationToken ct = default)
    {
        q.Normalize();
        var total = await query.CountAsync(ct);
        var rows = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        var pages = (int)Math.Ceiling(total / (double)q.PageSize);
        return new PagedViewModel<TVm>(rows.Select(map).ToList(),
            new PagedMetaViewModel(q.Page, q.PageSize, total, pages == 0 ? 1 : pages));
    }

    /* ------------------------------- دسته ------------------------------- */

    public static CategoryViewModel ToViewModel(this Category c, int productCount) =>
        new(c.Id, c.Name, c.Slug, c.Description, c.Icon, productCount);

    /* ------------------------------ محصول ------------------------------ */

    public static ProductListItemViewModel ToListItem(this Product p, decimal ratingAvg, int ratingCount, int planCount) =>
        new(p.Id, p.Slug, p.Name, p.Tagline, p.Type, p.Platform, p.Version, p.BasePriceToman,
            ratingAvg, ratingCount, p.Popularity, p.HeroImage, p.AccentColor, p.IsFeatured, p.IsActive,
            p.CreatedAt, p.Category?.Name, p.Category?.Slug, planCount);

    public static ProductDetailViewModel ToDetail(this Product p, decimal ratingAvg, int ratingCount,
        IEnumerable<Review> reviews) =>
        new(p.Id, p.Slug, p.Name, p.Tagline, p.Description, p.Type, p.Audience, p.Platform, p.Version,
            p.BasePriceToman, ratingAvg, ratingCount, p.Popularity, p.AccentColor, p.HeroImage,
            p.IsActive, p.IsFeatured, p.TrialDays, p.CreatedAt,
            p.CategoryId, p.Category?.Name ?? "", p.Category?.Slug ?? "",
            p.Features, p.Requirements, p.Gallery, p.DemoVideoUrl,
            p.Plans.OrderBy(x => x.SortOrder).Select(x => x.ToViewModel()).ToList(),
            p.Versions.OrderByDescending(x => x.ReleasedAt).Select(x => x.ToViewModel()).ToList(),
            reviews.Select(r => r.ToViewModel(p.Name)).ToList());

    public static PlanViewModel ToViewModel(this Plan p) =>
        new(p.Id, p.Name, p.Tier, p.BillingType, p.PriceToman, p.Seats, p.Description, p.IsPopular, p.LicenseMonths);

    public static PlanDetailViewModel ToDetail(this Plan p) =>
        new(p.Id, p.ProductId, p.Name, p.Tier, p.BillingType, p.PriceToman, p.Seats,
            p.Description, p.IsPopular, p.LicenseMonths, p.Features);

    public static ProductVersionViewModel ToViewModel(this ProductVersion v) =>
        new(v.Id, v.Version, v.ReleasedAt, v.Notes, v.InstallerUrl, v.SizeMb, v.Platform);

    public static ReviewViewModel ToViewModel(this Review r, string? productName = null) =>
        new(r.Id, r.ProductId, productName, r.AuthorName, r.AuthorCompany, r.Rating, r.Title, r.Body,
            r.Status, r.CreatedAt);

    /* ------------------------------- تجارت ------------------------------- */

    public static OrderItemViewModel ToViewModel(this OrderItem i) =>
        new(i.Id, i.ProductId, i.PlanId, i.ProductName, i.PlanName, i.BillingType,
            i.UnitPriceToman, i.Quantity, i.LineTotalToman);

    public static PaymentViewModel ToViewModel(this Payment p) =>
        new(p.Id, p.OrderId, p.Provider, p.Method, p.AmountToman, p.Status, p.TransactionRef, p.PaidAt);

    public static InvoiceViewModel ToViewModel(this Invoice i) =>
        new(i.Id, i.InvoiceNumber, i.OrderId, i.Status, i.IssuedAt, i.DueAt, i.TotalToman, i.TaxToman);

    public static OrderViewModel ToViewModel(this Order o) =>
        new(o.Id, o.OrderNumber, o.UserId, o.User?.FullName, o.User?.Email, o.User?.Company,
            o.Status, o.SubtotalToman, o.DiscountToman, o.TaxToman, o.TotalToman, o.Currency,
            o.DiscountCode, o.PlacedAt, o.BillingName, o.BillingCompany, o.BillingEmail,
            o.BillingCountry, o.Notes,
            o.Items.Select(x => x.ToViewModel()).ToList(),
            o.Payment?.ToViewModel(), o.Invoice?.ToViewModel());

    public static LicenseViewModel ToViewModel(this License l) =>
        new(l.Id, l.LicenseKey, l.UserId, l.User?.FullName, l.User?.Email,
            l.ProductId, l.Product?.Name ?? "", l.Product?.Slug ?? "", l.Plan?.Name,
            l.Seats, l.ActivationsUsed, l.Status, l.IssuedAt, l.ExpiresAt);

    public static DownloadViewModel ToViewModel(this Download d) =>
        new(d.Id, d.ProductId, d.Product?.Name ?? "", d.Product?.Slug ?? "", d.VersionId,
            d.Version, d.Kind, d.FileName, d.SizeMb, d.DownloadedAt, d.RequiresLicense, d.UserId);

    public static DiscountCodeViewModel ToViewModel(this DiscountCode d) =>
        new(d.Id, d.Code, d.Description, d.PercentOff, d.MaxRedemptions, d.TimesRedeemed,
            d.ExpiresAt, d.IsActive, d.CreatedAt);

    /* ------------------------------- پشتیبانی ------------------------------- */

    public static TicketMessageViewModel ToViewModel(this TicketMessage m) =>
        new(m.Id, m.TicketId, m.AuthorName, m.IsStaff, m.Body, m.CreatedAt);

    public static TicketViewModel ToViewModel(this Ticket t) =>
        new(t.Id, t.Reference, t.UserId, t.User?.FullName, t.Product?.Name, t.Subject, t.Category,
            t.Priority, t.Status, t.Environment, t.CreatedAt, t.LastActivityAt, t.Messages.Count);

    public static TicketDetailViewModel ToDetail(this Ticket t) =>
        new(t.Id, t.Reference, t.UserId, t.User?.FullName, t.ProductId, t.Product?.Name,
            t.Subject, t.Category, t.Priority, t.Status, t.Environment, t.CreatedAt, t.LastActivityAt,
            t.Messages.OrderBy(m => m.CreatedAt).Select(m => m.ToViewModel()).ToList());

    public static NotificationViewModel ToViewModel(this Notification n) =>
        new(n.Id, n.UserId, n.Title, n.Body, n.Kind, n.IsRead, n.CreatedAt, n.Link);

    /* ------------------------------- محتوا ------------------------------- */

    public static ArticleViewModel ToViewModel(this Article a) =>
        new(a.Id, a.Slug, a.Title, a.Excerpt, a.Body, a.Category, a.IsPublished, a.PublishedAt, a.ReadMinutes);

    public static TestimonialViewModel ToViewModel(this Testimonial t) =>
        new(t.Id, t.AuthorName, t.AuthorRole, t.Company, t.Quote, t.Rating);

    public static FaqViewModel ToViewModel(this Faq f) => new(f.Id, f.Question, f.Answer);

    public static SiteSettingsViewModel ToViewModel(this SiteSettings s) =>
        new(s.SiteName, s.SupportEmail, s.SalesEmail, s.SupportPhone, s.HeroHeadline, s.HeroSubheading,
            s.TaxRateBps, s.TrialDays, s.MaintenanceMode, s.Announcement ?? "");

    public static DemoRequestViewModel ToViewModel(this DemoRequest d) =>
        new(d.Id, d.ProductId, d.Product?.Name, d.Name, d.Email, d.Company, d.CompanySize,
            d.Phone, d.Message, d.Status, d.CreatedAt);

    public static ContactMessageViewModel ToViewModel(this ContactMessage c) =>
        new(c.Id, c.Name, c.Email, c.Company, c.Topic, c.Message, c.IsRead, c.CreatedAt);

    /* ------------------------------- کاربر ------------------------------- */

    public static UserViewModel ToViewModel(this User u) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country, u.CreatedAt, u.LastLoginAt);

    public static CustomerViewModel ToCustomer(this User u, int orderCount, decimal totalSpent, int licenseCount) =>
        new(u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country, u.CreatedAt, u.LastLoginAt,
            orderCount, totalSpent, licenseCount);
}
