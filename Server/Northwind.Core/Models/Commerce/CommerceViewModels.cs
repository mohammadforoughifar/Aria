using System.ComponentModel.DataAnnotations;
using Northwind.Data.Enums;

namespace Northwind.Core.Models.Commerce;

/* ================================ سبد ================================ */

public sealed record CartItemViewModel(
    Guid Id, Guid PlanId, int Quantity, Guid ProductId, string ProductName,
    string ProductSlug, string PlanName, PlanTier Tier, BillingType BillingType,
    string AccentColor, decimal UnitPriceToman, decimal LineTotalToman);

public sealed record CartViewModel(
    Guid Id, IReadOnlyList<CartItemViewModel> Items, int ItemCount,
    decimal SubtotalToman, decimal DiscountToman, decimal TaxToman, decimal TotalToman,
    string? DiscountCode, string Currency);

public sealed class AddToCartInput
{
    [Required] public Guid PlanId { get; set; }

    [Range(1, 99, ErrorMessage = "تعداد باید بین ۱ و ۹۹ باشد.")]
    public int Quantity { get; set; } = 1;

    /// <summary>کلیدِ سبدِ مهمان؛ برایِ کاربرِ واردشده لازم نیست.</summary>
    [MaxLength(64)] public string? SessionKey { get; set; }
}

public sealed class UpdateCartItemInput
{
    [Range(0, 99, ErrorMessage = "تعداد باید بین ۰ و ۹۹ باشد. صفر یعنی حذف.")]
    public int Quantity { get; set; } = 1;
}

public sealed class ApplyDiscountInput
{
    [Required(ErrorMessage = "کدِ تخفیف لازم است.")]
    [MaxLength(64)]
    public string Code { get; set; } = "";

    [MaxLength(64)] public string? SessionKey { get; set; }
}

/* ============================== تسویه‌حساب ============================== */

public sealed class CheckoutInput
{
    [Required(ErrorMessage = "کلیدِ سبد لازم است.")]
    [MaxLength(64)]
    public string SessionKey { get; set; } = "";

    [MaxLength(64)] public string? DiscountCode { get; set; }

    [MaxLength(32)] public string PaymentMethod { get; set; } = "card";

    [MaxLength(120)] public string? CardName { get; set; }
    [MaxLength(32)] public string? CardNumber { get; set; }

    [Required(ErrorMessage = "نامِ صورتحساب لازم است.")]
    [MaxLength(200)]
    public string BillingName { get; set; } = "";

    [MaxLength(200)] public string? BillingCompany { get; set; }

    [Required(ErrorMessage = "ایمیلِ صورتحساب لازم است.")]
    [EmailAddress(ErrorMessage = "ایمیلِ صورتحساب معتبر نیست.")]
    [MaxLength(256)]
    public string BillingEmail { get; set; } = "";

    [MaxLength(80)] public string BillingCountry { get; set; } = "Iran";

    /// <summary>شناسهٔ مالیاتی/اقتصادی برایِ فاکتورِ رسمی.</summary>
    [MaxLength(32)] public string? VatId { get; set; }
    [MaxLength(2000)] public string? Notes { get; set; }

    public AccountType AccountType { get; set; } = AccountType.Individual;

    [MaxLength(32)] public string? EconomicCode { get; set; }
    [MaxLength(32)] public string? NationalId { get; set; }
    [MaxLength(200)] public string? CompanyName { get; set; }
}

/// <summary>نتیجهٔ تسویه — سفارشِ ساخته‌شده و وضعیتِ پرداخت.</summary>
public sealed record CheckoutResultViewModel(
    OrderViewModel Order, bool PaymentSucceeded, string? Message);

/* =============================== سفارش =============================== */

public sealed record OrderItemViewModel(
    Guid Id, Guid ProductId, Guid PlanId, string ProductName, string PlanName,
    BillingType BillingType, decimal UnitPriceToman, int Quantity, decimal LineTotalToman);

public sealed record OrderViewModel(
    Guid Id, string OrderNumber, Guid UserId, string? CustomerName, string? CustomerEmail,
    string? CustomerCompany, OrderStatus Status, decimal SubtotalToman, decimal DiscountToman,
    decimal TaxToman, decimal TotalToman, string Currency, string? DiscountCode, DateTime PlacedAt,
    string? BillingName, string? BillingCompany, string? BillingEmail, string? BillingCountry,
    string? Notes, IReadOnlyList<OrderItemViewModel> Items,
    PaymentViewModel? Payment, InvoiceViewModel? Invoice);

public sealed class UpdateOrderStatusInput
{
    public OrderStatus Status { get; set; } = OrderStatus.Paid;
    [MaxLength(400)] public string? Note { get; set; }
}

/* =============================== پرداخت =============================== */

public sealed record PaymentViewModel(
    Guid Id, Guid OrderId, string Provider, string Method, decimal AmountToman,
    PaymentStatus Status, string? TransactionRef, DateTime? PaidAt);

/* =============================== فاکتور =============================== */

public sealed record InvoiceViewModel(
    Guid Id, string InvoiceNumber, Guid OrderId, InvoiceStatus Status,
    DateTime IssuedAt, DateTime? DueAt, decimal TotalToman, decimal TaxToman,
    OrderViewModel? Order = null);

/* =============================== لایسنس =============================== */

public sealed record LicenseViewModel(
    Guid Id, string LicenseKey, Guid UserId, string? UserName, string? UserEmail,
    Guid ProductId, string ProductName, string ProductSlug, string? PlanName,
    int Seats, int ActivationsUsed, LicenseStatus Status, DateTime IssuedAt, DateTime? ExpiresAt);

/// <summary>صدورِ لایسنس از سویِ مدیر — <c>POST /api/admin/licenses</c>.</summary>
public sealed class IssueLicenseInput
{
    [Required] public Guid UserId { get; set; }
    [Required] public Guid ProductId { get; set; }
    public Guid? PlanId { get; set; }

    [Range(1, 10000)] public int Seats { get; set; } = 1;

    /// <summary>مدتِ اعتبار به ماه؛ صفر یعنی همیشگی.</summary>
    [Range(0, 120)] public int Months { get; set; } = 12;
}

/// <summary>عملیاتِ رویِ لایسنس: <c>renew</c>، <c>revoke</c>، <c>activate</c>.</summary>
public sealed class LicenseActionInput
{
    [Required(ErrorMessage = "نوعِ عملیات لازم است.")]
    [RegularExpression("^(renew|revoke|activate)$", ErrorMessage = "عملیات باید renew، revoke یا activate باشد.")]
    public string Action { get; set; } = "renew";

    [Range(0, 120)] public int Months { get; set; } = 12;
}

/* ============================== دریافت‌ها ============================== */

public sealed record DownloadViewModel(
    Guid Id, Guid ProductId, string ProductName, string ProductSlug, Guid? VersionId,
    string Version, string Kind, string FileName, int SizeMb, DateTime DownloadedAt,
    bool RequiresLicense, Guid? UserId = null);

public sealed record AvailableVersionViewModel(
    Guid Id, string Version, DateTime ReleasedAt, string Platform, int SizeMb,
    string FileName, string? InstallerUrl, string Notes);

public sealed record AvailableDownloadViewModel(
    Guid ProductId, string ProductName, string ProductSlug, string AccentColor,
    bool Licensed, IReadOnlyList<AvailableVersionViewModel> Versions);

public sealed class RecordDownloadInput
{
    [Required] public Guid ProductId { get; set; }
    public Guid? VersionId { get; set; }
    [MaxLength(32)] public string Kind { get; set; } = "installer";
}

/* ============================= تخفیف ============================= */

public sealed record DiscountCodeViewModel(
    Guid Id, string Code, string? Description, int PercentOff, int MaxRedemptions,
    int TimesRedeemed, DateTime? ExpiresAt, bool IsActive, DateTime CreatedAt);

public sealed class DiscountCodeInput
{
    [Required(ErrorMessage = "کدِ تخفیف لازم است.")]
    [MaxLength(64)]
    public string Code { get; set; } = "";

    [MaxLength(400)] public string? Description { get; set; }

    [Range(1, 100, ErrorMessage = "درصدِ تخفیف باید بین ۱ و ۱۰۰ باشد.")]
    public int PercentOff { get; set; }

    /// <summary>سقفِ استفاده؛ صفر یعنی بی‌نهایت.</summary>
    [Range(0, 1000000)] public int MaxRedemptions { get; set; } = 100;

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
