using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

[ApiController]
[Route("api")]
public class CatalogController : ControllerBase
{
    private readonly AppDbContext _db;
    public CatalogController(AppDbContext db) => _db = db;

    private static List<string> List(string json) =>
        string.IsNullOrWhiteSpace(json) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(json) ?? new();

    private static CategoryDto MapCategory(Category c, int count) =>
        new(c.Id, c.Name, c.Slug, c.Description, c.Icon, count);

    private static PlanDto MapPlan(Plan p) =>
        new(p.Id, p.Name, p.Tier, p.BillingType, p.PriceToman, p.Seats, p.Description, p.IsPopular, p.LicenseMonths);

    private static ProductListItemDto MapListItem(Product p, decimal rating, int ratingCount) => new(
        p.Id, p.Slug, p.Name, p.Tagline, p.Type, p.Platform, p.Version, p.BasePriceToman,
        rating, ratingCount, p.Popularity, p.HeroImage, p.AccentColor, p.IsFeatured, p.IsActive,
        p.CreatedAt, p.Category?.Name, p.Category?.Slug, p.Plans.Count);

    private static ProductDetailDto MapDetail(Product p, decimal rating, int ratingCount) => new(
        p.Id, p.Slug, p.Name, p.Tagline, p.Description, p.Type, p.Platform, p.Version,
        p.BasePriceToman, rating, ratingCount, p.Popularity, p.AccentColor, p.HeroImage, p.IsActive,
        p.IsFeatured, p.TrialDays, p.CreatedAt,
        p.CategoryId, p.Category?.Name ?? "", p.Category?.Slug ?? "",
        List(p.FeaturesJson), List(p.RequirementsJson), List(p.GalleryJson), p.DemoVideoUrl,
        p.Plans.OrderBy(x => x.SortOrder).Select(MapPlan).ToList(),
        p.Versions.OrderByDescending(v => v.ReleasedAt)
            .Select(v => new ProductVersionDto(v.Id, v.Version, v.ReleasedAt, v.Notes, v.InstallerUrl, v.SizeMb, v.Platform)).ToList(),
        p.Reviews.Where(r => r.Status == ReviewStatus.Approved).OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewDto(r.Id, r.ProductId, p.Name, r.AuthorName, r.AuthorCompany,
                                       r.Rating, r.Title, r.Body, r.Status, r.CreatedAt)).ToList());

    private async Task<(decimal avg, int count)> RatingAsync(string productId)
    {
        var approved = await _db.Reviews
            .Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved)
            .Select(r => (double)r.Rating).ToListAsync();
        return approved.Count == 0 ? (0m, 0) : ((decimal)Math.Round(approved.Average(), 1), approved.Count);
    }

    /* --------------------------------- محصولات --------------------------------- */

    [HttpGet("products")]
    public async Task<ActionResult<Paged<ProductListItemDto>>> Products(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string? search = null,
        [FromQuery] string? sortBy = "newest", [FromQuery] string? sortDir = "desc",
        [FromQuery] string? category = null, [FromQuery] string? type = null,
        [FromQuery] string? audience = null, [FromQuery] bool? featured = null,
        [FromQuery] bool includeInactive = false)
    {
        var q = _db.Products.Include(p => p.Category).Include(p => p.Plans).AsQueryable();
        if (!includeInactive) q = q.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(p => p.Name.Contains(s) || p.Tagline.Contains(s) || p.Description.Contains(s));
        }
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category!.Slug == category);
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ProductType>(type, true, out var pt)) q = q.Where(p => p.Type == pt);
        if (!string.IsNullOrWhiteSpace(audience) && Enum.TryParse<Audience>(audience, true, out var au))
            q = q.Where(p => p.Audience == au || p.Audience == Audience.Both);
        if (featured == true) q = q.Where(p => p.IsFeatured);

        var desc = sortDir != "asc";
        q = (sortBy ?? "newest") switch
        {
            "popular"  => desc ? q.OrderByDescending(p => p.Popularity) : q.OrderBy(p => p.Popularity),
            "price-asc"  => q.OrderBy(p => p.BasePriceToman),
            "price-desc" => q.OrderByDescending(p => p.BasePriceToman),
            "name"       => desc ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
            "oldest"     => q.OrderBy(p => p.CreatedAt),
            _            => desc ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
        };

        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var list = new List<ProductListItemDto>();
        foreach (var p in items)
        {
            var (avg, count) = await RatingAsync(p.Id);
            list.Add(MapListItem(p, avg, count));
        }

        return Ok(new Paged<ProductListItemDto>(list, new PagedMeta(page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpGet("products/slug/{slug}")]
    public async Task<ActionResult<ProductDetailDto>> BySlug(string slug)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Plans)
            .Include(x => x.Versions).Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Slug == slug);
        if (p is null) return NotFound();
        var (avg, count) = await RatingAsync(p.Id);
        return Ok(MapDetail(p, avg, count));
    }

    /// <summary>
    /// جزئیاتِ چند محصول با یک درخواست — جایگزینِ فراخوانیِ یکی‌یکی (N+1).
    /// ترتیبِ خروجی همان ترتیبِ شناسه‌هایِ درخواستی است.
    /// </summary>
    [HttpGet("products/bulk")]
    public async Task<ActionResult<IReadOnlyList<ProductDetailDto>>> Bulk([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids)) return Ok(Array.Empty<ProductDetailDto>());

        var wanted = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Take(50).ToList();
        if (wanted.Count == 0) return Ok(Array.Empty<ProductDetailDto>());

        var items = await _db.Products
            .Include(x => x.Category).Include(x => x.Plans)
            .Include(x => x.Versions).Include(x => x.Reviews)
            .Where(p => wanted.Contains(p.Id))
            .ToListAsync();

        var byId = items.ToDictionary(p => p.Id);
        var list = new List<ProductDetailDto>();
        foreach (var id in wanted)
        {
            if (!byId.TryGetValue(id, out var p)) continue;
            var (avg, count) = await RatingAsync(p.Id);
            list.Add(MapDetail(p, avg, count));
        }
        return Ok(list);
    }

    [HttpGet("products/{id}")]
    public async Task<ActionResult<ProductDetailDto>> ById(string id)
    {
        var p = await _db.Products.Include(x => x.Category).Include(x => x.Plans)
            .Include(x => x.Versions).Include(x => x.Reviews).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var (avg, count) = await RatingAsync(p.Id);
        return Ok(MapDetail(p, avg, count));
    }

    [HttpGet("products/featured")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> Featured()
    {
        var items = await _db.Products.Include(p => p.Category).Include(p => p.Plans)
            .Where(p => p.IsActive && p.IsFeatured).OrderBy(p => p.Popularity).Take(4).ToListAsync();
        var list = new List<ProductListItemDto>();
        foreach (var p in items)
        {
            var (avg, count) = await RatingAsync(p.Id);
            list.Add(MapListItem(p, avg, count));
        }
        return Ok(list);
    }

    /* -------------------------------- دسته‌بندی -------------------------------- */

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Categories([FromQuery] bool includeInactive = false)
    {
        var cats = await _db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        var list = new List<CategoryDto>();
        foreach (var c in cats)
        {
            var count = await _db.Products.CountAsync(p => p.CategoryId == c.Id && (includeInactive || p.IsActive));
            list.Add(MapCategory(c, count));
        }
        return Ok(list);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<CategoryDto>> UpsertCategory([FromBody] CategoryDto dto)
    {
        var existing = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.Id);
        if (existing is null)
        {
            _db.Categories.Add(new Category
            {
                Id = dto.Id, Name = dto.Name, Slug = dto.Slug,
                Description = dto.Description, Icon = dto.Icon, SortOrder = 99,
            });
        }
        else
        {
            existing.Name = dto.Name;
            existing.Slug = dto.Slug;
            existing.Description = dto.Description;
            existing.Icon = dto.Icon;
        }
        await _db.SaveChangesAsync();
        var count = await _db.Products.CountAsync(p => p.CategoryId == dto.Id);
        return Ok(MapCategory(await _db.Categories.FirstAsync(c => c.Id == dto.Id), count));
    }

    [HttpDelete("categories/{id}")]
    public async Task<IActionResult> DeleteCategory(string id)
    {
        if (await _db.Products.AnyAsync(p => p.CategoryId == id)) return BadRequest("این دسته محصول دارد.");
        var c = await _db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /* ------------------------------ محتوای سایت ------------------------------ */

    [HttpGet("testimonials")]
    public async Task<ActionResult<IReadOnlyList<TestimonialDto>>> Testimonials() => Ok(
        (await _db.Testimonials.ToListAsync())
            .Select(t => new TestimonialDto(t.Id, t.AuthorName, t.AuthorRole, t.Company, t.Quote, t.Rating)).ToList());

    [HttpGet("faqs")]
    public async Task<ActionResult<IReadOnlyList<FaqDto>>> Faqs() => Ok(
        (await _db.Faqs.OrderBy(f => f.SortOrder).ToListAsync())
            .Select(f => new FaqDto(f.Id, f.Question, f.Answer)).ToList());

    [HttpPost("contact")]
    public async Task<IActionResult> Contact([FromBody] ContactRequest req)
    {
        _db.ContactMessages.Add(new ContactMessage
        {
            Name = req.Name, Email = req.Email, Company = req.Company,
            Topic = req.Topic, Message = req.Message,
        });
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("demo-requests")]
    public async Task<IActionResult> DemoRequest([FromBody] DemoRequestInput req)
    {
        _db.DemoRequests.Add(new DemoRequest
        {
            ProductId = req.ProductId, Name = req.Name, Email = req.Email, Company = req.Company,
            CompanySize = req.CompanySize, Phone = req.Phone, Message = req.Message,
        });
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public sealed record ContactRequest(string Name, string Email, string? Company, string Topic, string Message);
public sealed record DemoRequestInput(string? ProductId, string Name, string Email, string? Company,
    string? CompanySize, string? Phone, string? Message);
