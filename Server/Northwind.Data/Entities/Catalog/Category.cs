using Northwind.Data.Common;

namespace Northwind.Data.Entities.Catalog;

/// <summary>دستهٔ محصولات.</summary>
public class Category : EntityBase
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Icon { get; set; } = "package";
    public int SortOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
