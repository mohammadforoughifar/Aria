using Northwind.Data.Common;

namespace Northwind.Data.Entities.Commerce;

/// <summary>کدِ تخفیفِ درصدی با سقفِ تعدادِ استفاده.</summary>
public class DiscountCode : EntityBase
{
    public string Code { get; set; } = "";
    public string? Description { get; set; }

    /// <summary>درصدِ تخفیف از ۰ تا ۱۰۰.</summary>
    public int PercentOff { get; set; }

    /// <summary>سقفِ تعدادِ استفاده؛ صفر یعنی بی‌نهایت.</summary>
    public int MaxRedemptions { get; set; } = 100;
    public int TimesRedeemed { get; set; }

    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
