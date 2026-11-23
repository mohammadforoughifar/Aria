using Northwind.Domain.Common;
using Northwind.Domain.Enums;

namespace Northwind.Domain.Entities.Catalog;

/// <summary>یک نرم‌افزارِ قابلِ فروش.</summary>
public class Product : EntityBase
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";

    public ProductType Type { get; set; } = ProductType.Module;
    public Audience Audience { get; set; } = Audience.Both;

    public string Platform { get; set; } = "Windows";
    public string Version { get; set; } = "1.0.0";

    /// <summary>قیمتِ پایه به تومان — عددِ صحیح، چون تومان واحدِ خرد ندارد.</summary>
    public int BasePriceToman { get; set; }

    public string AccentColor { get; set; } = "#1E3A8A";
    public string? HeroImage { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public int TrialDays { get; set; } = 14;

    /// <summary>امتیازِ محبوبیت برایِ مرتب‌سازیِ «پرفروش‌ترین».</summary>
    public int Popularity { get; set; }

    /// <summary>ویژگی‌ها — در SQL Server به‌صورتِ ستونِ JSON ذخیره می‌شود.</summary>
    public List<string> Features { get; set; } = new();

    /// <summary>نیازمندی‌هایِ سیستم — ستونِ JSON.</summary>
    public List<string> Requirements { get; set; } = new();

    /// <summary>آدرسِ تصاویرِ گالری — ستونِ JSON.</summary>
    public List<string> Gallery { get; set; } = new();

    public string? DemoVideoUrl { get; set; }

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<ProductVersion> Versions { get; set; } = new List<ProductVersion>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
