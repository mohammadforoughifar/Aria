using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

[ApiController]
[Route("api")]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    public AccountController(AppDbContext db) => _db = db;

    private string? CurrentUserId =>
        Request.Headers.TryGetValue("X-User-Id", out var v) ? v.ToString() : null;

    private static UserDto Map(User u) => new(
        u.Id, u.Email, u.FullName, u.Role, u.Company, u.Phone, u.Country,
        u.CreatedAt, u.LastLoginAt);

    [HttpPost("auth/login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == req.Email.ToLower().Trim() || u.Phone == req.Email.Trim());

        if (user is null || user.PasswordHash != Seed.Hash(req.Password))
            return Unauthorized("ایمیل/موبایل یا رمز عبور نادرست است.");
        if (user.Locked) return Unauthorized("حساب شما قفل شده است.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(Map(user));
    }

    [HttpPost("auth/register")]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Conflict("حسابی با این ایمیل از قبل وجود دارد.");
        if (req.Password.Length < 8) return BadRequest("رمز عبور باید حداقل ۸ کاراکتر باشد.");

        var user = new User
        {
            Email = email, FullName = req.FullName.Trim(), PasswordHash = Seed.Hash(req.Password),
            Company = req.Company, Country = req.Country, AccountType = req.AccountType,
            Role = Role.Customer, CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(Map(user));
    }

    [HttpGet("account/me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        var u = await _db.Users.FindAsync(uid);
        return u is null ? Unauthorized() : Ok(Map(u));
    }

    [HttpPut("account/profile")]
    public async Task<ActionResult<UserDto>> Profile([FromBody] ProfileRequest req)
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        var u = await _db.Users.FindAsync(uid);
        if (u is null) return NotFound();

        u.FullName = req.FullName.Trim();
        u.Company = req.Company;
        u.Phone = req.Phone;
        u.Country = req.Country;
        await _db.SaveChangesAsync();
        return Ok(Map(u));
    }

    [HttpPost("account/password")]
    public async Task<IActionResult> Password([FromBody] PasswordRequest req)
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        var u = await _db.Users.FindAsync(uid);
        if (u is null) return NotFound();
        if (u.PasswordHash != Seed.Hash(req.Current)) return BadRequest("رمزِ فعلی نادرست است.");
        if (req.Next.Length < 8) return BadRequest("رمزِ جدید باید حداقل ۸ کاراکتر باشد.");

        u.PasswordHash = Seed.Hash(req.Next);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("notifications")]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Notifications()
    {
        var uid = CurrentUserId;
        if (uid is null) return Unauthorized();
        return Ok((await _db.Notifications.Where(n => n.UserId == uid)
            .OrderByDescending(n => n.CreatedAt).Take(30).ToListAsync())
            .Select(n => new NotificationDto(n.Id, n.UserId, n.Title, n.Body, n.Kind, n.IsRead, n.CreatedAt, n.Link))
            .ToList());
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> Read(string id)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n is null) return NotFound();
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return Ok();
    }

    /* ------------------------------ تنظیماتِ سایت ----------------------------- */

    [HttpGet("settings")]
    public async Task<ActionResult<SiteSettingsDto>> GetSettings()
    {
        var s = await _db.Settings.FirstAsync();
        return Ok(new SiteSettingsDto(s.SiteName, s.SupportEmail, s.SalesEmail, s.SupportPhone,
            s.HeroHeadline, s.HeroSubheading, s.TaxRateBps, s.TrialDays, s.MaintenanceMode, s.Announcement));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<SiteSettingsDto>> UpdateSettings([FromBody] SiteSettingsDto dto)
    {
        var s = await _db.Settings.FirstAsync();
        s.SiteName = dto.SiteName; s.SupportEmail = dto.SupportEmail; s.SalesEmail = dto.SalesEmail;
        s.SupportPhone = dto.SupportPhone; s.HeroHeadline = dto.HeroHeadline; s.HeroSubheading = dto.HeroSubheading;
        s.TaxRateBps = dto.TaxRateBps; s.TrialDays = dto.TrialDays;
        s.MaintenanceMode = dto.MaintenanceMode; s.Announcement = dto.Announcement;
        s.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(dto);
    }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RegisterRequest(string FullName, string Email, string Password,
    string? Company, string? Country, AccountType AccountType = AccountType.Individual);
public sealed record ProfileRequest(string FullName, string? Company, string? Phone, string? Country);
public sealed record PasswordRequest(string Current, string Next);
