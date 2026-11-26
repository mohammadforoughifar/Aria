// مدل‌هایِ JSON که API نسخهٔ ۱ برمی‌گرداند.
//
// چرا جدا از قراردادهایِ صفحه‌ها (Northwind.Client.Contracts)؟
//   قراردادهایِ مشترک برایِ پشتهٔ قدیمی نوشته شده‌اند (شناسه‌ها رشته، مبلغ‌ها int).
//   نسخهٔ ۱ شناسه‌ها را Guid و مبلغ‌ها را decimal می‌فرستد. این‌جا همان شکلِ
//   واقعیِ پاسخ‌ها آمده تا هیچ تبدیلی در مسیرِ نمایش گم نشود.
//
// enum‌ها از قراردادهایِ مشترک قرض گرفته شده‌اند چون نامِ مقادیرشان یکی است و
// سرور آن‌ها را به‌صورتِ رشته می‌فرستد؛ این‌طور نگاشتِ دستی لازم نیست.

using Northwind.Client.Contracts;

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
public sealed record V1Error(
    string? Code, string? Message, int Status, string? TraceId,
    IDictionary<string, string[]>? Errors = null);

/* ------------------------------ حساب کاربری ----------------------------- */

public sealed record V1User(
    Guid Id, string Email, string FullName, Role Role,
    string? Company, string? Phone, string? Country,
    DateTime CreatedAt, DateTime? LastLoginAt);

/// <summary>پاسخِ ورود/ثبت‌نام: کاربر و توکنِ دسترسی.</summary>
public sealed record V1AuthResult(V1User User, string Token, DateTime ExpiresAt);

public sealed record V1License(
    Guid Id, string LicenseKey, Guid UserId, string? UserName, string? UserEmail,
    Guid ProductId, string ProductName, string ProductSlug, string? PlanName,
    int Seats, int ActivationsUsed, LicenseStatus Status, DateTime IssuedAt, DateTime? ExpiresAt);

public sealed record V1AvailableVersion(
    Guid Id, string Version, DateTime ReleasedAt, string Platform, int SizeMb,
    string FileName, string? InstallerUrl, string Notes);

public sealed record V1AvailableDownload(
    Guid ProductId, string ProductName, string ProductSlug, string AccentColor,
    bool Licensed, IReadOnlyList<V1AvailableVersion> Versions);

public sealed record V1Download(
    Guid Id, Guid ProductId, string ProductName, string ProductSlug, Guid? VersionId,
    string Version, string Kind, string FileName, int SizeMb, DateTime DownloadedAt,
    bool RequiresLicense);

public sealed record V1TicketMessage(
    Guid Id, Guid TicketId, string AuthorName, bool IsStaff, string Body, DateTime CreatedAt);

public sealed record V1Ticket(
    Guid Id, string Reference, Guid UserId, string? UserName, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);

public sealed record V1TicketDetail(
    Guid Id, string Reference, Guid UserId, string? UserName, Guid? ProductId, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<V1TicketMessage> Messages);

public sealed record V1RevenuePoint(string Label, decimal TotalToman);

public sealed record V1CustomerOverview(
    int OrderCount, int ActiveLicenses, int ExpiringLicenses, int OpenTickets,
    decimal TotalSpentToman, int AvailableDownloads, decimal OutstandingInvoicesToman,
    IReadOnlyList<V1Order> RecentOrders,
    IReadOnlyList<V1License> ExpiringLicensesList,
    IReadOnlyList<V1RevenuePoint> SpendSeries);

public sealed record V1Notification(
    Guid Id, Guid UserId, string Title, string? Body, string Kind, bool IsRead,
    DateTime CreatedAt, string? Link);

/* ------------------------------ خطایِ اعتبارسنجی ------------------------------ */

/* ============================== موج ۳: مدیر ============================== */

/// <summary>یک ردیف از «۱۰ محصولِ پرفروش» در نمایِ کلیِ مدیر.</summary>
public sealed record V1TopProduct(
    Guid ProductId, string Name, string Slug, int UnitsSold, decimal RevenueToman, string AccentColor);

/// <summary>نمایِ کلیِ پنلِ مدیریت — <c>GET /api/v1/admin/overview</c>.</summary>
public sealed record V1AdminOverview(
    int TotalProducts, int TotalCustomers, int OpenTickets, int PendingReviews,
    int ActiveLicenses, int ExpiringLicenses,
    decimal RevenueThisMonthToman, decimal RevenueLastMonthToman,
    IReadOnlyList<V1RevenuePoint> RevenueSeries,
    IReadOnlyList<V1TopProduct> TopProducts,
    IReadOnlyList<V1Order> RecentOrders,
    IReadOnlyList<V1Ticket> RecentTickets);

/// <summary>کاربر در فهرستِ مدیران — <c>GET /api/v1/admin/customers</c>.</summary>
public sealed record V1Customer(
    Guid Id, string Email, string FullName, Role Role, string? Company, string? Country,
    DateTime CreatedAt, int OrderCount, decimal TotalSpentToman, int LicenseCount);

/// <summary>کدِ تخفیف — <c>/api/v1/admin/discounts</c>.</summary>
public sealed record V1Discount(
    Guid Id, string Code, string? Description, int PercentOff, int MaxRedemptions,
    int TimesRedeemed, DateTime? ExpiresAt, bool IsActive, DateTime CreatedAt);

/// <summary>درخواستِ دمو — <c>GET /api/v1/admin/demo-requests</c>.</summary>
public sealed record V1DemoRequest(
    Guid Id, string Name, string Email, string? Company, DemoRequestStatus Status, DateTime CreatedAt);

/// <summary>پیامِ فرمِ تماس — <c>GET /api/v1/admin/contact-messages</c>.</summary>
public sealed record V1ContactMessage(
    Guid Id, string Name, string Email, string? Company, string Topic, string Message,
    bool IsRead, DateTime CreatedAt);

/// <summary>بدنهٔ ۴۰۰ اعتبارسنجی: فیلد‌به‌فیلد.</summary>
public sealed record V1ValidationProblem(
    string? Code, string? Message, int Status, IDictionary<string, string[]>? Errors);
