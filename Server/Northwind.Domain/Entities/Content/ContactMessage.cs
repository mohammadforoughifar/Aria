using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Content;

/// <summary>پیامِ فرمِ «تماس با ما».</summary>
public class ContactMessage : EntityBase
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Company { get; set; }
    public string Topic { get; set; } = "عمومی";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }
}
