using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Content;

/// <summary>نقلِ‌قولِ مشتری برایِ نمایش در صفحهٔ نخست.</summary>
public class Testimonial : EntityBase
{
    public string AuthorName { get; set; } = "";
    public string AuthorRole { get; set; } = "";
    public string Company { get; set; } = "";
    public string Quote { get; set; } = "";
    public int Rating { get; set; } = 5;
}
