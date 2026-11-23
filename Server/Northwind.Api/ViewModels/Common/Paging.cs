using System.ComponentModel.DataAnnotations;

namespace Northwind.Api.ViewModels.Common;

/// <summary>فرادادهٔ صفحه‌بندی — هم‌شکل با کلاینت.</summary>
public sealed record PagedMetaViewModel(int Page, int PageSize, int TotalCount, int TotalPages);

/// <summary>پوششِ استانداردِ فهرست‌ها.</summary>
public sealed record PagedViewModel<T>(IReadOnlyList<T> Items, PagedMetaViewModel Meta)
{
    public static PagedViewModel<T> Empty(int pageSize = 12)
        => new(Array.Empty<T>(), new PagedMetaViewModel(1, pageSize, 0, 1));
}

/// <summary>
/// پارامترهایِ مشترکِ فهرست‌هایِ REST:
/// <c>GET /api/products?page=2&amp;pageSize=24&amp;search=حساب&amp;sort=price_desc</c>
/// </summary>
public class ListQuery
{
    public const int MaxPageSize = 100;

    /// <summary>شمارهٔ صفحه — از ۱ شروع می‌شود.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "شمارهٔ صفحه باید بزرگ‌تر از صفر باشد.")]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize, ErrorMessage = "تعدادِ هر صفحه باید بین ۱ و ۱۰۰ باشد.")]
    public int PageSize { get; set; } = 12;

    /// <summary>جست‌وجو در نام و توضیح.</summary>
    [MaxLength(120)]
    public string? Search { get; set; }

    /// <summary>کلیدِ مرتب‌سازی، مثلِ <c>price_desc</c> یا <c>newest</c>.</summary>
    [MaxLength(40)]
    public string? Sort { get; set; }

    /// <summary>مقادیر را در بازهٔ مجاز نگه می‌دارد تا کلاینت نتواند پایگاه‌داده را بکشد.</summary>
    public void Normalize()
    {
        Page = Page < 1 ? 1 : Page;
        PageSize = PageSize is < 1 or > MaxPageSize ? 12 : PageSize;
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
    }
}
