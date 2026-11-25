using Northwind.Data.Common;
using Northwind.Data.Entities.Identity;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Commerce;

/// <summary>سفارش — سرآمدِ اقلام، پرداخت، فاکتور و لایسنس‌هایِ حاصل از آن.</summary>
public class Order : EntityBase
{
    /// <summary>شمارهٔ خوانا برایِ انسان، مثلِ <c>NW-1403-0007</c>.</summary>
    public string OrderNumber { get; set; } = "";

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    // مبالغ به تومان
    public decimal SubtotalToman { get; set; }
    public decimal DiscountToman { get; set; }
    public decimal TaxToman { get; set; }
    public decimal TotalToman { get; set; }

    /// <summary>کدِ ارز؛ پیش‌فرض تومانِ ایران.</summary>
    public string Currency { get; set; } = "IRT";

    public string? DiscountCode { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    /* ---------- اطلاعاتِ صورتحساب (برایِ فاکتورِ رسمی) ---------- */
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
