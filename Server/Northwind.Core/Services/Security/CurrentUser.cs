using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Northwind.Core.Services.Security;

/// <summary>کاربرِ جاریِ درخواست — از توکنِ JWT خوانده می‌شود.</summary>
public interface ICurrentUser
{
    Guid? Id { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }

    /// <summary>شناسه را می‌گیرد یا ۴۰۱ می‌دهد.</summary>
    Guid RequireId();

    void RequireAdmin();
}

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public bool IsAdmin => Principal?.IsInRole("Admin") == true;

    public Guid RequireId() => Id ?? throw new UnauthorizedAccessException("ورود لازم است.");

    public void RequireAdmin()
    {
        if (!IsAdmin) throw new Services.Common.ForbiddenException("این عملیات فقط برایِ مدیر است.");
    }
}
