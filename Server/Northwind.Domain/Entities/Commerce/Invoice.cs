using Northwind.Domain.Common;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Commerce;

/// <summary>فاکتورِ رسمیِ یک سفارش (رابطهٔ یک‌به‌یک).</summary>
public class Invoice : EntityBase
{
    public string InvoiceNumber { get; set; } = "";

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }

    public decimal TotalToman { get; set; }
    public decimal TaxToman { get; set; }
}
