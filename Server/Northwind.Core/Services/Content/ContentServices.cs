using Microsoft.EntityFrameworkCore;
using Northwind.Core.Mapping;
using Northwind.Core.Services.Common;
using Northwind.Core.Models.Common;
using Northwind.Core.Services.Catalog;
using Northwind.Core.Models.Content;
using Northwind.Core.Models.Support;
using Northwind.Data.Entities.Content;
using Northwind.Data.Enums;
using Northwind.Data.Persistence;

namespace Northwind.Core.Services.Content;

public interface IContentService
{
    Task<PagedViewModel<ArticleViewModel>> ArticlesAsync(bool publishedOnly, ListQuery query, CancellationToken ct = default);
    Task<ArticleViewModel> ArticleBySlugAsync(string slug, CancellationToken ct = default);
    Task<ArticleViewModel> CreateArticleAsync(ArticleInput input, CancellationToken ct = default);
    Task<ArticleViewModel> UpdateArticleAsync(Guid id, ArticleInput input, CancellationToken ct = default);
    Task DeleteArticleAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TestimonialViewModel>> TestimonialsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<FaqViewModel>> FaqsAsync(CancellationToken ct = default);

    Task<SiteSettingsViewModel> SettingsAsync(CancellationToken ct = default);
    Task<SiteSettingsViewModel> UpdateSettingsAsync(SiteSettingsInput input, CancellationToken ct = default);
}

public sealed class ContentService : IContentService
{
    private readonly NorthwindDbContext _db;

    public ContentService(NorthwindDbContext db) => _db = db;

    /* ------------------------------ مقالات ------------------------------ */

    public async Task<PagedViewModel<ArticleViewModel>> ArticlesAsync(bool publishedOnly, ListQuery query, CancellationToken ct = default)
    {
        var q = _db.Articles.AsNoTracking().AsQueryable();

        if (publishedOnly) q = q.Where(a => a.IsPublished);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(a => EF.Functions.Like(a.Title, $"%{s}%") || EF.Functions.Like(a.Excerpt, $"%{s}%"));
        }

        q = q.OrderByDescending(a => a.PublishedAt);
        return await q.ToPagedAsync(query, a => a.ToViewModel(), ct);
    }

    public async Task<ArticleViewModel> ArticleBySlugAsync(string slug, CancellationToken ct = default)
    {
        var article = await _db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Slug == slug, ct)
            ?? throw new NotFoundException("مقاله", slug);

        return article.ToViewModel();
    }

    public async Task<ArticleViewModel> CreateArticleAsync(ArticleInput input, CancellationToken ct = default)
    {
        var slug = await UniqueSlugAsync(input.Slug, input.Title, null, ct);

        var article = new Article
        {
            Slug = slug,
            Title = input.Title.Trim(),
            Excerpt = input.Excerpt.Trim(),
            Body = input.Body,
            Category = input.Category,
            IsPublished = input.IsPublished,
            PublishedAt = DateTime.UtcNow,
            ReadMinutes = input.ReadMinutes
        };

        _db.Articles.Add(article);
        await _db.SaveChangesAsync(ct);
        return article.ToViewModel();
    }

    public async Task<ArticleViewModel> UpdateArticleAsync(Guid id, ArticleInput input, CancellationToken ct = default)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("مقاله", id);

        article.Slug = await UniqueSlugAsync(input.Slug, input.Title, id, ct);
        article.Title = input.Title.Trim();
        article.Excerpt = input.Excerpt.Trim();
        article.Body = input.Body;
        article.Category = input.Category;
        article.IsPublished = input.IsPublished;
        article.ReadMinutes = input.ReadMinutes;

        await _db.SaveChangesAsync(ct);
        return article.ToViewModel();
    }

    public async Task DeleteArticleAsync(Guid id, CancellationToken ct = default)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new NotFoundException("مقاله", id);

        _db.Articles.Remove(article);
        await _db.SaveChangesAsync(ct);
    }

    /* ------------------------------ نقلِ‌قول و پرسش ------------------------------ */

    public async Task<IReadOnlyList<TestimonialViewModel>> TestimonialsAsync(CancellationToken ct = default)
        => (await _db.Testimonials.AsNoTracking().OrderByDescending(t => t.CreatedAt).ToListAsync(ct))
            .Select(t => t.ToViewModel()).ToList();

    public async Task<IReadOnlyList<FaqViewModel>> FaqsAsync(CancellationToken ct = default)
        => (await _db.Faqs.AsNoTracking().OrderBy(f => f.SortOrder).ToListAsync(ct))
            .Select(f => f.ToViewModel()).ToList();

    /* ------------------------------ تنظیمات ------------------------------ */

    public async Task<SiteSettingsViewModel> SettingsAsync(CancellationToken ct = default)
    {
        var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        return settings?.ToViewModel() ?? new SiteSettingsViewModel("", "", "", "", "", "", 900, 14, false, "");
    }

    public async Task<SiteSettingsViewModel> UpdateSettingsAsync(SiteSettingsInput input, CancellationToken ct = default)
    {
        var settings = await _db.SiteSettings.FirstOrDefaultAsync(ct);

        if (settings is null)
        {
            settings = new SiteSettings { Id = SiteSettings.SingletonId };
            _db.SiteSettings.Add(settings);
        }

        settings.SiteName = input.SiteName;
        settings.SupportEmail = input.SupportEmail;
        settings.SalesEmail = input.SalesEmail;
        settings.SupportPhone = input.SupportPhone;
        settings.HeroHeadline = input.HeroHeadline;
        settings.HeroSubheading = input.HeroSubheading;
        settings.TaxRateBps = input.TaxRateBps;
        settings.TrialDays = input.TrialDays;
        settings.MaintenanceMode = input.MaintenanceMode;
        settings.Announcement = input.Announcement;

        await _db.SaveChangesAsync(ct);
        return settings.ToViewModel();
    }

    private async Task<string> UniqueSlugAsync(string? slug, string title, Guid? exceptId, CancellationToken ct)
    {
        var baseSlug = Slug.From(string.IsNullOrWhiteSpace(slug) ? title : slug);
        var candidate = baseSlug;
        var i = 2;

        while (await _db.Articles.AnyAsync(a => a.Slug == candidate && a.Id != exceptId, ct))
            candidate = $"{baseSlug}-{i++}";

        return candidate;
    }
}

/* ======================== تماس و درخواستِ دمو ======================== */

public interface IContactService
{
    Task<ContactMessageViewModel> SendAsync(ContactInput input, CancellationToken ct = default);
    Task<PagedViewModel<ContactMessageViewModel>> ListAsync(ListQuery query, bool? unreadOnly, CancellationToken ct = default);
    Task<ContactMessageViewModel> MarkReadAsync(Guid id, CancellationToken ct = default);

    Task<DemoRequestViewModel> RequestDemoAsync(DemoRequestInput input, CancellationToken ct = default);
    Task<PagedViewModel<DemoRequestViewModel>> DemoRequestsAsync(ListQuery query, DemoRequestStatus? status, CancellationToken ct = default);
    Task<DemoRequestViewModel> SetDemoStatusAsync(Guid id, DemoRequestStatus status, CancellationToken ct = default);
}

public sealed class ContactService : IContactService
{
    private readonly NorthwindDbContext _db;

    public ContactService(NorthwindDbContext db) => _db = db;

    public async Task<ContactMessageViewModel> SendAsync(ContactInput input, CancellationToken ct = default)
    {
        var message = new ContactMessage
        {
            Name = input.Name.Trim(),
            Email = input.Email.Trim(),
            Company = input.Company,
            Topic = string.IsNullOrWhiteSpace(input.Topic) ? "عمومی" : input.Topic,
            Message = input.Message.Trim()
        };

        _db.ContactMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message.ToViewModel();
    }

    public async Task<PagedViewModel<ContactMessageViewModel>> ListAsync(ListQuery query, bool? unreadOnly, CancellationToken ct = default)
    {
        var q = _db.ContactMessages.AsNoTracking().AsQueryable();

        if (unreadOnly == true) q = q.Where(c => !c.IsRead);

        q = q.OrderByDescending(c => c.CreatedAt);
        return await q.ToPagedAsync(query, c => c.ToViewModel(), ct);
    }

    public async Task<ContactMessageViewModel> MarkReadAsync(Guid id, CancellationToken ct = default)
    {
        var message = await _db.ContactMessages.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("پیام", id);

        message.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return message.ToViewModel();
    }

    public async Task<DemoRequestViewModel> RequestDemoAsync(DemoRequestInput input, CancellationToken ct = default)
    {
        if (input.ProductId is not null && !await _db.Products.AnyAsync(p => p.Id == input.ProductId, ct))
            throw new BadRequestException("محصولِ انتخاب‌شده وجود ندارد.", "product_not_found");

        var request = new DemoRequest
        {
            ProductId = input.ProductId,
            Name = input.Name.Trim(),
            Email = input.Email.Trim(),
            Company = input.Company,
            CompanySize = input.CompanySize,
            Phone = input.Phone,
            Message = input.Message,
            Status = DemoRequestStatus.New
        };

        _db.DemoRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return await _db.DemoRequests.AsNoTracking()
            .Include(d => d.Product)
            .Where(d => d.Id == request.Id)
            .Select(d => d.ToViewModel())
            .FirstAsync(ct);
    }

    public async Task<PagedViewModel<DemoRequestViewModel>> DemoRequestsAsync(ListQuery query, DemoRequestStatus? status, CancellationToken ct = default)
    {
        var q = _db.DemoRequests.AsNoTracking().Include(d => d.Product).AsQueryable();

        if (status is not null) q = q.Where(d => d.Status == status);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(d => EF.Functions.Like(d.Name, $"%{s}%")
                          || EF.Functions.Like(d.Email, $"%{s}%")
                          || EF.Functions.Like(d.Company!, $"%{s}%"));
        }

        q = q.OrderByDescending(d => d.CreatedAt);
        return await q.ToPagedAsync(query, d => d.ToViewModel(), ct);
    }

    public async Task<DemoRequestViewModel> SetDemoStatusAsync(Guid id, DemoRequestStatus status, CancellationToken ct = default)
    {
        var request = await _db.DemoRequests.Include(d => d.Product).FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new NotFoundException("درخواستِ دمو", id);

        request.Status = status;
        await _db.SaveChangesAsync(ct);
        return request.ToViewModel();
    }
}
