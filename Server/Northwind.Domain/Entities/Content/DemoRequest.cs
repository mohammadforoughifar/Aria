using Northwind.Domain.Common;
using Northwind.Domain.Entities.Catalog;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Content;

/// <summary>درخواستِ نمایشِ دمویِ نرم‌افزار از سویِ سازمان.</summary>
public class DemoRequest : EntityBase
{
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Company { get; set; }
    public string? CompanySize { get; set; }
    public string? Phone { get; set; }
    public string? Message { get; set; }

    public DemoRequestStatus Status { get; set; } = DemoRequestStatus.New;
}
