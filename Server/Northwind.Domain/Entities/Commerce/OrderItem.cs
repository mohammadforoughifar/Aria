using Northwind.Domain.Common;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Commerce;

/// <summary>
/// ردیفِ سفارش. نامِ محصول و طرح عمداً تکرار (denormalized) شده‌اند تا فاکتورِ قدیمی
/// با تغییرِ نامِ محصول در آینده عوض نشود — رفتارِ استانداردِ اسنادِ مالی.
/// </summary>
public class OrderItem : EntityBase
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public Guid PlanId { get; set; }

    public string ProductName { get; set; } = "";
    public string PlanName { get; set; } = "";

    public BillingType BillingType { get; set; }
    public decimal UnitPriceToman { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal LineTotalToman { get; set; }
}
