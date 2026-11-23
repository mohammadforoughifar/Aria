using Northwind.Domain.Common;

namespace Northwind.Domain.Entities.Content;

/// <summary>
/// تنظیماتِ سایت — یک رکوردِ تکی (singleton)، به همین دلیل Id همیشه ۱ است و
/// کلیدِ Guid ندارد.
/// </summary>
public class SiteSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string SiteName { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string SalesEmail { get; set; } = "";
    public string SupportPhone { get; set; } = "";

    public string HeroHeadline { get; set; } = "";
    public string HeroSubheading { get; set; } = "";

    /// <summary>نرخِ مالیات بر ارزش افزوده به واحدِ «پایهٔ یک‌صدمِ درصد» (۹۰۰ = ۹٪).</summary>
    public int TaxRateBps { get; set; } = 900;

    public int TrialDays { get; set; } = 14;
    public bool MaintenanceMode { get; set; }
    public string? Announcement { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
