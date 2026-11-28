using Northwind.Data.Common;

namespace Northwind.Data.Entities.Catalog;

/// <summary>دستهٔ محصولات.</summary>
public class Category : EntityBase
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public string Icon { get; set; } = "package";
    public int SortOrder { get; set; }

    /* ------------------- قیمت‌گذاریِ امکاناتِ جانبیِ همین دسته -------------------
       مدیر برایِ هر دسته تعیین می‌کند که «هر کاربرِ اضافه»، «هر شرکتِ اضافه» و
       «نسخهٔ اندروید/iOS» چقدر باشد. این مقدارها هنگامِ خرید به قیمتِ پایهٔ هر
       ماژول اضافه می‌شوند — یک‌بار برایِ هر ماژولِ انتخابی.                     */

    /// <summary>قیمتِ هر کاربرِ بیشتر از تعدادِ شاملِ قیمتِ پایه.</summary>
    public decimal PricePerUserToman { get; set; }

    /// <summary>قیمتِ هر شرکتِ اضافه (کاربرانِ هر شرکت همان تعدادِ خریداری‌شده است).</summary>
    public decimal PricePerExtraCompanyToman { get; set; }

    /// <summary>قیمتِ افزودنِ نسخهٔ اندروید.</summary>
    public decimal PriceAndroidToman { get; set; }

    /// <summary>قیمتِ افزودنِ نسخهٔ iOS.</summary>
    public decimal PriceIosToman { get; set; }

    /// <summary>چند کاربر در قیمتِ پایهٔ ماژول حساب شده است (پیش‌فرض: ۱).</summary>
    public int UsersIncluded { get; set; } = 1;

    /// <summary>چند شرکت در قیمتِ پایه حساب شده است (پیش‌فرض: ۱).</summary>
    public int CompaniesIncluded { get; set; } = 1;

    /// <summary>سقفِ تعدادِ کاربر برایِ خریدِ یک ماژول.</summary>
    public int MaxUsers { get; set; } = 50;

    /// <summary>سقفِ تعدادِ شرکتِ اضافه.</summary>
    public int MaxExtraCompanies { get; set; } = 10;

    /// <summary>آیا خریدِ چندشرکتی برایِ این دسته فعال است؟</summary>
    public bool AllowMultiCompany { get; set; } = true;

    /// <summary>آیا نسخهٔ اندروید/iOS به‌عنوانِ افزونه فروخته می‌شود؟</summary>
    public bool AllowMobileAddons { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
