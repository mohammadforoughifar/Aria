using Northwind.Data.Common;
using Northwind.Data.Entities.Catalog;

namespace Northwind.Data.Entities.Cart;

/// <summary>
/// یک ردیفِ سبدِ خرید. سبد برایِ کاربرِ مهمان با <see cref="SessionKey"/> و برایِ
/// کاربرِ واردشده با <see cref="UserId"/> شناخته می‌شود.
/// </summary>
public class CartItem : EntityBase
{
    public string SessionKey { get; set; } = "";
    public Guid? UserId { get; set; }

    public Guid PlanId { get; set; }
    public Plan? Plan { get; set; }

    public int Quantity { get; set; } = 1;

    /* -------------------- پیکربندیِ ماژول در سبد --------------------
       مشتری رویِ صفحهٔ نرم‌افزارها تعدادِ کاربر، شرکتِ اضافه و نسخه‌هایِ موبایل
       را انتخاب می‌کند. همان انتخاب‌ها این‌جا می‌مانند تا سبد، تسویه و فاکتور
       دقیقاً همان چیزی را نشان دهند که خریدار چیده است.                     */

    /// <summary>تعدادِ کاربر برایِ هر شرکت (پیش‌فرض: ۱).</summary>
    public int Users { get; set; } = 1;

    /// <summary>تعدادِ شرکتِ اضافه بر شرکتِ اصلی.</summary>
    public int ExtraCompanies { get; set; }

    public bool IncludeAndroid { get; set; }
    public bool IncludeIos { get; set; }

    /// <summary>
    /// قیمتِ واحدِ محاسبه‌شده برایِ همین پیکربندی. اگر خالی باشد، ردیف از مسیرِ
    /// قدیمیِ طرح‌هایِ قیمت آمده و قیمتش از <c>Plan.PriceToman</c> خوانده می‌شود.
    /// </summary>
    public decimal? ConfiguredUnitPriceToman { get; set; }
}
