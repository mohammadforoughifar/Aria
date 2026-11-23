using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

/// <summary>وبلاگ / مرکزِ راهنما — فهرستِ صفحه‌بندی‌شده، جزئیات، و مدیریتِ مقالات.</summary>
[ApiController]
[Route("api/articles")]
public class BlogController : ControllerBase
{
    private readonly AppDbContext _db;
    public BlogController(AppDbContext db) => _db = db;

    /// <summary>فهرستِ مقالات. مدیر می‌تواند پیش‌نویس‌ها را هم ببیند.</summary>
    [HttpGet]
    public async Task<ActionResult<Paged<ArticleDto>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 9,
        [FromQuery] bool publishedOnly = true, [FromQuery] string? search = null,
        [FromQuery] string? category = null)
    {
        var uid = Request.Headers["X-User-Id"].FirstOrDefault();
        var isAdmin = uid is not null && await _db.Users.AnyAsync(u => u.Id == uid && u.Role == Role.Admin);
        if (isAdmin) publishedOnly = false;

        var q = _db.Articles.AsQueryable();
        if (publishedOnly) q = q.Where(a => a.IsPublished);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(a => a.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => a.Title.Contains(s) || a.Excerpt.Contains(s) || (a.Category != null && a.Category.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ArticleDto(a.Id, a.Slug, a.Title, a.Excerpt, a.Body, a.Category,
                                        a.IsPublished, a.PublishedAt, a.ReadMinutes))
            .ToListAsync();

        return Ok(new Paged<ArticleDto>(items,
            new PagedMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize))));
    }

    /// <summary>جزئیاتِ یک مقاله با نشانیِ یکتا.</summary>
    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticleDto>> BySlug(string slug)
    {
        var a = await _db.Articles.FirstOrDefaultAsync(x => x.Slug == slug || x.Id == slug);
        if (a is null) return NotFound();
        return Ok(new ArticleDto(a.Id, a.Slug, a.Title, a.Excerpt, a.Body, a.Category,
                                 a.IsPublished, a.PublishedAt, a.ReadMinutes));
    }

    /// <summary>دسته‌بندی‌های موجودِ وبلاگ (برای فیلترِ کناری).</summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<string>>> Categories()
        => Ok(await _db.Articles.Where(a => a.Category != null)
            .Select(a => a.Category!).Distinct().OrderBy(c => c).ToListAsync());
}
