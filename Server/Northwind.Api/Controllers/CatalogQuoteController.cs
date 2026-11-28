using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Northwind.Core.Models.Common;
using Northwind.Core.Services.Catalog;

namespace Northwind.Api.Controllers;

/* ======================= قیمت‌گذاریِ پیکربندیِ خرید =======================
   صفحهٔ نرم‌افزارها انتخاب‌هایِ مشتری (ماژول + تعدادِ کاربر + شرکتِ اضافه +
   اندروید/iOS) را همین‌جا قیمت می‌زند. قیمت همیشه از سرور می‌آید، نه از
   حساب‌وکتابِ مرورگر؛ پس دست‌کاریِ صفحه نمی‌تواند مبلغِ نهایی را عوض کند. */

[ApiController]
[Route("api/v1/catalog")]
[Tags("کاتالوگ")]
public sealed class CatalogQuoteController : ControllerBase
{
    private readonly IModulePricingService _pricing;

    public CatalogQuoteController(IModulePricingService pricing) => _pricing = pricing;

    /// <summary>قیمتِ یک یا چند ماژول با پیکربندیِ انتخابیِ مشتری.</summary>
    [HttpPost("quote")]
    [AllowAnonymous]
    [ProducesResponseType<ModuleQuoteView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status400BadRequest)]
    public Task<ModuleQuoteView> Quote([FromBody] ModuleQuoteInput input, CancellationToken ct)
        => _pricing.QuoteAsync(
            input.Items.Select(i => new ModuleSelection
            {
                ProductId = i.ProductId,
                Users = i.Users,
                ExtraCompanies = i.ExtraCompanies,
                IncludeAndroid = i.IncludeAndroid,
                IncludeIos = i.IncludeIos
            }).ToList(), ct);
}

/// <summary>بدنهٔ درخواستِ قیمت‌گذاری.</summary>
public sealed class ModuleQuoteInput
{
    public List<ModuleQuoteItem> Items { get; set; } = new();
}

public sealed class ModuleQuoteItem
{
    public Guid ProductId { get; set; }

    /// <summary>تعدادِ کاربر (شاملِ کاربرِ قیمتِ پایه).</summary>
    public int Users { get; set; } = 1;

    /// <summary>تعدادِ شرکتِ اضافه بر شرکتِ اصلی.</summary>
    public int ExtraCompanies { get; set; }

    public bool IncludeAndroid { get; set; }
    public bool IncludeIos { get; set; }
}
