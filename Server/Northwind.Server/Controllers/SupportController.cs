using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

[ApiController]
[Route("api")]
public class SupportController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupportController(AppDbContext db) => _db = db;

    private string? CurrentUserId =>
        Request.Headers.TryGetValue("X-User-Id", out var v) ? v.ToString() : null;

    private async Task<bool> IsAdminAsync()
    {
        var uid = CurrentUserId;
        return uid is not null && await _db.Users.AnyAsync(u => u.Id == uid && u.Role == Role.Admin);
    }

    /* -------------------------------- لایسنس -------------------------------- */

    private static LicenseDto MapLicense(License l) => new(
        l.Id, l.LicenseKey, l.UserId, l.User?.FullName, l.User?.Email,
        l.ProductId, l.Product?.Name ?? "", l.Product?.Slug ?? "", l.Plan?.Name,
        l.Seats, l.ActivationsUsed, l.Status, l.IssuedAt, l.ExpiresAt);

    [HttpGet("licenses")]
    public async Task<ActionResult<Paged<LicenseDto>>> Licenses(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var q = _db.Licenses.Include(l => l.User).Include(l => l.Product).Include(l => l.Plan).AsQueryable();

        if (!await IsAdminAsync())
        {
            var uid = CurrentUserId;
            if (uid is null) return Unauthorized();
            q = q.Where(l => l.UserId == uid);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LicenseStatus>(status, true, out var st))
            q = q.Where(l => l.Status == st);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(l => l.LicenseKey.Contains(s) || (l.Product != null && l.Product.Name.Contains(s))
                          || (l.User != null && l.User.FullName.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderBy(l => l.ExpiresAt ?? DateTime.MaxValue)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new Paged<LicenseDto>(items.Select(MapLicense).ToList(),
            new PagedMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpPost("licenses/{id}/action")]
    public async Task<ActionResult<LicenseDto>> LicenseAction(string id, [FromBody] LicenseActionRequest req)
    {
        var l = await _db.Licenses.Include(x => x.User).Include(x => x.Product).Include(x => x.Plan)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();

        switch (req.Action.ToLowerInvariant())
        {
            case "extend":
                l.ExpiresAt = (l.ExpiresAt ?? DateTime.UtcNow).AddDays(req.Days ?? 90);
                l.Status = LicenseStatus.Active;
                break;
            case "revoke":
                l.Status = LicenseStatus.Revoked;
                break;
            case "activate":
                l.Status = LicenseStatus.Active;
                if (l.ActivationsUsed < l.Seats) l.ActivationsUsed++;
                break;
            case "release":
                if (l.ActivationsUsed > 0) l.ActivationsUsed--;
                break;
        }

        await _db.SaveChangesAsync();
        return Ok(MapLicense(l));
    }

    [HttpPost("licenses/generate")]
    public async Task<ActionResult<LicenseDto>> Generate([FromBody] GenerateLicenseRequest req)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var blocks = new List<string>();
        for (var b = 0; b < 4; b++)
        {
            var s = new char[5];
            for (var i = 0; i < 5; i++) s[i] = chars[Random.Shared.Next(chars.Length)];
            blocks.Add(new string(s));
        }

        var plan = req.PlanId is null ? null : await _db.Plans.FindAsync(req.PlanId);
        var license = new License
        {
            LicenseKey = string.Join("-", blocks),
            UserId = req.UserId,
            ProductId = req.ProductId,
            PlanId = req.PlanId,
            Seats = req.Seats > 0 ? req.Seats : plan?.Seats ?? 1,
            Status = LicenseStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(req.ValidDays ?? 365),
        };
        _db.Licenses.Add(license);
        await _db.SaveChangesAsync();
        await _db.Entry(license).Reference(x => x.User).LoadAsync();
        await _db.Entry(license).Reference(x => x.Product).LoadAsync();
        await _db.Entry(license).Reference(x => x.Plan).LoadAsync();
        return Ok(MapLicense(license));
    }

    /* -------------------------------- دانلود -------------------------------- */

    [HttpGet("downloads/available")]
    public async Task<ActionResult<IReadOnlyList<AvailableDownloadDto>>> Available()
    {
        var uid = CurrentUserId;
        var products = await _db.Products.Include(p => p.Versions).Where(p => p.IsActive).ToListAsync();
        var licensed = uid is null
            ? new HashSet<string>()
            : (await _db.Licenses.Where(l => l.UserId == uid && l.Status == LicenseStatus.Active).ToListAsync())
                .Select(l => l.ProductId).ToHashSet();

        return Ok(products.Select(p => new AvailableDownloadDto(
            p.Id, p.Name, p.Slug, p.AccentColor, licensed.Contains(p.Id),
            p.Versions.OrderByDescending(v => v.ReleasedAt)
                .Select(v => new AvailableVersionDto(v.Id, v.Version, v.ReleasedAt, v.Platform,
                    v.SizeMb, $"{p.Slug}-{v.Version}.exe", v.InstallerUrl, v.Notes)).ToList())).ToList());
    }

    [HttpGet("downloads/history")]
    public async Task<ActionResult<IReadOnlyList<DownloadDto>>> History()
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        return Ok((await _db.Downloads.Include(d => d.Product).Where(d => d.UserId == uid)
            .OrderByDescending(d => d.DownloadedAt).Take(50).ToListAsync())
            .Select(d => new DownloadDto(d.Id, d.ProductId, d.Product?.Name ?? "", d.Product?.Slug ?? "",
                d.VersionId, d.Version, d.Kind, d.FileName, d.SizeMb, d.DownloadedAt, d.RequiresLicense, d.UserId))
            .ToList());
    }

    [HttpPost("downloads")]
    public async Task<ActionResult<DownloadDto>> Record([FromBody] RecordDownloadRequest req)
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        var product = await _db.Products.FindAsync(req.ProductId);
        if (product is null) return NotFound();

        var d = new Download
        {
            ProductId = req.ProductId, UserId = uid, VersionId = req.VersionId, Version = req.Version,
            Kind = req.Kind, FileName = req.FileName, SizeMb = req.SizeMb,
            RequiresLicense = req.RequiresLicense, DownloadedAt = DateTime.UtcNow,
        };
        _db.Downloads.Add(d);
        await _db.SaveChangesAsync();

        return Ok(new DownloadDto(d.Id, d.ProductId, product.Name, product.Slug, d.VersionId, d.Version,
            d.Kind, d.FileName, d.SizeMb, d.DownloadedAt, d.RequiresLicense, uid));
    }

    /* ------------------------------- دیدگاه‌ها ------------------------------- */

    [HttpGet("reviews")]
    public async Task<ActionResult<Paged<ReviewDto>>> Reviews(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? productId = null, [FromQuery] string? status = null)
    {
        var q = _db.Reviews.Include(r => r.Product).AsQueryable();
        if (!string.IsNullOrWhiteSpace(productId)) q = q.Where(r => r.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReviewStatus>(status, true, out var st))
            q = q.Where(r => r.Status == st);
        else if (string.IsNullOrWhiteSpace(status) && string.IsNullOrWhiteSpace(productId))
            q = q.Where(r => r.Status == ReviewStatus.Approved);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new Paged<ReviewDto>(
            items.Select(r => new ReviewDto(r.Id, r.ProductId, r.Product?.Name, r.AuthorName, r.AuthorCompany,
                r.Rating, r.Title, r.Body, r.Status, r.CreatedAt)).ToList(),
            new PagedMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpPost("reviews")]
    public async Task<ActionResult<ReviewDto>> CreateReview([FromBody] NewReviewInput input)
    {
        var uid = CurrentUserId;
        var user = uid is null ? null : await _db.Users.FindAsync(uid);
        var review = new Review
        {
            ProductId = input.ProductId, UserId = uid,
            AuthorName = user?.FullName ?? "کاربر مهمان", AuthorCompany = user?.Company,
            Rating = Math.Clamp(input.Rating, 1, 5), Title = input.Title, Body = input.Body,
            Status = ReviewStatus.Pending, CreatedAt = DateTime.UtcNow,
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return Ok(new ReviewDto(review.Id, review.ProductId, null, review.AuthorName, review.AuthorCompany,
            review.Rating, review.Title, review.Body, review.Status, review.CreatedAt));
    }

    [HttpPatch("reviews/{id}/status")]
    public async Task<ActionResult<ReviewDto>> Moderate(string id, [FromBody] StatusRequest req)
    {
        var r = await _db.Reviews.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        if (Enum.TryParse<ReviewStatus>(req.Status, true, out var st)) r.Status = st;
        await _db.SaveChangesAsync();
        return Ok(new ReviewDto(r.Id, r.ProductId, r.Product?.Name, r.AuthorName, r.AuthorCompany,
            r.Rating, r.Title, r.Body, r.Status, r.CreatedAt));
    }

    [HttpDelete("reviews/{id}")]
    public async Task<IActionResult> DeleteReview(string id)
    {
        var r = await _db.Reviews.FindAsync(id);
        if (r is null) return NotFound();
        _db.Reviews.Remove(r);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /* -------------------------------- تیکت‌ها -------------------------------- */

    private static TicketDto MapTicket(Ticket t) => new(
        t.Id, t.Reference, t.UserId, t.User?.FullName, t.Product?.Name,
        t.Subject, t.Category, t.Priority, t.Status, t.Environment,
        t.CreatedAt, t.UpdatedAt, t.Messages.Count);

    [HttpGet("tickets")]
    public async Task<ActionResult<Paged<TicketDto>>> Tickets(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var q = _db.Tickets.Include(t => t.User).Include(t => t.Product).Include(t => t.Messages).AsQueryable();

        if (!await IsAdminAsync())
        {
            var uid = CurrentUserId;
            if (uid is null) return Unauthorized();
            q = q.Where(t => t.UserId == uid);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, true, out var st))
            q = q.Where(t => t.Status == st);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(t => t.Subject.Contains(s) || t.Reference.Contains(s));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(t => t.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new Paged<TicketDto>(items.Select(MapTicket).ToList(),
            new PagedMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpGet("tickets/{id}")]
    public async Task<ActionResult<TicketDetailDto>> Ticket(string id)
    {
        var t = await _db.Tickets.Include(x => x.User).Include(x => x.Product).Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        return Ok(new TicketDetailDto(
            t.Id, t.Reference, t.UserId, t.User?.FullName, t.ProductId, t.Product?.Name,
            t.Subject, t.Category, t.Priority, t.Status, t.Environment, t.CreatedAt, t.UpdatedAt,
            t.Messages.OrderBy(m => m.CreatedAt)
                .Select(m => new TicketMessageDto(m.Id, m.TicketId, m.AuthorName, m.IsStaff, m.Body, m.CreatedAt))
                .ToList()));
    }

    [HttpPost("tickets")]
    public async Task<ActionResult<TicketDto>> CreateTicket([FromBody] NewTicketInput input)
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        var user = await _db.Users.FindAsync(uid);
        var count = await _db.Tickets.CountAsync() + 1;

        var t = new Ticket
        {
            Reference = $"TK-1404-{2000 + count}",
            UserId = uid, ProductId = input.ProductId, Subject = input.Subject,
            Category = input.Category, Priority = input.Priority, Status = TicketStatus.Open,
            Environment = input.Environment, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
            Messages = new List<TicketMessage>
            {
                new() { AuthorName = user?.FullName ?? "کاربر", IsStaff = false, Body = input.Message, CreatedAt = DateTime.UtcNow },
            },
        };
        _db.Tickets.Add(t);
        await _db.SaveChangesAsync();
        await _db.Entry(t).Reference(x => x.User).LoadAsync();
        await _db.Entry(t).Reference(x => x.Product).LoadAsync();
        return Ok(MapTicket(t));
    }

    [HttpPost("tickets/{id}/messages")]
    public async Task<ActionResult<TicketDetailDto>> AddMessage(string id, [FromBody] AddMessageRequest req)
    {
        var t = await _db.Tickets.Include(x => x.User).Include(x => x.Product).Include(x => x.Messages)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();

        var uid = CurrentUserId;
        var user = uid is null ? null : await _db.Users.FindAsync(uid);
        var isStaff = user?.Role == Role.Admin;

        t.Messages.Add(new TicketMessage
        {
            AuthorName = user?.FullName ?? "کاربر", IsStaff = isStaff,
            Body = req.Body, CreatedAt = DateTime.UtcNow,
        });

        if (req.Close == true) t.Status = TicketStatus.Closed;
        else if (!isStaff && t.Status == TicketStatus.WaitingCustomer) t.Status = TicketStatus.Open;

        t.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await Ticket(id);
    }
}

public sealed record LicenseActionRequest(string Action, int? Days = null, string? MachineName = null);
public sealed record GenerateLicenseRequest(string UserId, string ProductId, string? PlanId, int Seats, int? ValidDays);
public sealed record RecordDownloadRequest(string ProductId, string? VersionId, string Version, string Kind, string FileName, int SizeMb, bool RequiresLicense);
public sealed record AddMessageRequest(string Body, bool? Close = null);
