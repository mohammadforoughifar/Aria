using Northwind.Data.Common;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Enums;

namespace Northwind.Data.Entities.Content;

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
