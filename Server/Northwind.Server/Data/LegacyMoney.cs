namespace Northwind.Server.Data;

/// <summary>
/// پشتهٔ قدیمی (AppDbContext) ستون‌هایِ مبلغش هنوز <c>int</c> است.
///
/// قراردادهایِ مشترک و API نسخهٔ ۱ <c>decimal</c> شده‌اند تا مبلغ‌هایِ میلیاردی جا
/// داشته باشند؛ ولی تا زمانی که صفحه‌هایِ قدیمی رویِ همین پایگاه‌داده کار می‌کنند،
/// این پل لازم است. هر جا مقدار از سقفِ int بگذرد به سقف بریده می‌شود تا
/// ذخیره‌سازی به‌جایِ پرتابِ خطا، حداکثرِ ممکن را نگه دارد.
/// این کلاس با حذفِ پشتهٔ قدیمی حذف می‌شود.
/// </summary>
internal static class LegacyMoney
{
    public static int ToLegacy(decimal toman)
    {
        if (toman > int.MaxValue) return int.MaxValue;
        if (toman < int.MinValue) return int.MinValue;
        return (int)Math.Round(toman, MidpointRounding.AwayFromZero);
    }
}
