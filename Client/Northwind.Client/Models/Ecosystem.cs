// مدل‌هایِ «دیاگرامِ اکوسیستمِ نرم‌افزار».
// داده‌ها از یک فایلِ JSON در wwwroot/data خوانده می‌شوند تا بعداً بشود
// همان‌ها را از پنلِ مدیریت هم ویرایش کرد.

using System.Text.Json.Serialization;

namespace Northwind.Client.Models;

/// <summary>کلِ پیکربندیِ دیاگرام.</summary>
public sealed class EcosystemConfig
{
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public EcosystemHub Hub { get; set; } = new();
    public List<EcosystemCard> People { get; set; } = new();
    public List<EcosystemCard> Systems { get; set; } = new();
    public List<EcosystemScenario> Scenarios { get; set; } = new();
}

/// <summary>هابِ مرکزی (محصول).</summary>
public sealed class EcosystemHub
{
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
}

/// <summary>یک کارت در ستونِ کاربران یا سیستم‌ها.</summary>
public sealed class EcosystemCard
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Desc { get; set; } = "";
    public string Icon { get; set; } = "user";

    /// <summary>رنگِ نشان در حالتِ فعال — از نشانه‌های تم استفاده می‌شود.</summary>
    public string BadgeTone { get; set; } = "brand";

    [JsonIgnore]
    public bool IsPerson { get; set; }
}

/// <summary>یک سناریو: با انتخابش، کارت‌ها و اتصال‌هایِ مرتبط فعال می‌شوند.</summary>
public sealed class EcosystemScenario
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>شناسهٔ کارت‌هایِ درگیر در این سناریو.</summary>
    public List<string> Cards { get; set; } = new();

    /// <summary>شناسهٔ اتصال‌هایِ فعال (اگر خالی باشد، همان Cards استفاده می‌شود).</summary>
    public List<string> Connectors { get; set; } = new();

    /// <summary>نشانِ وضعیتی که روی هر کارتِ فعال نمایش داده می‌شود (به‌ترتیبِ Cards).</summary>
    public Dictionary<string, string> Badges { get; set; } = new();
}
