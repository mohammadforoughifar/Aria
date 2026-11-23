using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Content;

/// <summary>مقالهٔ وبلاگ.</summary>
public class Article : EntityBase
{
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Excerpt { get; set; } = "";
    public string Body { get; set; } = "";
    public string? Category { get; set; }

    public bool IsPublished { get; set; } = true;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public int ReadMinutes { get; set; } = 4;
}
