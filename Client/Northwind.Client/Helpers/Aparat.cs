// کمکی‌هایِ ویدیویِ معرفیِ محصول (آپارات).
//
// لینک‌هایی که مدیر می‌تواند وارد کند:
//   https://www.aparat.com/v/nlk1rob
//   https://www.aparat.com/v/0sPat/معرفی-نرم-افزار
//   https://www.aparat.com/video/video/embed/videohash/nlk1rob/vt/frame   (لینکِ آمادهٔ جای‌گذاری)
//   https://.../file.mp4                                                  (ویدیویِ مستقیم)
//
// خروجی: لینکِ قابلِ جای‌گذاری در <iframe> و نشانیِ تصویرِ بندانگشتی.

using System.Text.RegularExpressions;

namespace Northwind.Client.Helpers;

public static partial class Aparat
{
    [GeneratedRegex(@"aparat\.com/(?:v|video/video/embed/videohash)/(?<hash>[A-Za-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex HashPattern();

    [GeneratedRegex(@"^https?://.+\.(mp4|webm|ogg)(\?.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DirectVideoPattern();

    /// <summary>کدِ ویدیو (hash) در آپارات؛ اگر لینکِ آپارات نباشد null.</summary>
    public static string? HashOf(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var m = HashPattern().Match(url.Trim());
        return m.Success ? m.Groups["hash"].Value : null;
    }

    /// <summary>لینکِ آپارات است (هر شکلی).</summary>
    public static bool IsAparat(string? url) => HashOf(url) is not null;

    /// <summary>ویدیویِ مستقیم (mp4/webm/ogg) است.</summary>
    public static bool IsDirect(string? url)
        => !string.IsNullOrWhiteSpace(url) && DirectVideoPattern().IsMatch(url.Trim());

    /// <summary>نشانیِ آماده برای <c>src</c> اگر and no <c>iframe</c>؛ برای mp4 و بقیه null.</summary>
    public static string? EmbedUrl(string? url)
    {
        var hash = HashOf(url);
        return hash is null ? null : $"https://www.aparat.com/video/video/embed/videohash/{hash}/vt/frame";
    }

    /// <summary>تصویرِ بندانگشتیِ آپارات؛ برایِ نمایشِ «پخش» پیش از بارگذاریِ پخش‌کننده.</summary>
    public static string? ThumbnailUrl(string? url)
    {
        var hash = HashOf(url);
        return hash is null ? null : $"https://www.aparat.com/video/video/thumbnail/videohash/{hash}/w/720/h/405";
    }

    /// <summary>آیا لینک به‌شکلی است که می‌توانیم نشانش دهیم؟</summary>
    public static bool IsPlayable(string? url) => IsAparat(url) || IsDirect(url);

    /// <summary>پیامِ راهنمایِ فارسی برایِ لینکِ نامعتبر.</summary>
    public static string? Problem(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null; // خالی = اختیاری

        var u = url.Trim();
        if (IsAparat(u) || IsDirect(u)) return null;

        if (u.Contains("aparat.com", StringComparison.OrdinalIgnoreCase))
            return "این لینکِ آپارات شناسایی نشد. نشانیِ صفحهٔ ویدیو باید شکلِ «aparat.com/v/کدِویدیو» داشته باشد.";

        return "فقط لینکِ آپارات (aparat.com/v/…) یا فایلِ ویدیویِ مستقیم (mp4) پذیرفته می‌شود.";
    }
}
