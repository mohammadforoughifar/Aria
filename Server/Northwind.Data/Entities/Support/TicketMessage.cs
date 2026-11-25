using Northwind.Data.Common;

namespace Northwind.Data.Entities.Support;

/// <summary>یک پیام در رشتهٔ گفتگویِ تیکت.</summary>
public class TicketMessage : EntityBase
{
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }

    public string AuthorName { get; set; } = "";

    /// <summary>درست وقتی که نویسنده از تیمِ پشتیبانی است.</summary>
    public bool IsStaff { get; set; }

    public string Body { get; set; } = "";
}
