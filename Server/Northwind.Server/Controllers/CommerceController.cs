using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;
using Northwind.Shared.Contracts;

namespace Northwind.Server.Controllers;

[ApiController]
[Route("api")]
public class CommerceController : ControllerBase
{
    private readonly AppDbContext _db;
    public CommerceController(AppDbContext db) => _db = db;

    private string? CurrentUserId =>
        Request.Headers.TryGetValue("X-User-Id", out var v) ? v.ToString() : null;

    private static CartItemDto MapCartItem(CartItem c) => new(
        c.Id, c.PlanId, c.Quantity,
        c.Plan!.ProductId, c.Plan.Product!.Name, c.Plan.Product.Slug, c.Plan.Name,
        c.Plan.Tier, c.Plan.BillingType, c.Plan.Product.AccentColor,
        c.Plan.PriceToman, c.Plan.PriceToman * c.Quantity);

    private async Task<CartDto> BuildCartAsync(string sessionKey, string? discountCode)
    {
        var items = await _db.CartItems.Include(c => c.Plan)!.ThenInclude(p => p!.Product)
            .Where(c => c.SessionKey == sessionKey).ToListAsync();

        var subtotal = items.Sum(i => i.Plan!.PriceToman * i.Quantity);

        int discount = 0;
        string? applied = null;
        if (!string.IsNullOrWhiteSpace(discountCode))
        {
            var code = await _db.DiscountCodes.FirstOrDefaultAsync(d =>
                d.Code == discountCode.ToUpperInvariant() && d.IsActive);
            if (code is not null && (code.ExpiresAt is null || code.ExpiresAt > DateTime.UtcNow)
                && code.TimesRedeemed < code.MaxRedemptions)
            {
                discount = (int)(subtotal * code.PercentOff / 100m);
                applied = code.Code;
            }
        }

        var taxRate = (await _db.Settings.AsNoTracking().FirstAsync()).TaxRateBps / 10_000m;
        var tax = (int)Math.Round((subtotal - discount) * taxRate);

        return new CartDto(sessionKey, items.Select(MapCartItem).ToList(),
            items.Sum(i => i.Quantity), subtotal, discount, tax,
            subtotal - discount + tax, applied, "IRT");
    }

    /* --------------------------------- سبد --------------------------------- */

    [HttpGet("cart")]
    public async Task<ActionResult<CartDto>> GetCart([FromQuery] string sessionKey, [FromQuery] string? discountCode = null)
        => Ok(await BuildCartAsync(sessionKey, discountCode));

    [HttpPost("cart/items")]
    public async Task<ActionResult<CartDto>> Add([FromQuery] string sessionKey, [FromBody] AddItemRequest req)
    {
        var plan = await _db.Plans.FindAsync(req.PlanId);
        if (plan is null) return NotFound("طرح یافت نشد.");

        var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.SessionKey == sessionKey && c.PlanId == req.PlanId);
        if (existing is not null) existing.Quantity += Math.Max(1, req.Quantity);
        else _db.CartItems.Add(new CartItem { SessionKey = sessionKey, PlanId = req.PlanId, Quantity = Math.Max(1, req.Quantity), UserId = CurrentUserId });

        await _db.SaveChangesAsync();
        return Ok(await BuildCartAsync(sessionKey, req.DiscountCode));
    }

    [HttpPut("cart/items/{itemId}")]
    public async Task<ActionResult<CartDto>> Update([FromQuery] string sessionKey, string itemId, [FromBody] UpdateItemRequest req)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.SessionKey == sessionKey);
        if (item is null) return NotFound();

        if (req.Quantity <= 0) _db.CartItems.Remove(item);
        else item.Quantity = req.Quantity;

        await _db.SaveChangesAsync();
        return Ok(await BuildCartAsync(sessionKey, req.DiscountCode));
    }

    [HttpDelete("cart/items/{itemId}")]
    public async Task<ActionResult<CartDto>> Remove([FromQuery] string sessionKey, string itemId, [FromQuery] string? discountCode = null)
    {
        var item = await _db.CartItems.FirstOrDefaultAsync(c => c.Id == itemId && c.SessionKey == sessionKey);
        if (item is not null) { _db.CartItems.Remove(item); await _db.SaveChangesAsync(); }
        return Ok(await BuildCartAsync(sessionKey, discountCode));
    }

    [HttpPost("cart/discount")]
    public async Task<ActionResult<CartDto>> ApplyDiscount([FromQuery] string sessionKey, [FromBody] DiscountRequest req)
        => Ok(await BuildCartAsync(sessionKey, req.Code));

    /* ------------------------------- تسویه‌حساب ------------------------------ */

    [HttpPost("checkout")]
    public async Task<ActionResult<OrderDto>> Checkout([FromBody] CheckoutDto input)
    {
        var cart = await BuildCartAsync(input.SessionKey, input.DiscountCode);
        if (cart.Items.Count == 0) return BadRequest("سبد خالی است.");

        var userId = CurrentUserId;
        if (userId is null) return Unauthorized("برای ثبتِ سفارش باید وارد شوید.");

        var settings = await _db.Settings.FirstAsync();
        var count = await _db.Orders.CountAsync() + 1;

        var order = new Order
        {
            OrderNumber = $"NW-1404-{1000 + count}",
            UserId = userId,
            Status = OrderStatus.Paid,                      // درگاهِ آزمایشی: موفق فرض می‌شود
            SubtotalToman = cart.SubtotalToman,
            DiscountToman = cart.DiscountToman,
            TaxToman = cart.TaxToman,
            TotalToman = cart.TotalToman,
            Currency = "IRT",
            DiscountCode = cart.DiscountCode,
            PlacedAt = DateTime.UtcNow,
            AccountType = input.AccountType,
            BillingName = input.BillingName,
            BillingCompany = input.AccountType == AccountType.Organization ? (input.CompanyName ?? input.BillingCompany) : input.BillingCompany,
            BillingEmail = input.BillingEmail,
            BillingCountry = input.BillingCountry,
            EconomicCode = input.EconomicCode,
            NationalId = input.NationalId,
            Notes = input.Notes,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId, PlanId = i.PlanId, ProductName = i.ProductName, PlanName = i.PlanName,
                BillingType = i.BillingType, UnitPriceToman = i.UnitPriceToman,
                Quantity = i.Quantity, LineTotalToman = i.LineTotalToman,
            }).ToList(),
            Payment = new Payment
            {
                AmountToman = cart.TotalToman, Method = input.PaymentMethod,
                Status = PaymentStatus.Succeeded, TransactionRef = $"TX-{Random.Shared.Next(100000, 999999)}",
                PaidAt = DateTime.UtcNow,
            },
            Invoice = new Invoice
            {
                InvoiceNumber = $"INV-1404-{2000 + count}", Status = InvoiceStatus.Paid,
                IssuedAt = DateTime.UtcNow, DueAt = DateTime.UtcNow.AddDays(30),
                TotalToman = cart.TotalToman, TaxToman = cart.TaxToman,
            },
        };

        _db.Orders.Add(order);

        // لایسنس برای هر آیتم صادر می‌شود
        foreach (var item in cart.Items)
        {
            var plan = await _db.Plans.FindAsync(item.PlanId);
            _db.Licenses.Add(new License
            {
                LicenseKey = NewKey(), UserId = userId, ProductId = item.ProductId, PlanId = item.PlanId,
                Seats = plan?.Seats ?? 1, Status = LicenseStatus.Active, IssuedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(plan?.LicenseMonths ?? 12),
            });
        }

        if (cart.DiscountCode is not null)
        {
            var code = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Code == cart.DiscountCode);
            if (code is not null) code.TimesRedeemed++;
        }

        _db.CartItems.RemoveRange(_db.CartItems.Where(c => c.SessionKey == input.SessionKey));
        await _db.SaveChangesAsync();

        settings.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(await MapOrderAsync(order.Id));
    }

    /* -------------------------------- سفارش‌ها ------------------------------- */

    private async Task<OrderDto> MapOrderAsync(string id)
    {
        var o = await _db.Orders.Include(x => x.Items).Include(x => x.Payment)
            .Include(x => x.Invoice).Include(x => x.User).FirstAsync(x => x.Id == id);
        return new OrderDto(
            o.Id, o.OrderNumber, o.UserId, o.User?.FullName, o.User?.Email, o.BillingCompany,
            o.Status, o.SubtotalToman, o.DiscountToman, o.TaxToman, o.TotalToman, o.Currency,
            o.DiscountCode, o.PlacedAt, o.BillingName, o.BillingCompany, o.BillingEmail, o.BillingCountry,
            o.Notes,
            o.Items.Select(i => new OrderItemDto(i.Id, i.ProductId, i.PlanId, i.ProductName, i.PlanName,
                i.BillingType, i.UnitPriceToman, i.Quantity, i.LineTotalToman)).ToList(),
            o.Payment is null ? null : new PaymentDto(o.Payment.Id, o.Payment.OrderId, o.Payment.Provider,
                o.Payment.Method, o.Payment.AmountToman, o.Payment.Status, o.Payment.TransactionRef, o.Payment.PaidAt),
            o.Invoice is null ? null : new InvoiceDto(o.Invoice.Id, o.Invoice.InvoiceNumber, o.Invoice.OrderId,
                o.Invoice.Status, o.Invoice.IssuedAt, o.Invoice.DueAt, o.Invoice.TotalToman, o.Invoice.TaxToman, null));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<Paged<OrderDto>>> Orders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null, [FromQuery] string? search = null)
    {
        var q = _db.Orders.Include(o => o.User).AsQueryable();

        // مشتری فقط سفارش‌های خودش را می‌بیند؛ مدیر همه را
        var uid = CurrentUserId;
        var isAdmin = uid is not null && await _db.Users.AnyAsync(u => u.Id == uid && u.Role == Role.Admin);
        if (!isAdmin && uid is not null) q = q.Where(o => o.UserId == uid);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var st))
            q = q.Where(o => o.Status == st);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(o => o.OrderNumber.Contains(s) || (o.User != null && o.User.FullName.Contains(s))
                          || (o.BillingCompany != null && o.BillingCompany.Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(o => o.PlacedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => o.Id).ToListAsync();

        var list = new List<OrderDto>();
        foreach (var id in items) list.Add(await MapOrderAsync(id));

        return Ok(new Paged<OrderDto>(list, new PagedMeta(page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpGet("orders/{id}")]
    public async Task<ActionResult<OrderDto>> Order(string id)
    {
        if (!await _db.Orders.AnyAsync(o => o.Id == id)) return NotFound();
        return Ok(await MapOrderAsync(id));
    }

    [HttpPatch("orders/{id}/status")]
    public async Task<ActionResult<OrderDto>> Status(string id, [FromBody] StatusRequest req)
    {
        var o = await _db.Orders.FindAsync(id);
        if (o is null) return NotFound();
        if (Enum.TryParse<OrderStatus>(req.Status, true, out var st)) o.Status = st;
        await _db.SaveChangesAsync();
        return Ok(await MapOrderAsync(id));
    }

    /* -------------------------------- فاکتورها ------------------------------- */

    [HttpGet("invoices")]
    public async Task<ActionResult<Paged<InvoiceDto>>> Invoices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var q = _db.Invoices.Include(i => i.Order).AsQueryable();
        var uid = CurrentUserId;
        var isAdmin = uid is not null && await _db.Users.AnyAsync(u => u.Id == uid && u.Role == Role.Admin);
        if (!isAdmin && uid is not null) q = q.Where(i => i.Order!.UserId == uid);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(i => i.IssuedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new Paged<InvoiceDto>(
            items.Select(i => new InvoiceDto(i.Id, i.InvoiceNumber, i.OrderId, i.Status, i.IssuedAt,
                i.DueAt, i.TotalToman, i.TaxToman, null)).ToList(),
            new PagedMeta(page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize))));
    }

    [HttpGet("invoices/{id}")]
    public async Task<ActionResult<InvoiceDto>> Invoice(string id)
    {
        var i = await _db.Invoices.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return NotFound();
        var order = await MapOrderAsync(i.OrderId);
        return Ok(new InvoiceDto(i.Id, i.InvoiceNumber, i.OrderId, i.Status, i.IssuedAt, i.DueAt,
            i.TotalToman, i.TaxToman, order));
    }

    private static string NewKey()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var blocks = new List<string>();
        for (var b = 0; b < 4; b++)
        {
            var s = new char[5];
            for (var i2 = 0; i2 < 5; i2++) s[i2] = chars[Random.Shared.Next(chars.Length)];
            blocks.Add(new string(s));
        }
        return string.Join("-", blocks);
    }
}

public sealed record AddItemRequest(string PlanId, int Quantity = 1, string? DiscountCode = null);
public sealed record UpdateItemRequest(int Quantity, string? DiscountCode = null);
public sealed record DiscountRequest(string Code);
public sealed record StatusRequest(string Status);
