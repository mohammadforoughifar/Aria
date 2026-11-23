using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) => _db = db;

    /* ------------------------------- نمای کلی ------------------------------- */

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewDto>> Overview()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var prevStart = monthStart.AddMonths(-1);

        var revenueThis = await _db.Orders
            .Where(o => o.PlacedAt >= monthStart && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
            .SumAsync(o => (int?)o.TotalToman) ?? 0;
        var revenuePrev = await _db.Orders
            .Where(o => o.PlacedAt >= prevStart && o.PlacedAt < monthStart && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
            .SumAsync(o => (int?)o.TotalToman) ?? 0;

        // سریِ ۱۲ ماههٔ درآمد (برچسبِ ماه‌های شمسی)
        var series = new List<RevenuePointDto>();
        var pc = new System.Globalization.PersianCalendar();
        string[] monthsFa = { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        for (var i = 11; i >= 0; i--)
        {
            var from = monthStart.AddMonths(-i);
            var to = from.AddMonths(1);
            var total = await _db.Orders
                .Where(o => o.PlacedAt >= from && o.PlacedAt < to && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
                .SumAsync(o => (int?)o.TotalToman) ?? 0;
            series.Add(new RevenuePointDto($"{monthsFa[pc.GetMonth(from)]} {pc.GetYear(from) % 100}", total));
        }

        var top = await _db.OrderItems
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new { g.Key.ProductId, g.Key.ProductName, Units = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.LineTotalToman) })
            .OrderByDescending(x => x.Revenue).Take(5).ToListAsync();

        var products = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.AccentColor);
        var slugs = await _db.Products.ToDictionaryAsync(p => p.Id, p => p.Slug);

        var recentOrders = await _db.Orders.Include(o => o.User).OrderByDescending(o => o.PlacedAt).Take(8)
            .Select(o => o.Id).ToListAsync();
        var recentTickets = await _db.Tickets.Include(t => t.User).Include(t => t.Product)
            .OrderByDescending(t => t.UpdatedAt).Take(6).ToListAsync();

        var orders = new List<OrderDto>();
        foreach (var id in recentOrders) orders.Add(await MapOrderLite(id));

        return Ok(new AdminOverviewDto(
            await _db.Products.CountAsync(),
            await _db.Users.CountAsync(u => u.Role == Role.Customer),
            await _db.Tickets.CountAsync(t => (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress)),
            await _db.Reviews.CountAsync(r => r.Status == ReviewStatus.Pending),
            await _db.Licenses.CountAsync(l => l.Status == LicenseStatus.Active),
            await _db.Licenses.CountAsync(l => (l.Status == LicenseStatus.Active && l.ExpiresAt <= now.AddDays(30))),
            revenueThis, revenuePrev, series,
            top.Select(t => new TopProductDto(t.ProductId, t.ProductName, slugs.GetValueOrDefault(t.ProductId, ""),
                t.Units, t.Revenue, products.GetValueOrDefault(t.ProductId, "#1E3A8A"))).ToList(),
            orders,
            recentTickets.Select(t => new TicketDto(t.Id, t.Reference, t.UserId, t.User?.FullName, t.Product?.Name,
                t.Subject, t.Category, t.Priority, t.Status, t.Environment, t.CreatedAt, t.UpdatedAt,
                t.Messages.Count)).ToList()));
    }

    private async Task<OrderDto> MapOrderLite(string id)
    {
        var o = await _db.Orders.Include(x => x.Items).Include(x => x.Payment).Include(x => x.User)
            .FirstAsync(x => x.Id == id);
        return new OrderDto(o.Id, o.OrderNumber, o.UserId, o.User?.FullName, o.User?.Email, o.BillingCompany,
            o.Status, o.SubtotalToman, o.DiscountToman, o.TaxToman, o.TotalToman, o.Currency, o.DiscountCode,
            o.PlacedAt, o.BillingName, o.BillingCompany, o.BillingEmail, o.BillingCountry, o.Notes,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.PlanId, i.ProductName, i.PlanName,
                i.BillingType, i.UnitPriceToman, i.Quantity, i.LineTotalToman)).ToList(),
            o.Payment is null ? null : new PaymentDto(o.Payment.Id, o.Payment.OrderId, o.Payment.Provider,
                o.Payment.Method, o.Payment.AmountToman, o.Payment.Status, o.Payment.TransactionRef, o.Payment.PaidAt),
            null);
    }

    /* --------------------------- نمای کلیِ مشتری --------------------------- */

    [HttpGet("customer-overview")]
    public async Task<ActionResult<CustomerOverviewDto>> CustomerOverview()
    {
        var uid = Request.Headers["X-User-Id"].ToString();
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var now = DateTime.UtcNow;

        var myOrders = await _db.Orders.Where(o => o.UserId == uid).ToListAsync();
        var lic = await _db.Licenses.Include(l => l.Product).Include(l => l.Plan)
            .Where(l => l.UserId == uid).ToListAsync();

        var pc = new System.Globalization.PersianCalendar();
        string[] monthsFa = { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
        var spend = new List<RevenuePointDto>();
        for (var i = 5; i >= 0; i--)
        {
            var from = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
            var to = from.AddMonths(1);
            var total = myOrders.Where(o => o.PlacedAt >= from && o.PlacedAt < to).Sum(o => o.TotalToman);
            spend.Add(new RevenuePointDto(monthsFa[pc.GetMonth(from)], total));
        }

        var recentIds = await _db.Orders.Include(o => o.Items).Where(o => o.UserId == uid)
            .OrderByDescending(o => o.PlacedAt).Take(5).Select(o => o.Id).ToListAsync();
        var recent = new List<OrderDto>();
        foreach (var id in recentIds) recent.Add(await MapOrderLite(id));

        return Ok(new CustomerOverviewDto(
            myOrders.Count,
            lic.Count(l => l.Status == LicenseStatus.Active),
            lic.Count(l => (l.Status == LicenseStatus.Active && l.ExpiresAt <= now.AddDays(30))),
            await _db.Tickets.CountAsync(t => t.UserId == uid && (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.WaitingCustomer)),
            myOrders.Sum(o => o.TotalToman),
            await _db.Products.CountAsync(p => p.IsActive),
            await _db.Invoices.Where(i => i.Order!.UserId == uid && i.Status == InvoiceStatus.Issued).SumAsync(i => (int?)i.TotalToman) ?? 0,
            recent,
            lic.Where(l => l.Status == LicenseStatus.Active)
               .OrderBy(l => l.ExpiresAt ?? DateTime.MaxValue).Take(4)
               .Select(l => new LicenseDto(l.Id, l.LicenseKey, l.UserId, null, null, l.ProductId,
                    l.Product?.Name ?? "", l.Product?.Slug ?? "", l.Plan?.Name, l.Seats, l.ActivationsUsed,
                    l.Status, l.IssuedAt, l.ExpiresAt)).ToList(),
            spend));
    }

    /* ------------------------------- مشتریان ------------------------------- */

    [HttpGet("customers")]
    public async Task<ActionResult<Paged<CustomerDto>>> Customers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string? search = null)
    {
        var q = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.FullName.Contains(s) || u.Email.Contains(s) || (u.Company != null && u.Company.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var list = new List<CustomerDto>();
        foreach (var u in items)
        {
            list.Add(new CustomerDto(
                u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country,
                u.CreatedAt, u.LastLoginAt,
                await _db.Orders.CountAsync(o => o.UserId == u.Id),
                await _db.Orders.Where(o => o.UserId == u.Id && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
                    .SumAsync(o => (int?)o.TotalToman) ?? 0,
                await _db.Licenses.CountAsync(l => l.UserId == u.Id)));
        }

        return Ok(new Paged<CustomerDto>(list, new PagedMeta(page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpPut("customers/{id}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(string id, [FromBody] UpdateCustomerRequest req)
    {
        var u = await _db.Users.FindAsync(id);
        if (u is null) return NotFound();

        u.FullName = req.FullName.Trim();
        u.Company = req.Company;
        u.Role = req.Role;
        u.Locked = req.Locked;
        await _db.SaveChangesAsync();

        return Ok(new CustomerDto(u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country,
            u.CreatedAt, u.LastLoginAt,
            await _db.Orders.CountAsync(o => o.UserId == u.Id),
            await _db.Orders.Where(o => o.UserId == u.Id && (o.Status == OrderStatus.Paid || o.Status == OrderStatus.Completed))
                .SumAsync(o => (int?)o.TotalToman) ?? 0,
            await _db.Licenses.CountAsync(l => l.UserId == u.Id)));
    }

    /* ----------------------------- کدهای تخفیف ----------------------------- */

    [HttpGet("discount-codes")]
    public async Task<ActionResult<IReadOnlyList<DiscountCodeDto>>> DiscountCodes() => Ok(
        (await _db.DiscountCodes.OrderByDescending(d => d.CreatedAt).ToListAsync())
            .Select(d => new DiscountCodeDto(d.Id, d.Code, d.Description, d.PercentOff, d.MaxRedemptions,
                d.TimesRedeemed, d.ExpiresAt, d.IsActive, d.CreatedAt)).ToList());

    [HttpPost("discount-codes")]
    public async Task<ActionResult<DiscountCodeDto>> UpsertDiscount([FromBody] DiscountCodeDto dto)
    {
        var existing = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Id == dto.Id);
        if (existing is null)
        {
            _db.DiscountCodes.Add(new DiscountCode
            {
                Id = dto.Id, Code = dto.Code.ToUpperInvariant(), Description = dto.Description,
                PercentOff = dto.PercentOff, MaxRedemptions = dto.MaxRedemptions,
                TimesRedeemed = dto.TimesRedeemed, ExpiresAt = dto.ExpiresAt, IsActive = dto.IsActive,
            });
        }
        else
        {
            existing.Code = dto.Code.ToUpperInvariant();
            existing.Description = dto.Description;
            existing.PercentOff = dto.PercentOff;
            existing.MaxRedemptions = dto.MaxRedemptions;
            existing.TimesRedeemed = dto.TimesRedeemed;
            existing.ExpiresAt = dto.ExpiresAt;
            existing.IsActive = dto.IsActive;
        }
        await _db.SaveChangesAsync();

        var d = await _db.DiscountCodes.FirstAsync(x => x.Id == dto.Id);
        return Ok(new DiscountCodeDto(d.Id, d.Code, d.Description, d.PercentOff, d.MaxRedemptions,
            d.TimesRedeemed, d.ExpiresAt, d.IsActive, d.CreatedAt));
    }

    [HttpDelete("discount-codes/{id}")]
    public async Task<IActionResult> DeleteDiscount(string id)
    {
        var d = await _db.DiscountCodes.FindAsync(id);
        if (d is null) return NotFound();
        _db.DiscountCodes.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /* --------------------------- درخواست‌ها و پیام‌ها --------------------------- */

    [HttpGet("demo-requests")]
    public async Task<ActionResult<IReadOnlyList<DemoRequestDto>>> DemoRequests() => Ok(
        (await _db.DemoRequests.Include(d => d.Product).OrderByDescending(d => d.CreatedAt).ToListAsync())
            .Select(d => new DemoRequestDto(d.Id, d.ProductId, d.Product?.Name, d.Name, d.Email,
                d.Company, d.CompanySize, d.Phone, d.Message, d.Status, d.CreatedAt)).ToList());

    [HttpPatch("demo-requests/{id}/status")]
    public async Task<IActionResult> DemoStatus(string id, [FromBody] StatusRequest req)
    {
        var d = await _db.DemoRequests.FindAsync(id);
        if (d is null) return NotFound();
        if (Enum.TryParse<DemoRequestStatus>(req.Status, true, out var st)) d.Status = st;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("contact-messages")]
    public async Task<ActionResult<IReadOnlyList<ContactMessageDto>>> ContactMessages() => Ok(
        (await _db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync())
            .Select(m => new ContactMessageDto(m.Id, m.Name, m.Email, m.Company, m.Topic, m.Message, m.IsRead, m.CreatedAt))
            .ToList());

    [HttpPost("contact-messages/{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        var m = await _db.ContactMessages.FindAsync(id);
        if (m is null) return NotFound();
        m.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    /* ------------------------------- محصولات ------------------------------- */

    [HttpPost("products")]
    public async Task<ActionResult<ProductDetailDto>> UpsertProduct([FromBody] ProductDetailDto dto)
    {
        var existing = await _db.Products.Include(p => p.Plans).FirstOrDefaultAsync(p => p.Id == dto.Id);
        if (existing is null)
        {
            _db.Products.Add(new Product
            {
                Id = dto.Id, Slug = dto.Slug, Name = dto.Name, Tagline = dto.Tagline,
                Description = dto.Description, Type = dto.Type, Platform = dto.Platform,
                Version = dto.Version, BasePriceToman = LegacyMoney.ToLegacy(dto.BasePriceToman),
                AccentColor = dto.AccentColor, HeroImage = dto.HeroImage,
                IsFeatured = dto.IsFeatured, IsActive = dto.IsActive, TrialDays = dto.TrialDays,
                CategoryId = dto.CategoryId, CreatedAt = DateTime.UtcNow,
                FeaturesJson = System.Text.Json.JsonSerializer.Serialize(dto.Features),
                RequirementsJson = System.Text.Json.JsonSerializer.Serialize(dto.SystemRequirements),
                GalleryJson = System.Text.Json.JsonSerializer.Serialize(dto.Gallery),
                Plans = dto.Plans.Select(p => new Plan
                {
                    Id = p.Id, Name = p.Name, Tier = p.Tier, BillingType = p.BillingType,
                    PriceToman = LegacyMoney.ToLegacy(p.PriceToman), Seats = p.Seats, Description = p.Description,
                    IsPopular = p.IsPopular,
                }).ToList(),
            });
        }
        else
        {
            existing.Slug = dto.Slug; existing.Name = dto.Name; existing.Tagline = dto.Tagline;
            existing.Description = dto.Description; existing.Type = dto.Type; existing.Platform = dto.Platform;
            existing.Version = dto.Version; existing.BasePriceToman = LegacyMoney.ToLegacy(dto.BasePriceToman);
            existing.AccentColor = dto.AccentColor; existing.HeroImage = dto.HeroImage;
            existing.IsFeatured = dto.IsFeatured; existing.IsActive = dto.IsActive;
            existing.TrialDays = dto.TrialDays; existing.CategoryId = dto.CategoryId;
            existing.FeaturesJson = System.Text.Json.JsonSerializer.Serialize(dto.Features);
            existing.RequirementsJson = System.Text.Json.JsonSerializer.Serialize(dto.SystemRequirements);
            existing.GalleryJson = System.Text.Json.JsonSerializer.Serialize(dto.Gallery);

            // همگام‌سازیِ طرح‌ها
            foreach (var plan in dto.Plans)
            {
                var target = existing.Plans.FirstOrDefault(p => p.Id == plan.Id);
                if (target is null)
                {
                    existing.Plans.Add(new Plan
                    {
                        Id = plan.Id, Name = plan.Name, Tier = plan.Tier, BillingType = plan.BillingType,
                        PriceToman = LegacyMoney.ToLegacy(plan.PriceToman), Seats = plan.Seats, Description = plan.Description,
                        IsPopular = plan.IsPopular,
                    });
                }
                else
                {
                    target.Name = plan.Name; target.Tier = plan.Tier; target.BillingType = plan.BillingType;
                    target.PriceToman = LegacyMoney.ToLegacy(plan.PriceToman); target.Seats = plan.Seats;
                    target.Description = plan.Description; target.IsPopular = plan.IsPopular;
                }
            }
            foreach (var stale in existing.Plans.Where(p => dto.Plans.All(x => x.Id != p.Id)).ToList())
                _db.Plans.Remove(stale);
        }

        await _db.SaveChangesAsync();
        return await new CatalogController(_db).ById(dto.Id) switch
        {
            ActionResult<ProductDetailDto> r => r,
            _ => NotFound(),
        };
    }

    [HttpDelete("products/{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p is null) return NotFound();
        _db.Products.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
    /* ---------------------------- دسته‌بندی‌ها ----------------------------- */

    [HttpPut("categories/{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(string id, [FromBody] CategoryDto dto)
    {
        var c = await _db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        c.Name = dto.Name;
        c.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Name) : dto.Slug;
        c.Description = dto.Description;
        c.Icon = string.IsNullOrWhiteSpace(dto.Icon) ? "package" : dto.Icon;
        await _db.SaveChangesAsync();
        return Ok(await MapCategory(c));
    }

    private async Task<CategoryDto> MapCategory(Category c)
        => new(c.Id, c.Name, c.Slug, c.Description, c.Icon,
               await _db.Products.CountAsync(p => p.CategoryId == c.Id));

    private static string Slugify(string name)
    {
        var s = name.Trim().ToLowerInvariant().Replace(" ", "-");
        var keep = new System.Text.StringBuilder();
        foreach (var ch in s)
            if (char.IsLetterOrDigit(ch) || ch == '-') keep.Append(ch);
        return keep.Length == 0 ? "item-" + Guid.NewGuid().ToString("N")[..6] : keep.ToString();
    }

    /* ------------------------------- مقالات -------------------------------- */

    [HttpGet("articles")]
    public async Task<ActionResult<Paged<ArticleDto>>> Articles(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var q = _db.Articles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(a => a.Title.Contains(s) || a.Slug.Contains(s) || (a.Category != null && a.Category.Contains(s)));
        }
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.PublishedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new ArticleDto(a.Id, a.Slug, a.Title, a.Excerpt, a.Body, a.Category,
                                        a.IsPublished, a.PublishedAt, a.ReadMinutes))
            .ToListAsync();
        return Ok(new Paged<ArticleDto>(items, new PagedMeta(page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpPost("articles")]
    public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] ArticleDto dto)
    {
        var a = new Article
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? Guid.NewGuid().ToString("N") : dto.Id,
            Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Title) : dto.Slug,
            Title = dto.Title, Excerpt = dto.Excerpt, Body = dto.Body,
            Category = dto.Category, IsPublished = dto.IsPublished,
            PublishedAt = DateTime.UtcNow,
            ReadMinutes = dto.ReadMinutes <= 0 ? 4 : dto.ReadMinutes,
        };
        _db.Articles.Add(a);
        await _db.SaveChangesAsync();
        return Ok(dto with { Id = a.Id, Slug = a.Slug, PublishedAt = a.PublishedAt });
    }

    [HttpPut("articles/{id}")]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(string id, [FromBody] ArticleDto dto)
    {
        var a = await _db.Articles.FindAsync(id);
        if (a is null) return NotFound();
        a.Title = dto.Title;
        a.Slug = string.IsNullOrWhiteSpace(dto.Slug) ? Slugify(dto.Title) : dto.Slug;
        a.Excerpt = dto.Excerpt; a.Body = dto.Body;
        a.Category = dto.Category; a.IsPublished = dto.IsPublished;
        a.ReadMinutes = dto.ReadMinutes <= 0 ? 4 : dto.ReadMinutes;
        await _db.SaveChangesAsync();
        return Ok(dto with { Id = a.Id, Slug = a.Slug, PublishedAt = a.PublishedAt });
    }

    [HttpDelete("articles/{id}")]
    public async Task<IActionResult> DeleteArticle(string id)
    {
        var a = await _db.Articles.FindAsync(id);
        if (a is null) return NotFound();
        _db.Articles.Remove(a);
        await _db.SaveChangesAsync();
        return NoContent();
    }

}

public sealed record UpdateCustomerRequest(string FullName, string? Company, Role Role, bool Locked);
