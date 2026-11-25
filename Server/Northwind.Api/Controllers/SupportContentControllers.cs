using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Core.Services.Content;
using Northwind.Core.Services.Security;
using Northwind.Core.Services.Support;
using Northwind.Core.Models.Common;
using Northwind.Core.Models.Content;
using Northwind.Core.Models.Support;
using Northwind.Data.Enums;

namespace Northwind.Api.Controllers;

/* =============================== تیکت‌ها =============================== */

[ApiController]
[Route("api/v1/tickets")]
[Tags("پشتیبانی")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;
    private readonly ICurrentUser _current;

    public TicketsController(ITicketService tickets, ICurrentUser current)
    {
        _tickets = tickets;
        _current = current;
    }

    [HttpPost]
    [ProducesResponseType<TicketDetailViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketDetailViewModel>> Create([FromBody] NewTicketInput input, CancellationToken ct)
    {
        var ticket = await _tickets.CreateAsync(input, _current.RequireId(), _current.Email ?? "کاربر", ct);
        return Created($"/api/v1/tickets/{ticket.Id}", ticket);
    }

    [HttpGet]
    public Task<PagedViewModel<TicketViewModel>> Mine(
        [FromQuery] ListQuery query, [FromQuery] TicketStatus? status, CancellationToken ct)
        => _tickets.ListMineAsync(_current.RequireId(), query, status, ct);

    [HttpGet("{id:guid}")]
    public Task<TicketDetailViewModel> Get(Guid id, CancellationToken ct)
        => _tickets.GetAsync(id, _current.Id ?? Guid.Empty, _current.IsAdmin, ct);

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<TicketDetailViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public Task<TicketDetailViewModel> AddMessage(Guid id, [FromBody] AddTicketMessageInput input, CancellationToken ct)
        => _tickets.AddMessageAsync(id, input, _current.Id ?? Guid.Empty, _current.Email ?? "کاربر", _current.IsAdmin, ct);

    /* ------------------------------ مدیریت ------------------------------ */

    [HttpGet("/api/v1/admin/tickets")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<TicketViewModel>> All([FromQuery] ListQuery query, [FromQuery] TicketStatus? status, CancellationToken ct)
        => _tickets.AdminListAsync(query, status, ct);

    [HttpPatch("/api/v1/admin/tickets/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public Task<TicketDetailViewModel> SetStatus(Guid id, [FromBody] UpdateTicketStatusInput input, CancellationToken ct)
        => _tickets.UpdateStatusAsync(id, input, _current.Email ?? "پشتیبانی");
}

/* ============================== اعلان‌ها ============================== */

[ApiController]
[Route("api/v1/notifications")]
[Tags("اعلان‌ها")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUser _current;

    public NotificationsController(INotificationService notifications, ICurrentUser current)
    {
        _notifications = notifications;
        _current = current;
    }

    [HttpGet]
    public Task<PagedViewModel<NotificationViewModel>> List([FromQuery] ListQuery query, [FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        => _notifications.ListMineAsync(_current.RequireId(), query, unreadOnly, ct);

    [HttpGet("unread-count")]
    public Task<int> UnreadCount(CancellationToken ct)
        => _notifications.UnreadCountAsync(_current.RequireId(), ct);

    [HttpPost("{id:guid}/read")]
    public Task<NotificationViewModel> MarkRead(Guid id, CancellationToken ct)
        => _notifications.MarkReadAsync(id, _current.RequireId(), ct);

    [HttpPost("read-all")]
    [ProducesResponseType<object>(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> MarkAllRead(CancellationToken ct)
    {
        var count = await _notifications.MarkAllReadAsync(_current.RequireId(), ct);
        return Ok(new { markedRead = count });
    }
}

/* =========================== محتوا و تنظیمات =========================== */

[ApiController]
[Route("api/v1")]
[Tags("محتوا")]
public sealed class ContentController : ControllerBase
{
    private readonly IContentService _content;

    public ContentController(IContentService content) => _content = content;

    /* ------------------------------ مقالات ------------------------------ */

    [HttpGet("articles")]
    [AllowAnonymous]
    public Task<PagedViewModel<ArticleViewModel>> Articles([FromQuery] ListQuery query, CancellationToken ct)
        => _content.ArticlesAsync(publishedOnly: true, query, ct);

    [HttpGet("articles/{slug}")]
    [AllowAnonymous]
    [ProducesResponseType<ArticleViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status404NotFound)]
    public Task<ArticleViewModel> Article(string slug, CancellationToken ct)
        => _content.ArticleBySlugAsync(slug, ct);

    [HttpGet("admin/articles")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<ArticleViewModel>> AllArticles([FromQuery] ListQuery query, CancellationToken ct)
        => _content.ArticlesAsync(publishedOnly: false, query, ct);

    [HttpPost("admin/articles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ArticleViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleViewModel>> CreateArticle([FromBody] ArticleInput input, CancellationToken ct)
    {
        var created = await _content.CreateArticleAsync(input, ct);
        return Created($"/api/v1/articles/{created.Slug}", created);
    }

    [HttpPut("admin/articles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<ArticleViewModel> UpdateArticle(Guid id, [FromBody] ArticleInput input, CancellationToken ct)
        => _content.UpdateArticleAsync(id, input, ct);

    [HttpDelete("admin/articles/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteArticle(Guid id, CancellationToken ct)
    {
        await _content.DeleteArticleAsync(id, ct);
        return NoContent();
    }

    /* ------------------------------ نقلِ‌قول و پرسش ------------------------------ */

    [HttpGet("testimonials")]
    [AllowAnonymous]
    public Task<IReadOnlyList<TestimonialViewModel>> Testimonials(CancellationToken ct)
        => _content.TestimonialsAsync(ct);

    [HttpGet("faqs")]
    [AllowAnonymous]
    public Task<IReadOnlyList<FaqViewModel>> Faqs(CancellationToken ct)
        => _content.FaqsAsync(ct);

    /* ------------------------------ تنظیمات ------------------------------ */

    [HttpGet("settings")]
    [AllowAnonymous]
    public Task<SiteSettingsViewModel> Settings(CancellationToken ct)
        => _content.SettingsAsync(ct);

    [HttpPut("admin/settings")]
    [Authorize(Roles = "Admin")]
    public Task<SiteSettingsViewModel> UpdateSettings([FromBody] SiteSettingsInput input, CancellationToken ct)
        => _content.UpdateSettingsAsync(input, ct);
}

/* ======================== تماس و درخواستِ دمو ======================== */

[ApiController]
[Route("api/v1")]
[Tags("ارتباط")]
public sealed class ContactController : ControllerBase
{
    private readonly IContactService _contact;

    public ContactController(IContactService contact) => _contact = contact;

    [HttpPost("contact")]
    [AllowAnonymous]
    [ProducesResponseType<ContactMessageViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ContactMessageViewModel>> Send([FromBody] ContactInput input, CancellationToken ct)
    {
        var message = await _contact.SendAsync(input, ct);
        return Created($"/api/v1/admin/contact-messages/{message.Id}", message);
    }

    [HttpGet("admin/contact-messages")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<ContactMessageViewModel>> Messages([FromQuery] ListQuery query, [FromQuery] bool? unreadOnly, CancellationToken ct)
        => _contact.ListAsync(query, unreadOnly, ct);

    [HttpPost("admin/contact-messages/{id:guid}/read")]
    [Authorize(Roles = "Admin")]
    public Task<ContactMessageViewModel> MarkRead(Guid id, CancellationToken ct)
        => _contact.MarkReadAsync(id, ct);

    [HttpPost("demo-requests")]
    [AllowAnonymous]
    [ProducesResponseType<DemoRequestViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DemoRequestViewModel>> RequestDemo([FromBody] DemoRequestInput input, CancellationToken ct)
    {
        var request = await _contact.RequestDemoAsync(input, ct);
        return Created($"/api/v1/admin/demo-requests/{request.Id}", request);
    }

    [HttpGet("admin/demo-requests")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<DemoRequestViewModel>> DemoRequests([FromQuery] ListQuery query, [FromQuery] DemoRequestStatus? status, CancellationToken ct)
        => _contact.DemoRequestsAsync(query, status, ct);

    [HttpPatch("admin/demo-requests/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public Task<DemoRequestViewModel> SetDemoStatus(Guid id, [FromBody] DemoStatusInput input, CancellationToken ct)
        => _contact.SetDemoStatusAsync(id, input.Status, ct);
}
