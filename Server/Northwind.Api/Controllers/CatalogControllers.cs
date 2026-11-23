using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northwind.Api.Services.Catalog;
using Northwind.Api.Services.Security;
using Northwind.Api.ViewModels.Catalog;
using Northwind.Api.ViewModels.Common;
using Northwind.Domain.Enums;

namespace Northwind.Api.Controllers;

/* ============================== محصولات ============================== */

[ApiController]
[Route("api/v1/products")]
[Tags("محصولات")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly ICurrentUser _current;

    public ProductsController(IProductService products, ICurrentUser current)
    {
        _products = products;
        _current = current;
    }

    /// <summary>فهرستِ صفحه‌بندی‌شدهٔ محصولات با فیلتر و مرتب‌سازی.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<PagedViewModel<ProductListItemViewModel>>(StatusCodes.Status200OK)]
    public Task<PagedViewModel<ProductListItemViewModel>> List(
        [FromQuery] ListQuery query,
        [FromQuery] string? category,
        [FromQuery] ProductType? type,
        [FromQuery] Audience? audience,
        [FromQuery] bool? featured,
        CancellationToken ct)
        => _products.ListAsync(new ProductFilter
        {
            CategorySlug = category,
            Type = type,
            Audience = audience,
            Featured = featured,
            IncludeInactive = _current.IsAdmin
        }, query, ct);

    /// <summary>محصولاتِ ویژهٔ صفحهٔ نخست.</summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public Task<IReadOnlyList<ProductListItemViewModel>> Featured([FromQuery] int take = 6, CancellationToken ct = default)
        => _products.FeaturedAsync(Math.Clamp(take, 1, 24), ct);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ProductDetailViewModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status404NotFound)]
    public Task<ProductDetailViewModel> Get(Guid id, CancellationToken ct)
        => _products.GetAsync(id, ct);

    /// <summary>محصول بر اساسِ اسلاگ — برایِ آدرس‌هایِ خوانا.</summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public Task<ProductDetailViewModel> GetBySlug(string slug, CancellationToken ct)
        => _products.GetBySlugAsync(slug, ct);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductDetailViewModel>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDetailViewModel>> Create([FromBody] ProductInput input, CancellationToken ct)
    {
        var created = await _products.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductDetailViewModel>(StatusCodes.Status200OK)]
    public Task<ProductDetailViewModel> Update(Guid id, [FromBody] ProductInput input, CancellationToken ct)
        => _products.UpdateAsync(id, input, ct);

    /// <summary>حذفِ محصول — اگر سفارش یا لایسنس داشته باشد، ۴۰۹ برمی‌گردد.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _products.DeleteAsync(id, ct);
        return NoContent();
    }

    /* ------------------------------ طرح‌ها ------------------------------ */

    [HttpGet("{id:guid}/plans")]
    [AllowAnonymous]
    public Task<IReadOnlyList<PlanViewModel>> Plans(Guid id, CancellationToken ct)
        => _products.ListPlansAsync(id, ct);

    [HttpPost("{id:guid}/plans")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<PlanViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PlanViewModel>> AddPlan(Guid id, [FromBody] PlanInput input, CancellationToken ct)
    {
        var plan = await _products.AddPlanAsync(id, input, ct);
        return Created($"/api/v1/products/{id}/plans/{plan.Id}", plan);
    }

    /* ------------------------------ نسخه‌ها ------------------------------ */

    [HttpGet("{id:guid}/versions")]
    [AllowAnonymous]
    public Task<IReadOnlyList<ProductVersionViewModel>> Versions(Guid id, CancellationToken ct)
        => _products.ListVersionsAsync(id, ct);

    [HttpPost("{id:guid}/versions")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ProductVersionViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductVersionViewModel>> AddVersion(Guid id, [FromBody] ProductVersionInput input, CancellationToken ct)
    {
        var version = await _products.AddVersionAsync(id, input, ct);
        return Created($"/api/v1/products/{id}/versions/{version.Id}", version);
    }
}

/// <summary>طرح‌هایِ قیمت به‌صورتِ منبعِ مستقل — ویرایش و حذف مستقیم.</summary>
[ApiController]
[Route("api/v1/plans")]
[Tags("طرح‌ها")]
[Authorize(Roles = "Admin")]
public sealed class PlansController : ControllerBase
{
    private readonly IProductService _products;

    public PlansController(IProductService products) => _products = products;

    [HttpPut("{id:guid}")]
    [ProducesResponseType<PlanViewModel>(StatusCodes.Status200OK)]
    public Task<PlanViewModel> Update(Guid id, [FromBody] PlanInput input, CancellationToken ct)
        => _products.UpdatePlanAsync(id, input, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _products.DeletePlanAsync(id, ct);
        return NoContent();
    }
}

/* ============================== دسته‌ها ============================== */

[ApiController]
[Route("api/v1/categories")]
[Tags("دسته‌ها")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyList<CategoryViewModel>> List(CancellationToken ct)
        => _categories.ListAsync(ct);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public Task<CategoryViewModel> Get(Guid id, CancellationToken ct)
        => _categories.GetAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<CategoryViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CategoryViewModel>> Create([FromBody] CategoryInput input, CancellationToken ct)
    {
        var created = await _categories.CreateAsync(input, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<CategoryViewModel> Update(Guid id, [FromBody] CategoryInput input, CancellationToken ct)
        => _categories.UpdateAsync(id, input, ct);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorViewModel>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _categories.DeleteAsync(id, ct);
        return NoContent();
    }
}

/* ============================== دیدگاه‌ها ============================== */

[ApiController]
[Route("api/v1")]
[Tags("دیدگاه‌ها")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviews;
    private readonly IProductService _products;
    private readonly ICurrentUser _current;

    public ReviewsController(IReviewService reviews, IProductService products, ICurrentUser current)
    {
        _reviews = reviews;
        _products = products;
        _current = current;
    }

    [HttpGet("products/{productId:guid}/reviews")]
    [AllowAnonymous]
    public Task<IReadOnlyList<ReviewViewModel>> ForProduct(Guid productId, CancellationToken ct)
        => _reviews.ForProductAsync(productId, ct);

    /// <summary>ثبتِ دیدگاه — با وضعیتِ «در انتظارِ تأیید» ذخیره می‌شود.</summary>
    [HttpPost("reviews")]
    [Authorize]
    [ProducesResponseType<ReviewViewModel>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewViewModel>> Create([FromBody] ReviewInput input, CancellationToken ct)
    {
        var userId = _current.RequireId();
        var me = await _products.GetAsync(input.ProductId, ct);   // اطمینان از وجودِ محصول
        var author = _current.Email ?? me.Name;

        var review = await _reviews.CreateAsync(input, userId, author, ct);
        return Created($"/api/v1/reviews/{review.Id}", review);
    }

    /// <summary>صفِ بررسیِ مدیر.</summary>
    [HttpGet("admin/reviews")]
    [Authorize(Roles = "Admin")]
    public Task<PagedViewModel<ReviewViewModel>> Queue([FromQuery] ListQuery query, [FromQuery] ReviewStatus? status, CancellationToken ct)
        => _reviews.QueueAsync(query, status, ct);

    [HttpPatch("admin/reviews/{id:guid}/status")]
    [Authorize(Roles = "Admin")]
    public Task<ReviewViewModel> SetStatus(Guid id, [FromBody] ReviewStatusInput input, CancellationToken ct)
        => _reviews.SetStatusAsync(id, input.Status, ct);

    [HttpDelete("admin/reviews/{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _reviews.DeleteAsync(id, ct);
        return NoContent();
    }
}
