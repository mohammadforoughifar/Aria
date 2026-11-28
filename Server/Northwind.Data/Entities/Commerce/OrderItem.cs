using Northwind.Data.Common;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Commerce;

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

    /* پیکربندیِ خریداری‌شده — برایِ فاکتور، گزارش و پشتیبانی. */
    public int Users { get; set; } = 1;
    public int ExtraCompanies { get; set; }
    public bool IncludeAndroid { get; set; }
    public bool IncludeIos { get; set; }
}
