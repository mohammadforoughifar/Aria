using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Catalog;

/// <summary>نسخهٔ منتشرشدهٔ یک محصول (تاریخچهٔ انتشار).</summary>
public class ProductVersion : EntityBase
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public string Version { get; set; } = "1.0.0";
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = "";
    public string? InstallerUrl { get; set; }
    public int SizeMb { get; set; }
    public string Platform { get; set; } = "Windows";
}
