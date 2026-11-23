// حالتِ درون‌حافظه‌ایِ فروشگاه (جانشینِ موقتِ پایگاه داده).
// داده‌ها در هر بارگذاریِ برنامه از MockData ساخته می‌شوند.

using Northwind.Client.Models;

using Northwind.Shared.Contracts;

namespace Northwind.Client.Services;

public sealed class CartLine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string SessionKey { get; set; } = "";
    public string PlanId { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = System.DateTime.UtcNow;
}

public sealed class StoreState
{
    public SiteSettingsDto Settings { get; set; } = default!;

    public List<UserDto> Users { get; } = new();
    public List<NotificationDto> Notifications { get; } = new();
    public Dictionary<string, string> Passwords { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<CategoryDto> Categories { get; } = new();
    public List<ProductDetailDto> Products { get; } = new();
    public List<ReviewDto> Reviews { get; } = new();

    public List<CartLine> CartLines { get; } = new();
    public List<OrderDto> Orders { get; } = new();
    public List<InvoiceDto> Invoices { get; } = new();
    public List<LicenseDto> Licenses { get; } = new();
    public List<DownloadDto> Downloads { get; } = new();

    public List<TicketDetailDto> Tickets { get; } = new();
    public List<DiscountCodeDto> Discounts { get; } = new();
    public List<TestimonialDto> Testimonials { get; } = new();
    public List<FaqDto> Faqs { get; } = new();

    public string? CurrentUserId { get; set; }
    public int OrderSeq { get; set; }
    public int InvoiceSeq { get; set; }

    public UserDto? CurrentUser =>
        CurrentUserId is null ? null : Users.FirstOrDefault(u => u.Id == CurrentUserId);

    public string NextOrderNumber() => $"NW-{System.DateTime.UtcNow.Year}-{(++OrderSeq):D4}";
    public string NextInvoiceNumber() => $"INV-{System.DateTime.UtcNow.Year}-{(++InvoiceSeq):D4}";

    public PlanDto? FindPlan(string planId) =>
        Products.SelectMany(p => p.Plans).FirstOrDefault(pl => pl.Id == planId);

    public ProductDetailDto? ProductOfPlan(string planId) =>
        Products.FirstOrDefault(p => p.Plans.Any(pl => pl.Id == planId));
}
