using System.ComponentModel.DataAnnotations;
using Northwind.Api.ViewModels.Commerce;
using Northwind.Api.ViewModels.Support;

namespace Northwind.Api.ViewModels.Content;

/* =============================== مقاله =============================== */

public sealed record ArticleViewModel(
    Guid Id, string Slug, string Title, string Excerpt, string Body,
    string? Category, bool IsPublished, DateTime PublishedAt, int ReadMinutes);

public sealed class ArticleInput
{
    [Required(ErrorMessage = "عنوانِ مقاله لازم است.")]
    [MaxLength(240)]
    public string Title { get; set; } = "";

    [MaxLength(160)] public string Slug { get; set; } = "";

    [Required(ErrorMessage = "خلاصهٔ مقاله لازم است.")]
    [MaxLength(600)]
    public string Excerpt { get; set; } = "";

    [Required(ErrorMessage = "متنِ مقاله لازم است.")]
    public string Body { get; set; } = "";

    [MaxLength(80)] public string? Category { get; set; }
    public bool IsPublished { get; set; } = true;
    [Range(1, 120)] public int ReadMinutes { get; set; } = 4;
}

/* ============================== نقلِ‌قول ============================== */

public sealed record TestimonialViewModel(
    Guid Id, string AuthorName, string AuthorRole, string Company, string Quote, int Rating);

/* ============================ پرسش‌هایِ پرتکرار ============================ */

public sealed record FaqViewModel(Guid Id, string Question, string Answer);

public sealed class FaqInput
{
    [Required(ErrorMessage = "پرسش لازم است.")]
    [MaxLength(400)]
    public string Question { get; set; } = "";

    [Required(ErrorMessage = "پاسخ لازم است.")]
    [MaxLength(4000)]
    public string Answer { get; set; } = "";

    public int SortOrder { get; set; }
}

/* ============================== تنظیمات ============================== */

public sealed record SiteSettingsViewModel(
    string SiteName, string SupportEmail, string SalesEmail, string SupportPhone,
    string HeroHeadline, string HeroSubheading, int TaxRateBps, int TrialDays,
    bool MaintenanceMode, string Announcement);

public sealed class SiteSettingsInput
{
    [Required] [MaxLength(160)] public string SiteName { get; set; } = "";
    [EmailAddress] [MaxLength(256)] public string SupportEmail { get; set; } = "";
    [EmailAddress] [MaxLength(256)] public string SalesEmail { get; set; } = "";
    [MaxLength(32)] public string SupportPhone { get; set; } = "";
    [MaxLength(240)] public string HeroHeadline { get; set; } = "";
    [MaxLength(600)] public string HeroSubheading { get; set; } = "";

    [Range(0, 10000, ErrorMessage = "نرخِ مالیات باید بین ۰ و ۱۰۰ درصد باشد.")]
    public int TaxRateBps { get; set; } = 900;

    [Range(0, 365)] public int TrialDays { get; set; } = 14;
    public bool MaintenanceMode { get; set; }
    [MaxLength(400)] public string? Announcement { get; set; }
}

/* ============================== داشبورد ============================== */

public sealed record RevenuePointViewModel(string Label, int TotalToman);

public sealed record TopProductViewModel(
    Guid ProductId, string Name, string Slug, int UnitsSold, int RevenueToman, string AccentColor);

/// <summary>خلاصهٔ پنلِ مدیر — <c>GET /api/admin/overview</c>.</summary>
public sealed record AdminOverviewViewModel(
    int TotalProducts, int TotalCustomers, int OpenTickets, int PendingReviews,
    int ActiveLicenses, int ExpiringLicenses,
    int RevenueThisMonthToman, int RevenueLastMonthToman,
    IReadOnlyList<RevenuePointViewModel> RevenueSeries,
    IReadOnlyList<TopProductViewModel> TopProducts,
    IReadOnlyList<OrderViewModel> RecentOrders,
    IReadOnlyList<TicketViewModel> RecentTickets);

/// <summary>خلاصهٔ پورتالِ مشتری — <c>GET /api/account/overview</c>.</summary>
public sealed record CustomerOverviewViewModel(
    int OrderCount, int ActiveLicenses, int ExpiringLicenses, int OpenTickets,
    int TotalSpentToman, int AvailableDownloads, int OutstandingInvoicesToman,
    IReadOnlyList<OrderViewModel> RecentOrders,
    IReadOnlyList<LicenseViewModel> ExpiringLicensesList,
    IReadOnlyList<RevenuePointViewModel> SpendSeries);
