using Northwind.Data.Common;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Entities.Support;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Identity;

/// <summary>کاربرِ سیستم — هم مشتری، هم مدیر.</summary>
public class User : EntityBase
{
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string FullName { get; set; } = "";

    /// <summary>هشِ رمز (PBKDF2). هرگز رمزِ خام ذخیره نمی‌شود.</summary>
    public string PasswordHash { get; set; } = "";

    public Role Role { get; set; } = Role.Customer;
    public AccountType AccountType { get; set; } = AccountType.Individual;

    /// <summary>حسابِ قفل‌شده نمی‌تواند وارد شود.</summary>
    public bool Locked { get; set; }

    public string? Company { get; set; }
    public string? Country { get; set; }
    public string? EconomicCode { get; set; }
    public string? NationalId { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
