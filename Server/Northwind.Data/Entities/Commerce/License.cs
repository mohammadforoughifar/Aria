using Northwind.Data.Common;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Entities.Identity;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Commerce;

/// <summary>لایسنسِ صادرشده برایِ یک کاربر رویِ یک محصول.</summary>
public class License : EntityBase
{
    /// <summary>کلیدِ لایسنس، یکتا — همان چیزی که کاربر در برنامه وارد می‌کند.</summary>
    public string LicenseKey { get; set; } = "";

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? PlanId { get; set; }
    public Plan? Plan { get; set; }

    public int Seats { get; set; } = 1;
    public int ActivationsUsed { get; set; }

    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    /// <summary>تاریخِ انقضا؛ خالی یعنی لایسنسِ همیشگی.</summary>
    public DateTime? ExpiresAt { get; set; }
}
