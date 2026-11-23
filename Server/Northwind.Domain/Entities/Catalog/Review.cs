using Northwind.Domain.Common;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Catalog;

/// <summary>دیدگاهِ کاربر دربارهٔ یک محصول — پیش از نمایش باید تأیید شود.</summary>
public class Review : EntityBase
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>اگر دیدگاه از سویِ کاربرِ ثبت‌شده باشد پر می‌شود.</summary>
    public Guid? UserId { get; set; }

    public string AuthorName { get; set; } = "";
    public string? AuthorCompany { get; set; }

    /// <summary>امتیاز از ۱ تا ۵.</summary>
    public int Rating { get; set; } = 5;
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
}
