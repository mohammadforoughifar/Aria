using Northwind.Domain.Common;
using Northwind.Domain.Entities.Catalog;

namespace Northwind.Domain.Entities.Commerce;

/// <summary>تاریخچهٔ دریافتِ فایل توسطِ کاربر (کاربرِ مهمان هم می‌تواند داشته باشد).</summary>
public class Download : EntityBase
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? UserId { get; set; }
    public Guid? VersionId { get; set; }

    public string Version { get; set; } = "";

    /// <summary>نوعِ فایل: <c>installer</c> یا <c>manual</c> یا <c>sample</c>.</summary>
    public string Kind { get; set; } = "installer";

    public string FileName { get; set; } = "";
    public int SizeMb { get; set; }
    public bool RequiresLicense { get; set; }
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
}
