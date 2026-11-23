using System.ComponentModel.DataAnnotations;
using Northwind.Domain.Enums;

namespace Northwind.Api.ViewModels.Support;

/* =============================== تیکت =============================== */

public sealed record TicketMessageViewModel(
    Guid Id, Guid TicketId, string AuthorName, bool IsStaff, string Body, DateTime CreatedAt);

public sealed record TicketViewModel(
    Guid Id, string Reference, Guid UserId, string? UserName, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt, int MessageCount);

public sealed record TicketDetailViewModel(
    Guid Id, string Reference, Guid UserId, string? UserName, Guid? ProductId, string? ProductName,
    string Subject, string Category, TicketPriority Priority, TicketStatus Status,
    string? Environment, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<TicketMessageViewModel> Messages);

/// <summary>ساختِ تیکتِ جدید — <c>POST /api/tickets</c>.</summary>
public sealed class NewTicketInput
{
    public Guid? ProductId { get; set; }

    [Required(ErrorMessage = "موضوعِ تیکت لازم است.")]
    [MaxLength(240)]
    public string Subject { get; set; } = "";

    [MaxLength(80)] public string Category { get; set; } = "عمومی";
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    [MaxLength(400)] public string? Environment { get; set; }

    [Required(ErrorMessage = "متنِ پیام لازم است.")]
    [MaxLength(8000)]
    public string Message { get; set; } = "";
}

public sealed class AddTicketMessageInput
{
    [Required(ErrorMessage = "متنِ پیام لازم است.")]
    [MaxLength(8000)]
    public string Body { get; set; } = "";
}

public sealed class UpdateTicketStatusInput
{
    public TicketStatus Status { get; set; } = TicketStatus.InProgress;
    [MaxLength(8000)] public string? Reply { get; set; }
}

/* ============================== اعلان ============================== */

public sealed record NotificationViewModel(
    Guid Id, Guid UserId, string Title, string? Body,
    string Kind, bool IsRead, DateTime CreatedAt, string? Link);

/* ============================ تماس با ما ============================ */

public sealed record ContactMessageViewModel(
    Guid Id, string Name, string Email, string? Company, string Topic,
    string Message, bool IsRead, DateTime CreatedAt);

public sealed class ContactInput
{
    [Required(ErrorMessage = "نام لازم است.")]
    [MaxLength(160)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "ایمیل لازم است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(80)] public string Topic { get; set; } = "عمومی";

    [Required(ErrorMessage = "متنِ پیام لازم است.")]
    [MaxLength(4000)]
    public string Message { get; set; } = "";
}

/* =========================== درخواستِ دمو =========================== */

public sealed record DemoRequestViewModel(
    Guid Id, Guid? ProductId, string? ProductName, string Name, string Email,
    string? Company, string? CompanySize, string? Phone, string? Message,
    DemoRequestStatus Status, DateTime CreatedAt);

public sealed class DemoRequestInput
{
    public Guid? ProductId { get; set; }

    [Required(ErrorMessage = "نام لازم است.")]
    [MaxLength(160)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "ایمیل لازم است.")]
    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست.")]
    [MaxLength(256)]
    public string Email { get; set; } = "";

    [MaxLength(200)] public string? Company { get; set; }
    [MaxLength(40)] public string? CompanySize { get; set; }
    [MaxLength(32)] public string? Phone { get; set; }
    [MaxLength(2000)] public string? Message { get; set; }
}

public sealed class DemoStatusInput
{
    public DemoRequestStatus Status { get; set; } = DemoRequestStatus.Contacted;
}
