using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Content;

/// <summary>پرسشِ پرتکرار.</summary>
public class Faq : EntityBase
{
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public int SortOrder { get; set; }
}
