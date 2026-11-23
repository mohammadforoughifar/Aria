using Northwind.Domain.Common;
using Northwind.Domain.Entities.Identity;

namespace Northwind.Domain.Entities.Support;

/// <summary>اعلانِ درون‌برنامه‌ای برایِ یک کاربر.</summary>
public class Notification : EntityBase
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Title { get; set; } = "";
    public string? Body { get; set; }

    /// <summary>یکی از <c>info</c>، <c>success</c>، <c>warning</c>، <c>danger</c>.</summary>
    public string Kind { get; set; } = "info";

    public bool IsRead { get; set; }

    /// <summary>مسیرِ مقصد هنگامِ کلیک.</summary>
    public string? Link { get; set; }
}
