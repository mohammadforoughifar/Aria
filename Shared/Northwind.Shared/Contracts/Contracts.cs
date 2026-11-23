// قراردادهای مشترک بین کلاینت و سرور.
// این تایپ‌ها همان چیزی هستند که API برمی‌گرداند و کلاینت مصرف می‌کند؛
// با System.Text.Json سریالایز می‌شوند و در دو سمت یکی‌اند.
// نکته: مبالغ همیشه به «تومانِ کامل» (عددِ صحیح) هستند و تبدیل به ریال/ارقام
// فارسی فقط در لایهٔ نمایش انجام می‌شود.

namespace Northwind.Shared.Contracts;

public enum Role { Admin, Customer }
public enum ProductType { Module, DesktopApp, AddOn }
public enum PlanTier { SingleUser, MultiUser, Enterprise }
public enum BillingType { OneTime, Annual }
public enum OrderStatus { Pending, Paid, Processing, Completed, Refunded, Cancelled }
public enum PaymentStatus { Pending, Succeeded, Failed, Refunded }
public enum LicenseStatus { Active, Expired, Revoked }
public enum TicketStatus { Open, InProgress, WaitingCustomer, Resolved, Closed }
public enum TicketPriority { Low, Normal, High, Critical }
public enum ReviewStatus { Pending, Approved, Rejected }
public enum InvoiceStatus { Draft, Issued, Paid, Void }

public sealed record PagedMeta(int Page, int PageSize, int TotalCount, int TotalPages);
public sealed record Paged<T>(IReadOnlyList<T> Items, PagedMeta Meta)
{
    public static Paged<T> Empty(int pageSize = 12) => new(Array.Empty<T>(), new PagedMeta(1, pageSize, 0, 1));
}

/* ------------------------------- Identity ------------------------------- */

public sealed record UserDto(
    string Id, string Email, string FullName, Role Role,
    string? Company, string? Phone, string? Country,
    DateTime CreatedAt, DateTime? LastLoginAt);

/* ------------------------------- Catalog -------------------------------- */

public sealed record CategoryDto(string Id, string Name, string Slug, string? Description, string Icon, int ProductCount);

public sealed record PlanDto(
    string Id, string Name, PlanTier Tier, BillingType BillingType,
    decimal PriceToman, int Seats, string? Description, bool IsPopular,
    int LicenseMonths = 12);

public sealed record ProductVersionDto(
    string Id, string Version, DateTime ReleasedAt, string Notes,
    string? InstallerUrl, int SizeMb, string Platform);

public sealed record ReviewDto(
    string Id, string ProductId, string? ProductName, string AuthorName, string? AuthorCompany,
    int Rating, string Title, string Body, ReviewStatus Status, DateTime CreatedAt);

public sealed record ProductListItemDto(
    string Id, string Slug, string Name, string Tagline, ProductType Type,
    string Platform, string Version, decimal BasePriceToman, decimal RatingAvg, int RatingCount,
    int Popularity, string? HeroImage, string AccentColor, bool IsFeatured, bool IsActive,
    DateTime CreatedAt, string? CategoryName, string? CategorySlug, int PlanCount);

public sealed record ProductDetailDto(
    string Id, string Slug, string Name, string Tagline, string Description,
    ProductType Type, string Platform, string Version, decimal BasePriceToman,
    decimal RatingAvg, int RatingCount, int Popularity, string AccentColor,
    string? HeroImage, bool IsActive,
    bool IsFeatured, int TrialDays, DateTime CreatedAt,
    string CategoryId, string CategoryName, string CategorySlug,
    IReadOnlyList<string> Features, IReadOnlyList<string> SystemRequirements,
    IReadOnlyList<string> Gallery, string? DemoVideoUrl,
    IReadOnlyList<PlanDto> Plans, IReadOnlyList<ProductVersionDto> Versions,
    IReadOnlyList<ReviewDto> Reviews);

/* ------------------------------- Commerce ------------------------------- */

public sealed record CartItemDto(
    string Id, string PlanId, int Quantity, string ProductId, string ProductName,
    string ProductSlug, string PlanName, PlanTier Tier, BillingType BillingType,
    string AccentColor, decimal UnitPriceToman, decimal LineTotalToman);

public sealed record CartDto(
    string Id, IReadOnlyList<CartItemDto> Items, int ItemCount,
    decimal SubtotalToman, decimal DiscountToman, decimal TaxToman, decimal TotalToman,
    string? DiscountCode, string Currency);

public sealed record OrderItemDto(
    string Id, string ProductId, string PlanId, string ProductName, string PlanName,
    BillingType BillingType, decimal UnitPriceToman, int Quantity, decimal LineTotalToman);

public sealed record OrderDto(
    string Id, string OrderNumber, string UserId, string? CustomerName, string? CustomerEmail,
    string? CustomerCompany, OrderStatus Status, decimal SubtotalToman, decimal DiscountToman,
    decimal TaxToman, decimal TotalToman, string Currency, string? DiscountCode, DateTime PlacedAt,
    string? BillingName, string? BillingCompany, string? BillingEmail, string? BillingCountry,
    string? Notes, IReadOnlyList<OrderItemDto> Items, PaymentDto? Payment, InvoiceDto? Invoice);

public sealed record PaymentDto(
    string Id, string OrderId, string Provider, string Method, decimal AmountToman,
    PaymentStatus Status, string? TransactionRef, DateTime? PaidAt);

public sealed record InvoiceDto(
    string Id, string InvoiceNumber, string OrderId, InvoiceStatus Status,
    DateTime IssuedAt, DateTime? DueAt, decimal TotalToman, decimal TaxToman, OrderDto? Order);

public sealed record LicenseDto(
    string Id, string LicenseKey, string UserId, string? UserName, string? UserEmail,
    string ProductId, string ProductName, string ProductSlug, string? PlanName,
    int Seats, int ActivationsUsed, LicenseStatus Status, DateTime IssuedAt, DateTime? ExpiresAt);

public sealed record DownloadDto(
    string Id, string ProductId, string ProductName, string ProductSlug, string? VersionId,
    string Version, string Kind, string FileName, int SizeMb, DateTime DownloadedAt, bool RequiresLicense,
    string? UserId = null);

public sealed record AvailableVersionDto(
    string Id, string Version, DateTime ReleasedAt, string Platform, int SizeMb,
    string FileName, string? InstallerUrl, string Notes);

public sealed record AvailableDownloadDto(
    string ProductId, string ProductName, string ProductSlug, string AccentColor,
    bool Licensed, IReadOnlyList<AvailableVersionDto> Versions);

public sealed record DiscountCodeDto(
    string Id, string Code, string? Description, int PercentOff, int MaxRedemptions,
    int TimesRedeemed, DateTime? ExpiresAt, bool IsActive, DateTime CreatedAt);

/* ------------------------------- Support -------------------------------- */

public sealed record TicketMessageDto(
    string Id, string TicketId, string AuthorName, bool IsStaff, string Body, DateTime CreatedAt);

public sealed record TicketDto(
    string Id, string Reference, string UserId, string? UserName, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);

public sealed record TicketDetailDto(
    string Id, string Reference, string UserId, string? UserName, string? ProductId, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<TicketMessageDto> Messages);

/* ------------------------------ Dashboard ------------------------------- */

public sealed record CustomerDto(
    string Id, string Email, string FullName, Role Role, string? Company, string? Phone,
    string? Country, DateTime CreatedAt, DateTime? LastLoginAt,
    int OrderCount, decimal TotalSpentToman, int LicenseCount);

public sealed record SiteSettingsDto(
    string SiteName, string SupportEmail, string SalesEmail, string SupportPhone,
    string HeroHeadline, string HeroSubheading, int TaxRateBps, int TrialDays,
    bool MaintenanceMode, string Announcement);

public sealed record TestimonialDto(string Id, string AuthorName, string AuthorRole, string Company, string Quote, int Rating);
public sealed record FaqDto(string Id, string Question, string Answer);

public sealed record RevenuePointDto(string Label, decimal TotalToman);
public sealed record TopProductDto(string ProductId, string Name, string Slug, int UnitsSold, decimal RevenueToman, string AccentColor);

public sealed record AdminOverviewDto(
    int TotalProducts, int TotalCustomers, int OpenTickets, int PendingReviews,
    int ActiveLicenses, int ExpiringLicenses,
    decimal RevenueThisMonthToman, decimal RevenueLastMonthToman,
    IReadOnlyList<RevenuePointDto> RevenueSeries,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<OrderDto> RecentOrders,
    IReadOnlyList<TicketDto> RecentTickets);

public sealed record CustomerOverviewDto(
    int OrderCount, int ActiveLicenses, int ExpiringLicenses, int OpenTickets,
    decimal TotalSpentToman, int AvailableDownloads, decimal OutstandingInvoicesToman,
    IReadOnlyList<OrderDto> RecentOrders,
    IReadOnlyList<LicenseDto> ExpiringLicensesList,
    IReadOnlyList<RevenuePointDto> SpendSeries);

/* ============================== نیازمندیِ جدید ============================== */

/// <summary>مخاطبِ محصول: سازمانی (B2B)، شخصی (B2C) یا هر دو.</summary>
public enum Audience { B2B, B2C, Both }

/// <summary>نوعِ حساب در ثبت‌نام: حقیقی یا حقوقی.</summary>
public enum AccountType { Individual, Organization }

public sealed record NotificationDto(
    string Id, string UserId, string Title, string? Body,
    string Kind, bool IsRead, DateTime CreatedAt, string? Link);

public sealed record ArticleDto(
    string Id, string Slug, string Title, string Excerpt, string Body,
    string? Category, bool IsPublished, DateTime PublishedAt, int ReadMinutes);

/// <summary>درخواستِ دمو / مشاورهٔ سازمانی.</summary>
public sealed record DemoRequestDto(
    string Id, string? ProductId, string? ProductName, string Name, string Email,
    string? Company, string? CompanySize, string? Phone, string? Message,
    DemoRequestStatus Status, DateTime CreatedAt);

public enum DemoRequestStatus { New, Contacted, Qualified, Closed }

/// <summary>پیامِ فرمِ تماس با ما.</summary>
public sealed record ContactMessageDto(
    string Id, string Name, string Email, string? Company, string Topic,
    string Message, bool IsRead, DateTime CreatedAt);

/// <summary>طرحِ قیمت با جزئیاتِ کامل برای صفحهٔ مقایسه.</summary>
public sealed record PlanDetailDto(
    string Id, string ProductId, string Name, PlanTier Tier, BillingType BillingType,
    decimal PriceToman, int Seats, string? Description, bool IsPopular,
    int LicenseMonths, IReadOnlyList<string> Features);

/// <summary>ورودیِ تسویه‌حساب با فیلدهای فاکتورِ رسمی.</summary>
public sealed record CheckoutDto(
    string SessionKey, string? DiscountCode, string PaymentMethod,
    string? CardName, string? CardNumber,
    string BillingName, string? BillingCompany, string BillingEmail, string BillingCountry,
    string? VatId, string? Notes,
    AccountType AccountType, string? EconomicCode, string? NationalId, string? CompanyName);

/// <summary>ورودیِ ایجادِ تیکتِ پشتیبانی.</summary>
public sealed record NewTicketInput(string? ProductId, string Subject, string Category,
    TicketPriority Priority, string? Environment, string Message);

/// <summary>ورودیِ ثبتِ دیدگاه روی محصول.</summary>
public sealed record NewReviewInput(string ProductId, int Rating, string Title, string Body);

/// <summary>ورودیِ ساخت/ویرایشِ مقالهٔ وبلاگ.</summary>
public sealed record ArticleInput(
    string? Id, string Title, string Slug, string Excerpt, string Body,
    string? Category, bool IsPublished, int ReadMinutes);

/// <summary>تغییرِ وضعیتِ درخواستِ دمو توسط مدیر.</summary>
public sealed record DemoStatusInput(DemoRequestStatus Status);
