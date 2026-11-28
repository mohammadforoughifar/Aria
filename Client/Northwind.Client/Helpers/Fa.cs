// فرمت‌بندی فارسی: ارقام فارسی، تقویم شمسی، مبالغ و زمان نسبی.
// (مستقل از CultureInfo تا در حالت InvariantGlobalization هم درست کار کند.)

namespace Northwind.Client.Helpers;

public static class Fa
{
    private const string Digits = "۰۱۲۳۴۵۶۷۸۹";
    private static readonly string[] Months =
        { "", "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
             "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

    private static readonly System.Globalization.PersianCalendar Pc = new();

    /// <summary>ارقام یک رشته را به فارسی تبدیل می‌کند و جداکنندهٔ هزارگان را عربی می‌گذارد.</summary>
    public static string DigitsOf(string input)
    {
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] >= '0' && chars[i] <= '9') chars[i] = Digits[chars[i] - '0'];
            else if (chars[i] == ',') chars[i] = '٬';
        }
        return new string(chars);
    }

    public static string Num(long value) => DigitsOf(value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
    public static string Num(decimal value) => DigitsOf(value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture));
    public static string Num(int value) => Num((long)value);

    /// <summary>
    /// مبلغ به «تومان» با ارقامِ فارسی: «۴۹٬۸۰۰٬۰۰۰ تومان».
    ///
    /// مبالغ <c>decimal</c> هستند (تا میلیاردی جا داشته باشند). تومان در ایران
    /// واحدِ بدونِ اعشار است، پس اعشار فقط وقتی نشان داده می‌شود که واقعاً باشد؛
    /// در حالتِ عادی عددِ صحیح و تمیز می‌بینید، نه «۴۹٬۸۰۰٬۰۰۰٫۰۰».
    /// </summary>
    public static string Money(decimal toman)
    {
        var whole = Math.Round(toman, 2, MidpointRounding.AwayFromZero);
        var text = whole == Math.Truncate(whole)
            ? whole.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)
            : whole.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
        return DigitsOf(text) + " تومان";
    }

    public static string Money(int toman) => Money((decimal)toman);

    /// <summary>مبلغِ فشرده برای نمودارها و KPIها: «۴۹٫۸ میلیون تومان»</summary>
    public static string MoneyCompact(decimal toman)
    {
        var abs = Math.Abs(toman);
        if (abs >= 1_000_000_000) return $"{DigitsOf((toman / 1_000_000_000m).ToString("0.#"))} میلیارد تومان";
        if (abs >= 1_000_000) return $"{DigitsOf((toman / 1_000_000m).ToString("0.#"))} میلیون تومان";
        if (abs >= 1_000) return $"{DigitsOf((toman / 1_000m).ToString("0"))} هزار تومان";
        return Money(toman);
    }

    public static string MoneyCompact(int toman) => MoneyCompact((decimal)toman);

    /// <summary>
    /// تاریخِ شمسی. سال با جداکنندهٔ هزارگان نمی‌آید (۱۴۰۵، نه ۱٬۴۰۵)؛ جداکننده
    /// فقط برایِ مبلغ و مقدار است، نه برایِ شمارهٔ سال.
    /// </summary>
    public static string Date(DateTime? value) =>
        value is null
            ? "—"
            : $"{Num(Pc.GetDayOfMonth(value.Value))} {Months[Pc.GetMonth(value.Value)]} " +
              DigitsOf(Pc.GetYear(value.Value).ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static string Num2(int value) => DigitsOf(value.ToString("00", System.Globalization.CultureInfo.InvariantCulture));

    public static string DateTime(DateTime? value) =>
        value is null ? "—" : $"{Date(value)}، ساعت {Num2(value.Value.Hour)}:{Num2(value.Value.Minute)}";

    public static string TimeAgo(DateTime? value)
    {
        if (value is null) return "—";
        var diff = System.DateTime.UtcNow - value.Value.ToUniversalTime();
        var mins = (int)diff.TotalMinutes;
        if (mins < 1) return "همین حالا";
        if (mins < 60) return $"{Num(mins)} دقیقه پیش";
        var hours = mins / 60;
        if (hours < 24) return $"{Num(hours)} ساعت پیش";
        var days = hours / 24;
        if (days < 30) return $"{Num(days)} روز پیش";
        var months = days / 30;
        if (months < 12) return $"{Num(months)} ماه پیش";
        return $"{Num(months / 12)} سال پیش";
    }

    public static int? DaysUntil(DateTime? value)
    {
        if (value is null) return null;
        return (int)Math.Ceiling((value.Value.ToUniversalTime() - System.DateTime.UtcNow).TotalDays);
    }

    public static string Size(int mb) =>
        mb >= 1024
            ? $"{Num(Math.Round(mb / 1024m, 1))} گیگابایت"
            : $"{Num(mb)} مگابایت";

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).ToArray();
        return string.Concat(parts.Select(p => p.Length > 0 ? p[0].ToString() : ""));
    }

    public static string Percent(int value) => $"{Num(value)}٪";

    public static string SignedPercent(decimal value)
    {
        var sign = value > 0 ? "+" : value < 0 ? "−" : "";
        return $"{sign}{DigitsOf(Math.Abs(Math.Round(value, 1)).ToString("0.0"))}٪";
    }
}
