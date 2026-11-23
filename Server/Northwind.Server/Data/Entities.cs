// موجودیت‌های پایگاه‌داده (EF Core / SQLite).
// این کلاس‌ها آینهٔ قراردادهای Shared هستند اما به‌شکلِ «قابلِ نگاشت» طراحی شده‌اند:
// لیست‌های رشته‌ای به‌صورت JSON در یک ستون ذخیره می‌شوند و enumها به int نگاشت می‌شوند.

using Northwind.Shared.Contracts;

namespace Northwind.Server.Data;

/* ------------------------------- Identity -------------------------------- */

public class User
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string FullName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public Role Role { get; set; } = Role.Customer;
    public AccountType AccountType { get; set; } = AccountType.Individual;
    public bool Locked { get; set; }
    public string? Company { get; set; }
    public string? Country { get; set; }
    public string? EconomicCode { get; set; }
    public string? NationalId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

/* ------------------------------- Catalog --------------------------------- */

public class Category
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Icon { get; set; } = "package";
    public int SortOrder { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public ProductType Type { get; set; } = ProductType.Module;
    public Audience Audience { get; set; } = Audience.Both;
    public string Platform { get; set; } = "Windows";
    public string Version { get; set; } = "1.0.0";
    public int BasePriceToman { get; set; }
    public string AccentColor { get; set; } = "#1E3A8A";
    public string? HeroImage { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int TrialDays { get; set; } = 14;
    public int Popularity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>ویژگی‌ها، نیازمندی‌ها و تصاویرِ گالری — هر کدام یک لیستِ رشته‌ایِ JSON.</summary>
    public string FeaturesJson { get; set; } = "[]";
    public string RequirementsJson { get; set; } = "[]";
    public string GalleryJson { get; set; } = "[]";
    public string? DemoVideoUrl { get; set; }

    public string CategoryId { get; set; } = "";
    public Category? Category { get; set; }

    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Plan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }
    public string Name { get; set; } = "";
    public PlanTier Tier { get; set; } = PlanTier.SingleUser;
    public BillingType BillingType { get; set; } = BillingType.OneTime;
    public int PriceToman { get; set; }
    public int Seats { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsPopular { get; set; }
    public int LicenseMonths { get; set; } = 12;
    public string FeaturesJson { get; set; } = "[]";
    public int SortOrder { get; set; }
}

public class ProductVersion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }
    public string Version { get; set; } = "1.0.0";
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = "";
    public string? InstallerUrl { get; set; }
    public int SizeMb { get; set; }
    public string Platform { get; set; } = "Windows";
}

public class Review
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }
    public string? UserId { get; set; }
    public string AuthorName { get; set; } = "";
    public string? AuthorCompany { get; set; }
    public int Rating { get; set; } = 5;          // ۱ تا ۵
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* -------------------------------- Cart ----------------------------------- */

public class CartItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string SessionKey { get; set; } = "";
    public string? UserId { get; set; }
    public string PlanId { get; set; } = "";
    public Plan? Plan { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* ------------------------------- Commerce --------------------------------- */

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OrderNumber { get; set; } = "";
    public string UserId { get; set; } = "";
    public User? User { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int SubtotalToman { get; set; }
    public int DiscountToman { get; set; }
    public int TaxToman { get; set; }
    public int TotalToman { get; set; }
    public string Currency { get; set; } = "IRT";
    public string? DiscountCode { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    // صورتحساب — برای فاکتورِ رسمی
    public AccountType AccountType { get; set; } = AccountType.Individual;
    public string? BillingName { get; set; }
    public string? BillingCompany { get; set; }
    public string BillingEmail { get; set; } = "";
    public string? BillingCountry { get; set; }
    public string? EconomicCode { get; set; }
    public string? NationalId { get; set; }
    public string? Notes { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
    public Invoice? Invoice { get; set; }
}

public class OrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OrderId { get; set; } = "";
    public Order? Order { get; set; }
    public string ProductId { get; set; } = "";
    public string PlanId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string PlanName { get; set; } = "";
    public BillingType BillingType { get; set; }
    public int UnitPriceToman { get; set; }
    public int Quantity { get; set; } = 1;
    public int LineTotalToman { get; set; }
}

public class Payment
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string OrderId { get; set; } = "";
    public Order? Order { get; set; }
    public string Provider { get; set; } = "mock";
    public string Method { get; set; } = "card";
    public int AmountToman { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class Invoice
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string InvoiceNumber { get; set; } = "";
    public string OrderId { get; set; } = "";
    public Order? Order { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public int TotalToman { get; set; }
    public int TaxToman { get; set; }
}

public class License
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string LicenseKey { get; set; } = "";
    public string UserId { get; set; } = "";
    public User? User { get; set; }
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }
    public string? PlanId { get; set; }
    public Plan? Plan { get; set; }
    public int Seats { get; set; } = 1;
    public int ActivationsUsed { get; set; }
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public class Download
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = "";
    public Product? Product { get; set; }
    public string? UserId { get; set; }
    public string? VersionId { get; set; }
    public string Version { get; set; } = "";
    public string Kind { get; set; } = "installer";
    public string FileName { get; set; } = "";
    public int SizeMb { get; set; }
    public bool RequiresLicense { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}

public class DiscountCode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public int PercentOff { get; set; }
    public int MaxRedemptions { get; set; } = 100;
    public int TimesRedeemed { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* -------------------------------- Support --------------------------------- */

public class Ticket
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Reference { get; set; } = "";
    public string UserId { get; set; } = "";
    public User? User { get; set; }
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "عمومی";
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public string? Environment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
}

public class TicketMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TicketId { get; set; } = "";
    public Ticket? Ticket { get; set; }
    public string AuthorName { get; set; } = "";
    public bool IsStaff { get; set; }
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/* -------------------------------- Content --------------------------------- */

public class Article
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Category { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public int ReadMinutes { get; set; } = 4;
}

public class Testimonial
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AuthorName { get; set; } = "";
    public string AuthorRole { get; set; } = "";
    public string Company { get; set; } = "";
    public string Quote { get; set; } = "";
    public int Rating { get; set; } = 5;
}

public class Faq
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
}

public class SiteSettings
{
    public int Id { get; set; } = 1;                 // رکوردِ تکی (singleton)
    public string SiteName { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string SalesEmail { get; set; } = "";
    public string SupportPhone { get; set; } = "";
    public string HeroHeadline { get; set; } = "";
    public string HeroSubheading { get; set; } = "";
    public int TaxRateBps { get; set; } = 900;       // ۹٪ ارزش افزوده
    public int TrialDays { get; set; } = 14;
    public bool MaintenanceMode { get; set; }
    public string? Announcement { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Notification
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string UserId { get; set; } = "";
    public User? User { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public string Kind { get; set; } = "info";       // info|success|warning|danger
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DemoRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string? ProductId { get; set; }
    public Product? Product { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Company { get; set; }
    public string? CompanySize { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }
    public DemoRequestStatus Status { get; set; } = DemoRequestStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ContactMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Company { get; set; }
    public string Topic { get; set; } = "عمومی";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
