// مدل‌هایِ JSON که API نسخهٔ ۱ برمی‌گرداند.
//
// چرا جدا از قراردادهایِ مشترک (Northwind.Shared)؟
//   قراردادهایِ مشترک برایِ پشتهٔ قدیمی نوشته شده‌اند (شناسه‌ها رشته، مبلغ‌ها int).
//   نسخهٔ ۱ شناسه‌ها را Guid و مبلغ‌ها را decimal می‌فرستد. این‌جا همان شکلِ
//   واقعیِ پاسخ‌ها آمده تا هیچ تبدیلی در مسیرِ نمایش گم نشود.
//
// enum‌ها از قراردادهایِ مشترک قرض گرفته شده‌اند چون نامِ مقادیرشان یکی است و
// سرور آن‌ها را به‌صورتِ رشته می‌فرستد؛ این‌طور نگاشتِ دستی لازم نیست.

using Northwind.Shared.Contracts;

namespace Northwind.Client.Services.V1;

/* ------------------------------ پیمایش ------------------------------ */

public sealed record V1PageMeta(int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record V1Paged<T>(IReadOnlyList<T> Items, V1PageMeta Meta);

/* ------------------------------- کاتالوگ ------------------------------ */

public sealed record V1Category(
    Guid Id, string Name, string Slug, string? Description, string Icon, int ProductCount);

public sealed record V1Plan(
    Guid Id, string Name, PlanTier Tier, BillingType BillingType,
    decimal PriceToman, int Seats, string? Description, bool IsPopular, int LicenseMonths);

public sealed record V1ProductVersion(
    Guid Id, string Version, DateTime ReleasedAt, string Notes,
    string? InstallerUrl, int SizeMb, string Platform);

public sealed record V1Review(
    Guid Id, Guid ProductId, string? ProductName, string AuthorName, string? AuthorCompany,
    int Rating, string Title, string Body, ReviewStatus Status, DateTime CreatedAt);

public sealed record V1ProductListItem(
    Guid Id, string Slug, string Name, string Tagline, ProductType Type,
    string Platform, string Version, decimal BasePriceToman, decimal RatingAvg, int RatingCount,
    int Popularity, string? HeroImage, string AccentColor, bool IsFeatured, bool IsActive,
    DateTime CreatedAt, string? CategoryName, string? CategorySlug, int PlanCount);

public sealed record V1ProductDetail(
    Guid Id, string Slug, string Name, string Tagline, string Description,
    ProductType Type, Audience Audience, string Platform, string Version,
    decimal BasePriceToman, decimal RatingAvg, int RatingCount, int Popularity,
    string AccentColor, string? HeroImage, bool IsActive, bool IsFeatured, int TrialDays,
    DateTime CreatedAt, Guid CategoryId, string CategoryName, string CategorySlug,
    IReadOnlyList<string> Features, IReadOnlyList<string> SystemRequirements,
    IReadOnlyList<string> Gallery, string? DemoVideoUrl,
    IReadOnlyList<V1Plan> Plans, IReadOnlyList<V1ProductVersion> Versions,
    IReadOnlyList<V1Review> Reviews);

/* -------------------------------- سبد -------------------------------- */

public sealed record V1CartItem(
    Guid Id, Guid PlanId, int Quantity, Guid ProductId, string ProductName,
    string ProductSlug, string PlanName, PlanTier Tier, BillingType BillingType,
    string AccentColor, decimal UnitPriceToman, decimal LineTotalToman);

public sealed record V1Cart(
    Guid Id, IReadOnlyList<V1CartItem> Items, int ItemCount,
    decimal SubtotalToman, decimal DiscountToman, decimal TaxToman, decimal TotalToman,
    string? DiscountCode, string Currency);

public sealed record V1Payment(
    Guid Id, Guid OrderId, string Provider, string Method, decimal AmountToman,
    PaymentStatus Status, string? TransactionRef, DateTime? PaidAt);

public sealed record V1Invoice(
    Guid Id, string InvoiceNumber, Guid OrderId, InvoiceStatus Status,
    DateTime IssuedAt, DateTime? DueAt, decimal TotalToman, decimal TaxToman);

public sealed record V1OrderItem(
    Guid Id, Guid ProductId, Guid PlanId, string ProductName, string PlanName,
    BillingType BillingType, decimal UnitPriceToman, int Quantity, decimal LineTotalToman);

public sealed record V1Order(
    Guid Id, string OrderNumber, Guid UserId, string? CustomerName, string? CustomerEmail,
    string? CustomerCompany, OrderStatus Status, decimal SubtotalToman, decimal DiscountToman,
    decimal TaxToman, decimal TotalToman, string Currency, string? DiscountCode, DateTime PlacedAt,
    string? BillingName, string? BillingCompany, string? BillingEmail, string? BillingCountry,
    string? Notes, IReadOnlyList<V1OrderItem> Items, V1Payment? Payment, V1Invoice? Invoice);

public sealed record V1CheckoutResult(V1Order Order, bool PaymentSucceeded, string? Message);

/* -------------------------------- محتوا ------------------------------- */

public sealed record V1Settings(
    string SiteName, string SupportEmail, string SalesEmail, string SupportPhone,
    string HeroHeadline, string HeroSubheading, int TaxRateBps, int TrialDays,
    bool MaintenanceMode, string Announcement);

public sealed record V1Testimonial(
    Guid Id, string AuthorName, string AuthorRole, string Company, string Quote, int Rating);

public sealed record V1Faq(Guid Id, string Question, string Answer);

public sealed record V1Article(
    Guid Id, string Slug, string Title, string Excerpt, string Body,
    string? Category, bool IsPublished, DateTime PublishedAt, int ReadMinutes);

/* -------------------------------- خطا -------------------------------- */

/// <summary>بدنهٔ خطایِ استانداردِ نسخهٔ ۱: <c>{ code, message, status, traceId }</c>.</summary>
public sealed record V1Error(string? Code, string? Message, int Status, string? TraceId);
