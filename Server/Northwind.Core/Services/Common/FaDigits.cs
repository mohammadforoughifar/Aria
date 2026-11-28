namespace Northwind.Core.Services.Common;

/// <summary>ارقامِ لاتین را به فارسی برمی‌گرداند (برایِ متن‌هایی که سرور می‌سازد).</summary>
public static class FaDigits
{
    private const string Persian = "۰۱۲۳۴۵۶۷۸۹";

    public static string Of(int value) => string.Concat(
        value.ToString(System.Globalization.CultureInfo.InvariantCulture)
             .Select(ch => ch is >= '0' and <= '9' ? Persian[ch - '0'] : ch));
}
