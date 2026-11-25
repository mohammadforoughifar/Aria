using Microsoft.EntityFrameworkCore;
using Northwind.Core.Mapping;
using Northwind.Core.Services.Common;
using Northwind.Core.Models.Catalog;
using Northwind.Core.Models.Common;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Enums;
using Northwind.Data.Persistence;

namespace Northwind.Core.Services.Catalog;

/* ============================== قراردادها ============================== */

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryViewModel>> ListAsync(CancellationToken ct = default);
    Task<CategoryViewModel> GetAsync(Guid id, CancellationToken ct = default);
    Task<CategoryViewModel> CreateAsync(CategoryInput input, CancellationToken ct = default);
    Task<CategoryViewModel> UpdateAsync(Guid id, CategoryInput input, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IProductService
{
    Task<PagedViewModel<ProductListItemViewModel>> ListAsync(ProductFilter filter, ListQuery query, CancellationToken ct = default);

    /// <summary>محصولاتِ ویژه برایِ صفحهٔ نخست.</summary>
    Task<IReadOnlyList<ProductListItemViewModel>> FeaturedAsync(int take = 6, CancellationToken ct = default);

    Task<ProductDetailViewModel> GetAsync(Guid id, CancellationToken ct = default);
    Task<ProductDetailViewModel> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<ProductDetailViewModel> CreateAsync(ProductInput input, CancellationToken ct = default);
    Task<ProductDetailViewModel> UpdateAsync(Guid id, ProductInput input, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<PlanViewModel>> ListPlansAsync(Guid productId, CancellationToken ct = default);
    Task<PlanViewModel> AddPlanAsync(Guid productId, PlanInput input, CancellationToken ct = default);
    Task<PlanViewModel> UpdatePlanAsync(Guid planId, PlanInput input, CancellationToken ct = default);
    Task DeletePlanAsync(Guid planId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVersionViewModel>> ListVersionsAsync(Guid productId, CancellationToken ct = default);
    Task<ProductVersionViewModel> AddVersionAsync(Guid productId, ProductVersionInput input, CancellationToken ct = default);
}

public interface IReviewService
{
    Task<IReadOnlyList<ReviewViewModel>> ForProductAsync(Guid productId, CancellationToken ct = default);
    Task<PagedViewModel<ReviewViewModel>> QueueAsync(ListQuery query, ReviewStatus? status, CancellationToken ct = default);
    Task<ReviewViewModel> CreateAsync(ReviewInput input, Guid? userId, string authorName, CancellationToken ct = default);
    Task<ReviewViewModel> SetStatusAsync(Guid id, ReviewStatus status, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>پالایه‌هایِ فهرستِ محصول — بیرون از <see cref="ListQuery"/> تا صفحه‌بندی تمیز بماند.</summary>
public sealed class ProductFilter
{
    public string? CategorySlug { get; set; }
    public ProductType? Type { get; set; }
    public Audience? Audience { get; set; }
    public bool? Featured { get; set; }

    /// <summary>مدیر باید محصولاتِ غیرفعال را هم ببیند.</summary>
    public bool IncludeInactive { get; set; }
}

/* ============================== دسته‌ها ============================== */

public sealed class CategoryService : ICategoryService
{
    private readonly NorthwindDbContext _db;

    public CategoryService(NorthwindDbContext db) => _db = db;

    public async Task<IReadOnlyList<CategoryViewModel>> ListAsync(CancellationToken ct = default)
    {
        var counts = await _db.Products
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, ct);

        var categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .ToListAsync(ct);

        return categories.Select(c => c.ToViewModel(counts.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CategoryViewModel> GetAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("دسته", id);

        var count = await _db.Products.CountAsync(p => p.CategoryId == id && p.IsActive, ct);
        return category.ToViewModel(count);
    }

    public async Task<CategoryViewModel> CreateAsync(CategoryInput input, CancellationToken ct = default)
    {
        var slug = await UniqueSlugAsync(input.Slug, input.Name, null, ct);

        var category = new Category
        {
            Name = input.Name.Trim(),
            Slug = slug,
            Description = input.Description,
            Icon = input.Icon,
            SortOrder = input.SortOrder
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return category.ToViewModel(0);
    }

    public async Task<CategoryViewModel> UpdateAsync(Guid id, CategoryInput input, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("دسته", id);

        category.Name = input.Name.Trim();
        category.Slug = await UniqueSlugAsync(input.Slug, input.Name, id, ct);
        category.Description = input.Description;
        category.Icon = input.Icon;
        category.SortOrder = input.SortOrder;

        await _db.SaveChangesAsync(ct);
        var count = await _db.Products.CountAsync(p => p.CategoryId == id && p.IsActive, ct);
        return category.ToViewModel(count);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException("دسته", id);

        if (await _db.Products.AnyAsync(p => p.CategoryId == id, ct))
            throw new ConflictException("این دسته محصول دارد و حذف نمی‌شود. اول محصولاتش را جابه‌جا یا حذف کنید.", "category_not_empty");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<string> UniqueSlugAsync(string? slug, string fallbackName, Guid? exceptId, CancellationToken ct)
    {
        var baseSlug = string.IsNullOrWhiteSpace(slug) ? Slug.From(fallbackName) : Slug.From(slug);
        var candidate = baseSlug;
        var i = 2;

        while (await _db.Categories.AnyAsync(c => c.Slug == candidate && c.Id != exceptId, ct))
            candidate = $"{baseSlug}-{i++}";

        return candidate;
    }
}

/* ============================== محصولات ============================== */

public sealed class ProductService : IProductService
{
    private readonly NorthwindDbContext _db;

    public ProductService(NorthwindDbContext db) => _db = db;

    public async Task<PagedViewModel<ProductListItemViewModel>> ListAsync(ProductFilter filter, ListQuery query, CancellationToken ct = default)
    {
        query.Normalize();

        var q = _db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!filter.IncludeInactive) q = q.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.CategorySlug)) q = q.Where(p => p.Category!.Slug == filter.CategorySlug);
        if (filter.Type is not null) q = q.Where(p => p.Type == filter.Type);
        if (filter.Audience is not null) q = q.Where(p => p.Audience == filter.Audience || p.Audience == Audience.Both);
        if (filter.Featured is not null) q = q.Where(p => p.IsFeatured == filter.Featured);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(p => EF.Functions.Like(p.Name, $"%{s}%")
                          || EF.Functions.Like(p.Tagline, $"%{s}%")
                          || EF.Functions.Like(p.Description, $"%{s}%"));
        }

        q = query.Sort switch
        {
            "price_asc" => q.OrderBy(p => p.BasePriceToman),
            "price_desc" => q.OrderByDescending(p => p.BasePriceToman),
            "popular" => q.OrderByDescending(p => p.Popularity),
            "name" => q.OrderBy(p => p.Name),
            "oldest" => q.OrderBy(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.CreatedAt)
        };

        var total = await q.CountAsync(ct);
        var rows = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(p => new { Product = p, PlanCount = p.Plans.Count })
            .ToListAsync(ct);

        var ratings = await RatingsAsync(rows.Select(r => r.Product.Id).ToList(), ct);
        var pages = (int)Math.Ceiling(total / (double)query.PageSize);

        var items = rows.Select(r =>
        {
            var (avg, count) = ratings.GetValueOrDefault(r.Product.Id);
            return r.Product.ToListItem(avg, count, r.PlanCount);
        }).ToList();

        return new PagedViewModel<ProductListItemViewModel>(items,
            new PagedMetaViewModel(query.Page, query.PageSize, total, pages == 0 ? 1 : pages));
    }

    public async Task<IReadOnlyList<ProductListItemViewModel>> FeaturedAsync(int take = 6, CancellationToken ct = default)
    {
        var rows = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderByDescending(p => p.Popularity)
            .Take(take)
            .Select(p => new { Product = p, PlanCount = p.Plans.Count })
            .ToListAsync(ct);

        var ratings = await RatingsAsync(rows.Select(r => r.Product.Id).ToList(), ct);
        return rows.Select(r =>
        {
            var (avg, count) = ratings.GetValueOrDefault(r.Product.Id);
            return r.Product.ToListItem(avg, count, r.PlanCount);
        }).ToList();
    }

    public async Task<ProductDetailViewModel> GetAsync(Guid id, CancellationToken ct = default)
    {
        var product = await LoadDetailAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("محصول", id);
        return await ToDetailAsync(product, ct);
    }

    public async Task<ProductDetailViewModel> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var product = await LoadDetailAsync(p => p.Slug == slug, ct)
            ?? throw new NotFoundException("محصول", slug);
        return await ToDetailAsync(product, ct);
    }

    public async Task<ProductDetailViewModel> CreateAsync(ProductInput input, CancellationToken ct = default)
    {
        if (!await _db.Categories.AnyAsync(c => c.Id == input.CategoryId, ct))
            throw new BadRequestException("دستهٔ انتخاب‌شده وجود ندارد.", "category_not_found");

        var product = new Product
        {
            Name = input.Name.Trim(),
            Slug = await UniqueSlugAsync(input.Slug, input.Name, null, ct),
            Tagline = input.Tagline.Trim(),
            Description = input.Description,
            Type = input.Type,
            Audience = input.Audience,
            Platform = input.Platform,
            Version = input.Version,
            BasePriceToman = input.BasePriceToman,
            AccentColor = input.AccentColor,
            HeroImage = input.HeroImage,
            IsFeatured = input.IsFeatured,
            IsActive = input.IsActive,
            TrialDays = input.TrialDays,
            Popularity = input.Popularity,
            Features = input.Features,
            Requirements = input.SystemRequirements,
            Gallery = input.Gallery,
            DemoVideoUrl = input.DemoVideoUrl,
            CategoryId = input.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(product.Id, ct);
    }

    public async Task<ProductDetailViewModel> UpdateAsync(Guid id, ProductInput input, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("محصول", id);

        if (!await _db.Categories.AnyAsync(c => c.Id == input.CategoryId, ct))
            throw new BadRequestException("دستهٔ انتخاب‌شده وجود ندارد.", "category_not_found");

        product.Name = input.Name.Trim();
        product.Slug = await UniqueSlugAsync(input.Slug, input.Name, id, ct);
        product.Tagline = input.Tagline.Trim();
        product.Description = input.Description;
        product.Type = input.Type;
        product.Audience = input.Audience;
        product.Platform = input.Platform;
        product.Version = input.Version;
        product.BasePriceToman = input.BasePriceToman;
        product.AccentColor = input.AccentColor;
        product.HeroImage = input.HeroImage;
        product.IsFeatured = input.IsFeatured;
        product.IsActive = input.IsActive;
        product.TrialDays = input.TrialDays;
        product.Popularity = input.Popularity;
        product.Features = input.Features;
        product.Requirements = input.SystemRequirements;
        product.Gallery = input.Gallery;
        product.DemoVideoUrl = input.DemoVideoUrl;
        product.CategoryId = input.CategoryId;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException("محصول", id);

        // سفارشِ ثبت‌شده نباید با حذفِ محصول بی‌مبنا شود.
        if (await _db.OrderItems.AnyAsync(i => i.ProductId == id, ct))
            throw new ConflictException("این محصول در سفارش‌هایِ ثبت‌شده آمده است. به‌جایِ حذف، غیرفعالش کنید.", "product_has_orders");

        if (await _db.Licenses.AnyAsync(l => l.ProductId == id, ct))
            throw new ConflictException("برایِ این محصول لایسنس صادر شده است. به‌جایِ حذف، غیرفعالش کنید.", "product_has_licenses");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
    }

    /* ------------------------------ طرح‌ها ------------------------------ */

    public async Task<IReadOnlyList<PlanViewModel>> ListPlansAsync(Guid productId, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new NotFoundException("محصول", productId);

        var plans = await _db.Plans.AsNoTracking()
            .Where(p => p.ProductId == productId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);

        return plans.Select(p => p.ToViewModel()).ToList();
    }

    public async Task<PlanViewModel> AddPlanAsync(Guid productId, PlanInput input, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new NotFoundException("محصول", productId);

        var plan = new Plan
        {
            ProductId = productId,
            Name = input.Name.Trim(),
            Tier = input.Tier,
            BillingType = input.BillingType,
            PriceToman = input.PriceToman,
            Seats = input.Seats,
            Description = input.Description,
            IsPopular = input.IsPopular,
            LicenseMonths = input.LicenseMonths,
            Features = input.Features,
            SortOrder = input.SortOrder
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan.ToViewModel();
    }

    public async Task<PlanViewModel> UpdatePlanAsync(Guid planId, PlanInput input, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new NotFoundException("طرح", planId);

        plan.Name = input.Name.Trim();
        plan.Tier = input.Tier;
        plan.BillingType = input.BillingType;
        plan.PriceToman = input.PriceToman;
        plan.Seats = input.Seats;
        plan.Description = input.Description;
        plan.IsPopular = input.IsPopular;
        plan.LicenseMonths = input.LicenseMonths;
        plan.Features = input.Features;
        plan.SortOrder = input.SortOrder;

        await _db.SaveChangesAsync(ct);
        return plan.ToViewModel();
    }

    public async Task DeletePlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
            ?? throw new NotFoundException("طرح", planId);

        if (await _db.OrderItems.AnyAsync(i => i.PlanId == planId, ct))
            throw new ConflictException("این طرح در سفارش‌هایِ ثبت‌شده آمده و حذف نمی‌شود.", "plan_has_orders");

        if (await _db.CartItems.AnyAsync(c => c.PlanId == planId, ct))
            throw new ConflictException("این طرح در سبدِ خریدِ کاربران است.", "plan_in_carts");

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
    }

    /* ------------------------------ نسخه‌ها ------------------------------ */

    public async Task<IReadOnlyList<ProductVersionViewModel>> ListVersionsAsync(Guid productId, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == productId, ct))
            throw new NotFoundException("محصول", productId);

        var versions = await _db.ProductVersions.AsNoTracking()
            .Where(v => v.ProductId == productId)
            .OrderByDescending(v => v.ReleasedAt)
            .ToListAsync(ct);

        return versions.Select(v => v.ToViewModel()).ToList();
    }

    public async Task<ProductVersionViewModel> AddVersionAsync(Guid productId, ProductVersionInput input, CancellationToken ct = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, ct)
            ?? throw new NotFoundException("محصول", productId);

        if (await _db.ProductVersions.AnyAsync(v => v.ProductId == productId && v.Version == input.Version, ct))
            throw new ConflictException("این شمارهٔ نسخه قبلاً ثبت شده است.", "version_exists");

        var version = new ProductVersion
        {
            ProductId = productId,
            Version = input.Version,
            ReleasedAt = input.ReleasedAt,
            Notes = input.Notes,
            InstallerUrl = input.InstallerUrl,
            SizeMb = input.SizeMb,
            Platform = input.Platform
        };

        _db.ProductVersions.Add(version);
        product.Version = input.Version;      // آخرین نسخه رویِ خودِ محصول هم می‌نشیند
        await _db.SaveChangesAsync(ct);

        return version.ToViewModel();
    }

    /* ------------------------------ کمکی ------------------------------ */

    private async Task<Product?> LoadDetailAsync(System.Linq.Expressions.Expression<Func<Product, bool>> predicate, CancellationToken ct)
        => await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Plans)
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(predicate, ct);

    private async Task<ProductDetailViewModel> ToDetailAsync(Product product, CancellationToken ct)
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == product.Id && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var avg = reviews.Count > 0 ? Math.Round((decimal)reviews.Average(r => r.Rating), 1) : 0m;
        return product.ToDetail(avg, reviews.Count, reviews);
    }

    private async Task<Dictionary<Guid, (decimal Avg, int Count)>> RatingsAsync(List<Guid> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0) return new();

        var rows = await _db.Reviews.AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId) && r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.ProductId)
            .Select(g => new { Id = g.Key, Avg = g.Average(x => (double)x.Rating), Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.Id, r => (Math.Round((decimal)r.Avg, 1), r.Count));
    }

    private async Task<string> UniqueSlugAsync(string? slug, string fallbackName, Guid? exceptId, CancellationToken ct)
    {
        var baseSlug = Slug.From(string.IsNullOrWhiteSpace(slug) ? fallbackName : slug);
        var candidate = baseSlug;
        var i = 2;

        while (await _db.Products.AnyAsync(p => p.Slug == candidate && p.Id != exceptId, ct))
            candidate = $"{baseSlug}-{i++}";

        return candidate;
    }
}

/* ============================== دیدگاه‌ها ============================== */

public sealed class ReviewService : IReviewService
{
    private readonly NorthwindDbContext _db;

    public ReviewService(NorthwindDbContext db) => _db = db;

    public async Task<IReadOnlyList<ReviewViewModel>> ForProductAsync(Guid productId, CancellationToken ct = default)
    {
        var reviews = await _db.Reviews.AsNoTracking()
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return reviews.Select(r => r.ToViewModel()).ToList();
    }

    public async Task<PagedViewModel<ReviewViewModel>> QueueAsync(ListQuery query, ReviewStatus? status, CancellationToken ct = default)
    {
        var q = _db.Reviews.AsNoTracking().Include(r => r.Product).AsQueryable();

        if (status is not null) q = q.Where(r => r.Status == status);

        q = q.OrderByDescending(r => r.CreatedAt);

        return await q.ToPagedAsync(query, r => r.ToViewModel(r.Product?.Name), ct);
    }

    public async Task<ReviewViewModel> CreateAsync(ReviewInput input, Guid? userId, string authorName, CancellationToken ct = default)
    {
        if (!await _db.Products.AnyAsync(p => p.Id == input.ProductId, ct))
            throw new NotFoundException("محصول", input.ProductId);

        var review = new Review
        {
            ProductId = input.ProductId,
            UserId = userId,
            AuthorName = authorName,
            Rating = input.Rating,
            Title = input.Title.Trim(),
            Body = input.Body.Trim(),
            Status = ReviewStatus.Pending      // پیش از نمایش باید تأیید شود
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);
        return review.ToViewModel();
    }

    public async Task<ReviewViewModel> SetStatusAsync(Guid id, ReviewStatus status, CancellationToken ct = default)
    {
        var review = await _db.Reviews.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("دیدگاه", id);

        review.Status = status;
        await _db.SaveChangesAsync(ct);
        return review.ToViewModel(review.Product?.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundException("دیدگاه", id);

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(ct);
    }
}

/* ------------------------------- ابزار ------------------------------- */

internal static class Slug
{
    /// <summary>ساختِ اسلاگ از متن — فاصله‌ها به خطِ تیره و حروف کوچک.</summary>
    public static string From(string text)
    {
        var cleaned = text.Trim().ToLowerInvariant()
            .Replace('ي', 'ی').Replace('ك', 'ک')
            .Replace(' ', '-');

        var chars = cleaned.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray();
        var slug = new string(chars).Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? $"item-{Guid.NewGuid():N}"[..12] : slug;
    }
}
