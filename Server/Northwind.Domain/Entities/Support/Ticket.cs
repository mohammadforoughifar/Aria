using Northwind.Domain.Common;
using Northwind.Domain.Entities.Catalog;
using Northwind.Domain.Entities.Identity;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Support;

/// <summary>تیکتِ پشتیبانی با رشتهٔ پیام‌ها.</summary>
public class Ticket : EntityBase
{
    /// <summary>شمارهٔ پیگیریِ خوانا، مثلِ <c>TK-1403-012</c>.</summary>
    public string Reference { get; set; } = "";

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>محصولِ مرتبط، اگر تیکت دربارهٔ نرم‌افزارِ خاصی باشد.</summary>
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string Subject { get; set; } = "";
    public string Category { get; set; } = "عمومی";
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    /// <summary>محیطِ کاربر (نسخهٔ ویندوز، نسخهٔ نرم‌افزار) برایِ تسریعِ پاسخ.</summary>
    public string? Environment { get; set; }

    /// <summary>
    /// زمانِ آخرین پیام در رشتهٔ گفتگو. عمداً از <see cref="EntityBase.UpdatedAt"/> جدا است:
    /// آن یکی زمانِ تغییرِ رکورد است، این یکی زمانِ تغییرِ گفتگو.
    /// </summary>
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
}
