using System.ComponentModel.DataAnnotations;
using Northwind.Domain.Enums;

namespace Northwind.Api.ViewModels.Account;

/* ============================== کاربر ============================== */

public sealed record UserViewModel(
    Guid Id, string Email, string FullName, Role Role,
    string? Company, string? Phone, string? Country,
    DateTime CreatedAt, DateTime? LastLoginAt);

/// <summary>سطرِ جدولِ مشتریان در پنلِ مدیر، با آمارِ خرید.</summary>
public sealed record CustomerViewModel(
    Guid Id, string Email, string FullName, Role Role, string? Company, string? Phone,
    string? Country, DateTime CreatedAt, DateTime? LastLoginAt,
    int OrderCount, int TotalSpentToman, int LicenseCount);

/* ============================== احراز هویت ============================== */

public sealed class LoginInput
{
    [Required(ErrorMessage = "ایمیل لازم است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "رمز عبور لازم است.")]
    [MinLength(6, ErrorMessage = "رمز عبور حداقل ۶ نویسه است.")]
    [MaxLength(128)]
    public string Password { get; set; } = "";
}

public sealed class RegisterInput
{
    [Required(ErrorMessage = "نام و نامِ خانوادگی لازم است.")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "ایمیل لازم است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "رمز عبور لازم است.")]
    [MinLength(6, ErrorMessage = "رمز عبور حداقل ۶ نویسه است.")]
    [MaxLength(128)]
    public string Password { get; set; } = "";

    [MaxLength(32)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(80)] public string? Country { get; set; }

    public AccountType AccountType { get; set; } = AccountType.Individual;
    [MaxLength(32)] public string? EconomicCode { get; set; }
    [MaxLength(32)] public string? NationalId { get; set; }
}

/// <summary>
/// پاسخِ ورود. در مرحلهٔ سرویس‌ها با JWT پر می‌شود؛ فعلاً شکلِ قرارداد را تثبیت می‌کند.
/// </summary>
public sealed record AuthResultViewModel(
    UserViewModel User, string Token, DateTime ExpiresAt);

public sealed class UpdateProfileInput
{
    [Required(ErrorMessage = "نام و نامِ خانوادگی لازم است.")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [MaxLength(32)] public string? Phone { get; set; }
    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(80)] public string? Country { get; set; }
    [MaxLength(32)] public string? EconomicCode { get; set; }
    [MaxLength(32)] public string? NationalId { get; set; }
}

public sealed class ChangePasswordInput
{
    [Required(ErrorMessage = "رمزِ فعلی لازم است.")]
    [MaxLength(128)]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "رمزِ جدید لازم است.")]
    [MinLength(6, ErrorMessage = "رمزِ جدید حداقل ۶ نویسه است.")]
    [MaxLength(128)]
    public string NewPassword { get; set; } = "";
}

/* ============================ مدیریتِ کاربران ============================ */

public sealed class UpdateCustomerInput
{
    public Role Role { get; set; } = Role.Customer;
    public bool Locked { get; set; }
}
