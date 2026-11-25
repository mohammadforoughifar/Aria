using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Core.Services.Admin;
using Northwind.Core.Services.Identity;
using Northwind.Core.Services.Security;
using Northwind.Core.Models.Account;
using Northwind.Core.Models.Common;
using Northwind.Core.Models.Content;

namespace Northwind.Api.Controllers;

/* ============================ احراز هویت ============================ */

[ApiController]
[Route("api/v1/auth")]
[Tags("احراز هویت")]
public sealed class AuthController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AuthController(IAccountService accounts) => _accounts = accounts;

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResultViewModel>> Register([FromBody] RegisterInput input, CancellationToken ct)
    {
        var result = await _accounts.RegisterAsync(input, ct);
        return Created($"/api/v1/account/me", result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public Task<AuthResultViewModel> Login([FromBody] LoginInput input, CancellationToken ct)
        => _accounts.LoginAsync(input, ct);
}

/* ============================== حساب من ============================== */

[ApiController]
[Route("api/v1/account")]
[Tags("حساب کاربری")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService _accounts;
    private readonly ICurrentUser _current;

    public AccountController(IAccountService accounts, ICurrentUser current)
    {
        _accounts = accounts;
        _current = current;
    }

    [HttpGet("me")]
    public Task<UserViewModel> Me(CancellationToken ct)
        => _accounts.MeAsync(_current.RequireId(), ct);

    [HttpPut("me")]
    public Task<UserViewModel> UpdateProfile([FromBody] UpdateProfileInput input, CancellationToken ct)
        => _accounts.UpdateProfileAsync(_current.RequireId(), input, ct);

    [HttpPost("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordInput input, CancellationToken ct)
    {
        await _accounts.ChangePasswordAsync(_current.RequireId(), input, ct);
        return NoContent();
    }

    /// <summary>خلاصهٔ پورتالِ مشتری — همهٔ کارت‌هایِ داشبورد در یک درخواست.</summary>
    [HttpGet("overview")]
    public Task<CustomerOverviewViewModel> Overview(CancellationToken ct)
        => _accounts.OverviewAsync(_current.RequireId(), ct);
}

/* ============================== پنلِ مدیر ============================== */

[ApiController]
[Route("api/v1/admin")]
[Tags("مدیریت")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IAccountService _accounts;

    public AdminController(IAdminService admin, IAccountService accounts)
    {
        _admin = admin;
        _accounts = accounts;
    }

    /// <summary>شاخص‌ها، درآمدِ ۱۲ ماه، پرفروش‌ها و آخرین رویدادها.</summary>
    [HttpGet("overview")]
    public Task<AdminOverviewViewModel> Overview(CancellationToken ct)
        => _admin.OverviewAsync(ct);

    [HttpGet("customers")]
    public Task<PagedViewModel<CustomerViewModel>> Customers([FromQuery] ListQuery query, CancellationToken ct)
        => _accounts.ListCustomersAsync(query, ct);

    [HttpPut("customers/{id:guid}")]
    [ProducesResponseType<CustomerViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public Task<CustomerViewModel> UpdateCustomer(Guid id, [FromBody] UpdateCustomerInput input, CancellationToken ct)
        => _accounts.UpdateCustomerAsync(id, input, ct);
}
