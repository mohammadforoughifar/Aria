using Northwind.Domain.Common;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Commerce;

/// <summary>تلاشِ پرداخت برایِ یک سفارش (رابطهٔ یک‌به‌یک).</summary>
public class Payment : EntityBase
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    /// <summary>نامِ درگاه؛ در نسخهٔ نمایشی <c>mock</c>.</summary>
    public string Provider { get; set; } = "mock";
    public string Method { get; set; } = "card";

    public int AmountToman { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }
}
