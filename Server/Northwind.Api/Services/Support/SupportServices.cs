using Microsoft.EntityFrameworkCore;
using Northwind.Api.Mapping;
using Northwind.Api.Services.Common;
using Northwind.Api.ViewModels.Common;
using Northwind.Api.ViewModels.Support;
using Northwind.Domain.Entities.Support;
using Northwind.Domain.Enums;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Services.Support;

public interface ITicketService
{
    Task<TicketDetailViewModel> CreateAsync(NewTicketInput input, Guid userId, string authorName, CancellationToken ct = default);
    Task<PagedViewModel<TicketViewModel>> ListMineAsync(Guid userId, ListQuery query, TicketStatus? status = null, CancellationToken ct = default);
    Task<TicketDetailViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default);
    Task<TicketDetailViewModel> AddMessageAsync(Guid id, AddTicketMessageInput input, Guid userId, string authorName, bool isAdmin, CancellationToken ct = default);
    Task<PagedViewModel<TicketViewModel>> AdminListAsync(ListQuery query, TicketStatus? status, CancellationToken ct = default);
    Task<TicketDetailViewModel> UpdateStatusAsync(Guid id, UpdateTicketStatusInput input, string staffName, CancellationToken ct = default);
}

public sealed class TicketService : ITicketService
{
    private readonly NorthwindDbContext _db;

    public TicketService(NorthwindDbContext db) => _db = db;

    public async Task<TicketDetailViewModel> CreateAsync(NewTicketInput input, Guid userId, string authorName, CancellationToken ct = default)
    {
        if (input.ProductId is not null && !await _db.Products.AnyAsync(p => p.Id == input.ProductId, ct))
            throw new BadRequestException("محصولِ انتخاب‌شده وجود ندارد.", "product_not_found");

        var now = DateTime.UtcNow;

        var ticket = new Ticket
        {
            Reference = await NextReferenceAsync(ct),
            UserId = userId,
            ProductId = input.ProductId,
            Subject = input.Subject.Trim(),
            Category = string.IsNullOrWhiteSpace(input.Category) ? "عمومی" : input.Category,
            Priority = input.Priority,
            Status = TicketStatus.Open,
            Environment = input.Environment,
            CreatedAt = now,
            LastActivityAt = now
        };

        ticket.Messages.Add(new TicketMessage
        {
            AuthorName = authorName,
            IsStaff = false,
            Body = input.Message.Trim(),
            CreatedAt = now
        });

        _db.Tickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        return await GetAsync(ticket.Id, userId, true, ct);
    }

    public async Task<PagedViewModel<TicketViewModel>> ListMineAsync(Guid userId, ListQuery query,
        TicketStatus? status = null, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking()
            .Include(t => t.User).Include(t => t.Product).Include(t => t.Messages)
            .Where(t => t.UserId == userId);

        if (status is not null) q = q.Where(t => t.Status == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(t => EF.Functions.Like(t.Subject, $"%{s}%")
                          || EF.Functions.Like(t.Reference, $"%{s}%"));
        }

        return await q.OrderByDescending(t => t.LastActivityAt)
                      .ToPagedAsync(query, t => t.ToViewModel(), ct);
    }

    public async Task<TicketDetailViewModel> GetAsync(Guid id, Guid userId, bool isAdmin, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.AsNoTracking()
            .Include(t => t.User).Include(t => t.Product).Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("تیکت", id);

        if (!isAdmin && ticket.UserId != userId)
            throw new ForbiddenException("این تیکت به شما تعلق ندارد.");

        return ticket.ToDetail();
    }

    public async Task<TicketDetailViewModel> AddMessageAsync(Guid id, AddTicketMessageInput input, Guid userId, string authorName, bool isAdmin, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.Include(t => t.Messages).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("تیکت", id);

        if (!isAdmin && ticket.UserId != userId)
            throw new ForbiddenException("این تیکت به شما تعلق ندارد.");

        if (ticket.Status is TicketStatus.Closed)
            throw new ConflictException("این تیکت بسته شده است؛ برایِ پیامِ تازه تیکتِ جدید بزنید.", "ticket_closed");

        var now = DateTime.UtcNow;

        ticket.Messages.Add(new TicketMessage
        {
            AuthorName = isAdmin ? (string.IsNullOrWhiteSpace(authorName) ? "پشتیبانی" : authorName) : authorName,
            IsStaff = isAdmin,
            Body = input.Body.Trim(),
            CreatedAt = now
        });

        ticket.LastActivityAt = now;

        // پیامِ پشتیبانی وضعیت را جلو می‌برد؛ پیامِ کاربر آن را به «در انتظارِ پشتیبانی» برمی‌گرداند.
        ticket.Status = isAdmin ? TicketStatus.WaitingCustomer : TicketStatus.InProgress;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, userId, true, ct);
    }

    public async Task<PagedViewModel<TicketViewModel>> AdminListAsync(ListQuery query, TicketStatus? status, CancellationToken ct = default)
    {
        var q = _db.Tickets.AsNoTracking()
            .Include(t => t.User).Include(t => t.Product).Include(t => t.Messages)
            .AsQueryable();

        if (status is not null) q = q.Where(t => t.Status == status);

        if (query.Search is not null)
        {
            var s = query.Search;
            q = q.Where(t => EF.Functions.Like(t.Subject, $"%{s}%")
                          || EF.Functions.Like(t.Reference, $"%{s}%")
                          || EF.Functions.Like(t.User!.Email, $"%{s}%"));
        }

        q = q.OrderByDescending(t => t.LastActivityAt);
        return await q.ToPagedAsync(query, t => t.ToViewModel(), ct);
    }

    public async Task<TicketDetailViewModel> UpdateStatusAsync(Guid id, UpdateTicketStatusInput input, string staffName, CancellationToken ct = default)
    {
        var ticket = await _db.Tickets.Include(t => t.Messages).FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException("تیکت", id);

        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(input.Reply))
        {
            ticket.Messages.Add(new TicketMessage
            {
                AuthorName = string.IsNullOrWhiteSpace(staffName) ? "پشتیبانی" : staffName,
                IsStaff = true,
                Body = input.Reply.Trim(),
                CreatedAt = now
            });
        }

        ticket.Status = input.Status;
        ticket.LastActivityAt = now;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(id, ticket.UserId, true, ct);
    }

    private async Task<string> NextReferenceAsync(CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var count = await _db.Tickets.CountAsync(ct) + 1;

        for (var i = 0; i < 1000; i++)
        {
            var candidate = $"TK-{year}-{count + i:D4}";
            if (!await _db.Tickets.AnyAsync(t => t.Reference == candidate, ct)) return candidate;
        }

        return $"TK-{year}-{Guid.NewGuid():N}"[..16];
    }
}

/* ============================== اعلان‌ها ============================== */

public interface INotificationService
{
    Task<PagedViewModel<NotificationViewModel>> ListMineAsync(Guid userId, ListQuery query, bool unreadOnly, CancellationToken ct = default);
    Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationViewModel> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly NorthwindDbContext _db;

    public NotificationService(NorthwindDbContext db) => _db = db;

    public async Task<PagedViewModel<NotificationViewModel>> ListMineAsync(Guid userId, ListQuery query, bool unreadOnly, CancellationToken ct = default)
    {
        var q = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        if (unreadOnly) q = q.Where(n => !n.IsRead);

        q = q.OrderByDescending(n => n.CreatedAt);
        return await q.ToPagedAsync(query, n => n.ToViewModel(), ct);
    }

    public Task<int> UnreadCountAsync(Guid userId, CancellationToken ct = default)
        => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task<NotificationViewModel> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new NotFoundException("اعلان", id);

        if (notification.UserId != userId)
            throw new ForbiddenException("این اعلان برایِ شما نیست.");

        notification.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return notification.ToViewModel();
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var unread = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync(ct);
        if (unread.Count == 0) return 0;

        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return unread.Count;
    }
}
