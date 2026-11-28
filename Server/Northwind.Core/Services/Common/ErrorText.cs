using System.Text.RegularExpressions;

namespace Northwind.Core.Services.Common;

/// <summary>
/// پیام‌هایِ اعتبارسنجی را به فارسیِ روشن تبدیل می‌کند.
///
/// چرا لازم است؟ اتریبیوت‌هایِ اعتبارسنجی که پیامِ فارسی ندارند (مثلِ
/// <c>[Range(1, 120)]</c> یا <c>[MaxLength(80)]</c>) متنِ پیش‌فرضِ انگلیسیِ
/// چارچوب را تولید می‌کنند و همان متن به کاربر می‌رسید. این‌جا هر پیامِ
/// بی‌فارسی با معادلِ فارسی و بدونِ جزئیاتِ فنی جای‌گزین می‌شود؛ پیام‌هایِ
/// فارسیِ خودمان دست‌نخورده می‌مانند.
/// </summary>
public static class ErrorText
{
    public const string Fallback = "مقدارِ واردشده معتبر نیست.";

    public static string Persian(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return Fallback;

        var text = message.Trim();

        // پیامِ خودمان فارسی است؛ همان را نگه می‌داریم.
        if (HasPersian(text)) return text;

        if (text.Contains("is required", StringComparison.OrdinalIgnoreCase))
            return "این فیلد الزامی است.";

        var range = Regex.Match(text, @"must be between (-?\d+) and (-?\d+)", RegexOptions.IgnoreCase);
        if (range.Success)
            return $"این مقدار باید بینِ {range.Groups[1].Value} و {range.Groups[2].Value} باشد.";

        var minMax = Regex.Match(text,
            @"minimum length of (\d+) and a maximum length of (\d+)", RegexOptions.IgnoreCase);
        if (minMax.Success)
            return $"طولِ این مقدار باید بینِ {minMax.Groups[1].Value} و {minMax.Groups[2].Value} نویسه باشد.";

        var min = Regex.Match(text, @"minimum length of (\d+)", RegexOptions.IgnoreCase);
        if (min.Success)
            return $"این مقدار باید دست‌کم {min.Groups[1].Value} نویسه باشد.";

        var maxLength = Regex.Match(text, @"maximum length of '?(\d+)'?", RegexOptions.IgnoreCase);
        if (maxLength.Success)
            return $"این مقدار حداکثر می‌تواند {maxLength.Groups[1].Value} نویسه باشد.";

        if (text.Contains("is not valid", StringComparison.OrdinalIgnoreCase)
            || text.Contains("could not be converted", StringComparison.OrdinalIgnoreCase)
            || text.Contains("not in the correct format", StringComparison.OrdinalIgnoreCase)
            || text.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return "قالبِ این مقدار درست نیست.";

        if (text.Contains("JSON", StringComparison.OrdinalIgnoreCase))
            return "بدنهٔ درخواست JSON معتبر نیست.";

        return Fallback;
    }

    /// <summary>آیا متن فارسی است؟ (حروفِ عربی/فارسی و شکل‌هایِ نمایشی)</summary>
    public static bool HasPersian(string text) =>
        text.Any(ch => (ch >= '\u0600' && ch <= '\u06FF') || (ch >= '\uFB50' && ch <= '\uFDFF'));
}
