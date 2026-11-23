using System.ComponentModel.DataAnnotations;
using Northwind.Api.ViewModels.Common;
using Northwind.Domain.Enums;

namespace Northwind.Api.ViewModels.Catalog;

/* ============================== دسته‌ها ============================== */

public sealed record CategoryViewModel(
    Guid Id, string Name, string Slug, string? Description, string Icon, int ProductCount);

/// <summary>بدنهٔ ساخت/ویرایشِ دسته — <c>POST /api/categories</c>، <c>PUT /api/categories/{id}</c>.</summary>
public sealed class CategoryInput
{
    [Required(ErrorMessage = "نامِ دسته لازم است.")]
    [MaxLength(120, ErrorMessage = "نامِ دسته حداکثر ۱۲۰ نویسه است.")]
    public string Name { get; set; } = "";

    [MaxLength(140)]
    public string Slug { get; set; } = "";

    [MaxLength(600)]
    public string? Description { get; set; }

    [MaxLength(64)]
    public string Icon { get; set; } = "package";

    public int SortOrder { get; set; }
}

/* ============================== محصولات ============================== */

/// <summary>سطرِ فهرست — سبک، برایِ صفحهٔ کاتالوگ.</summary>
public sealed record ProductListItemViewModel(
    Guid Id, string Slug, string Name, string Tagline, ProductType Type,
    string Platform, string Version, int BasePriceToman, decimal RatingAvg, int RatingCount,
    int Popularity, string? HeroImage, string AccentColor, bool IsFeatured, bool IsActive,
    DateTime CreatedAt, string? CategoryName, string? CategorySlug, int PlanCount);

/// <summary>صفحهٔ جزئیات — همه‌چیز، بدونِ درخواستِ دوم.</summary>
public sealed record ProductDetailViewModel(
    Guid Id, string Slug, string Name, string Tagline, string Description,
    ProductType Type, Audience Audience, string Platform, string Version, int BasePriceToman,
    decimal RatingAvg, int RatingCount, int Popularity, string AccentColor,
    string? HeroImage, bool IsActive, bool IsFeatured, int TrialDays, DateTime CreatedAt,
    Guid CategoryId, string CategoryName, string CategorySlug,
    IReadOnlyList<string> Features, IReadOnlyList<string> SystemRequirements,
    IReadOnlyList<string> Gallery, string? DemoVideoUrl,
    IReadOnlyList<PlanViewModel> Plans,
    IReadOnlyList<ProductVersionViewModel> Versions,
    IReadOnlyList<ReviewViewModel> Reviews);

public sealed class ProductInput
{
    [Required(ErrorMessage = "نامِ محصول لازم است.")]
    [MaxLength(160)]
    public string Name { get; set; } = "";

    [MaxLength(140)]
    public string Slug { get; set; } = "";

    [MaxLength(240)]
    public string Tagline { get; set; } = "";

    [Required(ErrorMessage = "توضیحات لازم است.")]
    public string Description { get; set; } = "";

    public ProductType Type { get; set; } = ProductType.Module;
    public Audience Audience { get; set; } = Audience.Both;

    [MaxLength(40)] public string Platform { get; set; } = "Windows";
    [MaxLength(32)] public string Version { get; set; } = "1.0.0";

    [Range(0, int.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد.")]
    public int BasePriceToman { get; set; }

    [MaxLength(16)] public string AccentColor { get; set; } = "#1E3A8A";
    [MaxLength(400)] public string? HeroImage { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;

    [Range(0, 365)] public int TrialDays { get; set; } = 14;
    public int Popularity { get; set; }

    public List<string> Features { get; set; } = new();
    public List<string> SystemRequirements { get; set; } = new();
    public List<string> Gallery { get; set; } = new();

    [MaxLength(400)] public string? DemoVideoUrl { get; set; }

    [Required(ErrorMessage = "دستهٔ محصول لازم است.")]
    public Guid CategoryId { get; set; }
}

/* ============================== طرح‌ها ============================== */

public sealed record PlanViewModel(
    Guid Id, string Name, PlanTier Tier, BillingType BillingType,
    int PriceToman, int Seats, string? Description, bool IsPopular, int LicenseMonths);

/// <summary>طرحِ کامل با ویژگی‌ها — برایِ صفحهٔ مقایسه.</summary>
public sealed record PlanDetailViewModel(
    Guid Id, Guid ProductId, string Name, PlanTier Tier, BillingType BillingType,
    int PriceToman, int Seats, string? Description, bool IsPopular,
    int LicenseMonths, IReadOnlyList<string> Features);

public sealed class PlanInput
{
    [Required(ErrorMessage = "نامِ طرح لازم است.")]
    [MaxLength(120)]
    public string Name { get; set; } = "";

    public PlanTier Tier { get; set; } = PlanTier.SingleUser;
    public BillingType BillingType { get; set; } = BillingType.OneTime;

    [Range(0, int.MaxValue, ErrorMessage = "قیمت نمی‌تواند منفی باشد.")]
    public int PriceToman { get; set; }

    [Range(1, 10000)] public int Seats { get; set; } = 1;

    [MaxLength(600)] public string? Description { get; set; }

    public bool IsPopular { get; set; }

    [Range(0, 120)] public int LicenseMonths { get; set; } = 12;

    public List<string> Features { get; set; } = new();
    public int SortOrder { get; set; }
}

/* ============================== نسخه‌ها ============================== */

public sealed record ProductVersionViewModel(
    Guid Id, string Version, DateTime ReleasedAt, string Notes,
    string? InstallerUrl, int SizeMb, string Platform);

public sealed class ProductVersionInput
{
    [Required(ErrorMessage = "شمارهٔ نسخه لازم است.")]
    [MaxLength(32)]
    public string Version { get; set; } = "";

    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(4000)] public string Notes { get; set; } = "";
    [MaxLength(400)] public string? InstallerUrl { get; set; }
    [Range(0, 100000)] public int SizeMb { get; set; }
    [MaxLength(40)] public string Platform { get; set; } = "Windows";
}

/* ============================== دیدگاه‌ها ============================== */

public sealed record ReviewViewModel(
    Guid Id, Guid ProductId, string? ProductName, string AuthorName, string? AuthorCompany,
    int Rating, string Title, string Body, ReviewStatus Status, DateTime CreatedAt);

public sealed class ReviewInput
{
    [Required(ErrorMessage = "محصول لازم است.")]
    public Guid ProductId { get; set; }

    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "عنوانِ دیدگاه لازم است.")]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "متنِ دیدگاه لازم است.")]
    [MaxLength(4000)]
    public string Body { get; set; } = "";
}

/// <summary>تغییرِ وضعیتِ دیدگاه توسطِ مدیر — <c>PATCH /api/admin/reviews/{id}/status</c>.</summary>
public sealed class ReviewStatusInput
{
    public ReviewStatus Status { get; set; } = ReviewStatus.Approved;
}
