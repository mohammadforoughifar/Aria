using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Api.Services.Commerce;
using Northwind.Api.Services.Security;
using Northwind.Api.ViewModels.Commerce;
using Northwind.Api.ViewModels.Common;
using Northwind.Domain.Enums;

namespace Northwind.Api.Controllers;

/* ================================ سبد ================================ */

[ApiController]
[Route("api/v1/cart")]
[Tags("سبد خرید")]
public sealed class CartController : ControllerBase
{
    private readonly ICartService _cart;
    private readonly ICurrentUser _current;

    public CartController(ICartService cart, ICurrentUser current)
    {
        _cart = cart;
        _current = current;
    }

    /// <summary>سبدِ کاربرِ واردشده یا سبدِ مهمان با <c>sessionKey</c>.</summary>
    [HttpGet]
    [AllowAnonymous]
    public Task<CartViewModel> Get([FromQuery] string? sessionKey, [FromQuery] string? discountCode, CancellationToken ct)
        => _cart.GetAsync(sessionKey ?? "guest", _current.Id, discountCode, ct);

    [HttpPost("items")]
    [AllowAnonymous]
    [ProducesResponseType<CartViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status404NotFound)]
    public Task<CartViewModel> Add([FromBody] AddToCartInput input, CancellationToken ct)
        => _cart.AddAsync(input, _current.Id, ct);

    [HttpPut("items/{itemId:guid}")]
    [AllowAnonymous]
    public Task<CartViewModel> UpdateQuantity(Guid itemId, [FromBody] UpdateCartItemInput input,
        [FromQuery] string? sessionKey, [FromQuery] string? discountCode, CancellationToken ct)
        => _cart.UpdateQuantityAsync(sessionKey ?? "guest", _current.Id, itemId, input.Quantity, discountCode, ct);

    [HttpDelete("items/{itemId:guid}")]
    [AllowAnonymous]
    public async Task<CartViewModel> Remove(Guid itemId, [FromQuery] string? sessionKey, CancellationToken ct)
    {
        var cart = await _cart.RemoveAsync(sessionKey ?? "guest", _current.Id, itemId, ct);
        return cart;
    }

    [HttpDelete]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Clear([FromQuery] string? sessionKey, CancellationToken ct)
    {
        await _cart.ClearAsync(sessionKey ?? "guest", _current.Id, ct);
        return NoContent();
    }

    /// <summary>پیش‌نمایشِ کدِ تخفیف رویِ سبد، بدونِ تغییرِ سبد.</summary>
    [HttpPost("discount")]
    [AllowAnonymous]
    [ProducesResponseType<CartViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public Task<CartViewModel> ApplyDiscount([FromBody] ApplyDiscountInput input, CancellationToken ct)
        => _cart.PreviewDiscountAsync(input.SessionKey ?? "guest", _current.Id, input.Code, ct);
}

/* ============================== سفارش‌ها ============================== */

[ApiController]
[Route("api/v1")]
[Tags("سفارش‌ها")]
[Authorize]
public sealed class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly ICurrentUser _current;

    public OrdersController(IOrderService orders, ICurrentUser current)
    {
        _orders = orders;
        _current = current;
    }

    /// <summary>
    /// تسویه‌حساب — سبد به سفارش، پرداخت، فاکتور و لایسنس تبدیل می‌شود.
    /// برایِ کاربرِ واردشده با توکن کار می‌کند و برایِ مهمان با
    /// <c>sessionKey</c>؛ در حالتِ مهمان، سفارش به حسابی با ایمیلِ صورتحساب
    /// وصل می‌شود (ساخته می‌شود اگر نباشد).
    /// </summary>
    [HttpPost("checkout")]
    [AllowAnonymous]
    [ProducesResponseType<CheckoutResultViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CheckoutResultViewModel>> Checkout([FromBody] CheckoutInput input, CancellationToken ct)
    {
        var result = await _orders.CheckoutAsync(input, _current.Id, ct);
        return Created($"/api/v1/orders/{result.Order.Id}", result);
    }

    [HttpGet("orders")]
    public Task<PagedViewModel<OrderViewModel>> Mine(
        [FromQuery] ListQuery query, [FromQuery] OrderStatus? status, CancellationToken ct)
        => _orders.ListMineAsync(_current.RequireId(), query, status, ct);

    [HttpGet("orders/{id:guid}")]
    [ProducesResponseType<OrderViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status403Forbidden)]
    public Task<OrderViewModel> Get(Guid id, CancellationToken ct)
        => _orders.GetAsync(id, _current.Id ?? Guid.Empty, _current.IsAdmin, ct);

    [HttpGet("invoices")]
    public Task<PagedViewModel<InvoiceViewModel>> Invoices([FromQuery] ListQuery query, CancellationToken ct)
        => _orders.InvoicesMineAsync(_current.RequireId(), query, ct);

    [HttpGet("invoices/{id:guid}")]
    public Task<InvoiceViewModel> Invoice(Guid id, CancellationToken ct)
        => _orders.GetInvoiceAsync(id, _current.Id ?? Guid.Empty, _current.IsAdmin, ct);

    /* ------------------------------ مدیریت ------------------------------ */

    [HttpGet("admin/orders")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<OrderViewModel>> All([FromQuery] ListQuery query, [FromQuery] OrderStatus? status, CancellationToken ct)
        => _orders.AdminListAsync(query, status, ct);

    [HttpPatch("admin/orders/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<OrderViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public Task<OrderViewModel> SetStatus(Guid id, [FromBody] UpdateOrderStatusInput input, CancellationToken ct)
        => _orders.UpdateStatusAsync(id, input.Status, ct);
}

/* =============================== لایسنس =============================== */

[ApiController]
[Route("api/v1/licenses")]
[Tags("لایسنس‌ها")]
[Authorize]
public sealed class LicensesController : ControllerBase
{
    private readonly ILicenseService _licenses;
    private readonly ICurrentUser _current;

    public LicensesController(ILicenseService licenses, ICurrentUser current)
    {
        _licenses = licenses;
        _current = current;
    }

    [HttpGet]
    public Task<PagedViewModel<LicenseViewModel>> Mine(
        [FromQuery] ListQuery query, [FromQuery] LicenseStatus? status, CancellationToken ct)
        => _licenses.ListMineAsync(_current.RequireId(), query, status, ct);

    [HttpGet("{id:guid}")]
    public Task<LicenseViewModel> Get(Guid id, CancellationToken ct)
        => _licenses.GetAsync(id, _current.Id ?? Guid.Empty, _current.IsAdmin, ct);

    /// <summary>فعال‌سازیِ یک نشست — ظرفیتِ لایسنس را مصرف می‌کند.</summary>
    [HttpPost("{id:guid}/action")]
    [ProducesResponseType<LicenseViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public Task<LicenseViewModel> Action(Guid id, [FromBody] LicenseActionInput input, CancellationToken ct)
        => _licenses.ActionAsync(id, input, _current.Id ?? Guid.Empty, _current.IsAdmin, ct);

    /* ------------------------------ مدیریت ------------------------------ */

    [HttpGet("/api/v1/admin/licenses")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<LicenseViewModel>> All([FromQuery] ListQuery query, [FromQuery] LicenseStatus? status, CancellationToken ct)
        => _licenses.AdminListAsync(query, status, ct);

    [HttpPost("/api/v1/admin/licenses")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<LicenseViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LicenseViewModel>> Issue([FromBody] IssueLicenseInput input, CancellationToken ct)
    {
        var license = await _licenses.IssueAsync(input, ct);
        return Created($"/api/v1/licenses/{license.Id}", license);
    }
}

/* ============================== دریافت‌ها ============================== */

[ApiController]
[Route("api/v1/downloads")]
[Tags("دریافت‌ها")]
public sealed class DownloadsController : ControllerBase
{
    private readonly IDownloadService _downloads;
    private readonly ICurrentUser _current;

    public DownloadsController(IDownloadService downloads, ICurrentUser current)
    {
        _downloads = downloads;
        _current = current;
    }

    /// <summary>فایل‌هایِ در دسترس — با پرچمِ «لایسنس دارد یا نه».</summary>
    [HttpGet("available")]
    [AllowAnonymous]
    public Task<IReadOnlyList<AvailableDownloadViewModel>> Available(CancellationToken ct)
        => _downloads.AvailableAsync(_current.Id, ct);

    [HttpGet]
    [Authorize]
    public Task<PagedViewModel<DownloadViewModel>> History([FromQuery] ListQuery query, CancellationToken ct)
        => _downloads.HistoryAsync(_current.RequireId(), query, ct);

    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<DownloadViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DownloadViewModel>> Record([FromBody] RecordDownloadInput input, CancellationToken ct)
    {
        var download = await _downloads.RecordAsync(input, _current.Id, ct);
        return Created($"/api/v1/downloads/{download.Id}", download);
    }
}

/* ============================== کدِ تخفیف ============================== */

[ApiController]
[Route("api/v1")]
[Tags("تخفیف")]
public sealed class DiscountCodesController : ControllerBase
{
    private readonly IDiscountService _discounts;

    public DiscountCodesController(IDiscountService discounts) => _discounts = discounts;

    /// <summary>بررسیِ کد و برگرداندنِ مبلغِ تخفیف.</summary>
    [HttpPost("discounts/validate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<object>> Validate([FromBody] ApplyDiscountInput input, [FromQuery] decimal subtotal = 0, CancellationToken ct = default)
    {
        var (code, percent, amount) = await _discounts.ValidateAsync(input.Code, subtotal, ct);
        return Ok(new { code, percentOff = percent, discountToman = amount });
    }

    [HttpGet("admin/discounts")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<DiscountCodeViewModel>> List([FromQuery] ListQuery query, CancellationToken ct)
        => _discounts.ListAsync(query, ct);

    [HttpPost("admin/discounts")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<DiscountCodeViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DiscountCodeViewModel>> Create([FromBody] DiscountCodeInput input, CancellationToken ct)
    {
        var created = await _discounts.CreateAsync(input, ct);
        return Created($"/api/v1/admin/discounts/{created.Id}", created);
    }

    [HttpPut("admin/discounts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<DiscountCodeViewModel> Update(Guid id, [FromBody] DiscountCodeInput input, CancellationToken ct)
        => _discounts.UpdateAsync(id, input, ct);

    [HttpDelete("admin/discounts/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _discounts.DeleteAsync(id, ct);
        return NoContent();
    }
}
