using Northwind.Data.Common;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Catalog;

/// <summary>طرحِ قیمتِ یک محصول (تک‌کاربره، چندکاربره، سازمانی).</summary>
public class Plan : EntityBase
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string Name { get; set; } = "";
    public PlanTier Tier { get; set; } = PlanTier.SingleUser;
    public BillingType BillingType { get; set; } = BillingType.OneTime;

    public decimal PriceToman { get; set; }
    public int Seats { get; set; } = 1;
    public string? Description { get; set; }
    public bool IsPopular { get; set; }

    /// <summary>مدتِ اعتبارِ لایسنس به ماه؛ صفر یعنی همیشگی.</summary>
    public int LicenseMonths { get; set; } = 12;

    public List<string> Features { get; set; } = new();
    public int SortOrder { get; set; }
}
