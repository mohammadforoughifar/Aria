using Microsoft.EntityFrameworkCore;
using Northwind.Core.Services.Common;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Persistence;

namespace Northwind.Core.Services.Catalog;

/// <summary>ریزِ محاسبهٔ قیمتِ یک ماژول با پیکربندیِ انتخابیِ مشتری.</summary>
public sealed record ModulePriceView(
    decimal BaseToman,
    decimal UsersToman,
    decimal CompaniesToman,
    decimal AndroidToman,
    decimal IosToman,
    decimal TotalToman,
    int Users,
    int ExtraCompanies,
    bool IncludeAndroid,
    bool IncludeIos,
    int IncludedUsers,
    int IncludedCompanies);

/// <summary>
/// یکی از انتخاب‌هایِ مشتری: کدام ماژول، با چند کاربر، چند شرکتِ اضافه و
/// کدام نسخه‌هایِ موبایل.
/// </summary>
public sealed class ModuleSelection
{
    public Guid ProductId { get; set; }

    /// <summary>تعدادِ کاربر برایِ هر شرکت (شاملِ کاربرِ قیمتِ پایه).</summary>
    public int Users { get; set; } = 1;

    /// <summary>شرکت‌هایِ اضافه بر شرکتِ اصلی.</summary>
    public int ExtraCompanies { get; set; }

    public bool IncludeAndroid { get; set; }
    public bool IncludeIos { get; set; }
}

/// <summary>یک ردیفِ قیمت‌گذاری‌شده که به کلاینت برمی‌گردد.</summary>
public sealed record ModuleQuoteLine(
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    string CategoryId,
    string CategoryName,
    string CategorySlug,
    string AccentColor,
    ModulePriceView Price,
    string Summary);

/// <summary>جمعِ کلِ انتخاب‌هایِ مشتری.</summary>
public sealed record ModuleQuoteView(
    IReadOnlyList<ModuleQuoteLine> Lines,
    int ModuleCount,
    decimal TotalToman,
    string Currency);

public interface IModulePricingService
{
    /// <summary>قیمتِ یک ماژول را با پیکربندیِ داده‌شده حساب می‌کند (بدونِ دسترسی به پایگاه‌داده).</summary>
    ModulePriceView Calculate(Product product, Category category, ModuleSelection selection);

    /// <summary>چند انتخاب را یک‌جا قیمت می‌زند؛ قیمتِ هر ردیف مستقل است.</summary>
    Task<ModuleQuoteView> QuoteAsync(IReadOnlyList<ModuleSelection> selections, CancellationToken ct = default);
}

public sealed class ModulePricingService : IModulePricingService
{
    private readonly NorthwindDbContext _db;

    public ModulePricingService(NorthwindDbContext db) => _db = db;

    public ModulePriceView Calculate(Product product, Category category, ModuleSelection selection)
    {
        var includedUsers = Math.Max(1, category.UsersIncluded);
        var includedCompanies = Math.Max(1, category.CompaniesIncluded);

        var users = selection.Users <= 0 ? includedUsers : selection.Users;
        var maxUsers = Math.Max(includedUsers, category.MaxUsers);
        if (users > maxUsers)
            throw new BadRequestException(
                $"تعدادِ کاربر برایِ «{category.Name}» حداکثر {maxUsers} نفر است.", "users_out_of_range");

        var extraCompanies = Math.Max(0, selection.ExtraCompanies);
        if (extraCompanies > 0 && !category.AllowMultiCompany)
            throw new BadRequestException(
                $"نسخهٔ چندشرکتی برایِ «{category.Name}» فعال نیست.", "multi_company_disabled");

        var maxCompanies = Math.Max(0, category.MaxExtraCompanies);
        if (extraCompanies > maxCompanies)
            throw new BadRequestException(
                $"تعدادِ شرکتِ اضافه برایِ «{category.Name}» حداکثر {maxCompanies} است.", "companies_out_of_range");

        if (category.AllowMobileAddons is false && (selection.IncludeAndroid || selection.IncludeIos))
            throw new BadRequestException(
                $"نسخهٔ موبایل برایِ «{category.Name}» فروخته نمی‌شود.", "mobile_addons_disabled");

        /* کاربران و شرکت‌ها بر پایهٔ «شاملِ قیمتِ پایه» حساب می‌شوند:
           ماژول با یک کاربر و یک شرکت فروخته می‌شود (پیش‌فرض: نسخهٔ دسکتاپ)
           و هر کاربر/شرکتِ بیشتر، افزونهٔ قیمت‌دار است. */
        var billableUsers = Math.Max(0, users - includedUsers);
        var billableCompanies = Math.Max(0, extraCompanies - (includedCompanies - 1));

        var basePrice = product.BasePriceToman;
        var usersPrice = billableUsers * category.PricePerUserToman;
        var companiesPrice = billableCompanies * category.PricePerExtraCompanyToman;
        var androidPrice = selection.IncludeAndroid ? category.PriceAndroidToman : 0m;
        var iosPrice = selection.IncludeIos ? category.PriceIosToman : 0m;

        return new ModulePriceView(
            basePrice,
            usersPrice,
            companiesPrice,
            androidPrice,
            iosPrice,
            basePrice + usersPrice + companiesPrice + androidPrice + iosPrice,
            users,
            extraCompanies,
            selection.IncludeAndroid,
            selection.IncludeIos,
            includedUsers,
            includedCompanies);
    }

    public async Task<ModuleQuoteView> QuoteAsync(IReadOnlyList<ModuleSelection> selections, CancellationToken ct = default)
    {
        if (selections.Count == 0)
            return new ModuleQuoteView(Array.Empty<ModuleQuoteLine>(), 0, 0m, "IRT");

        if (selections.Count > 50)
            throw new BadRequestException("تعدادِ ماژول‌هایِ یک سفارش حداکثر ۵۰ است.", "too_many_modules");

        // تکراری‌ها یکی می‌شوند (آخرین پیکربندی برنده است)
        var distinct = selections
            .GroupBy(s => s.ProductId)
            .Select(g => g.Last())
            .ToList();

        var ids = distinct.Select(s => s.ProductId).ToList();
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync(ct);

        if (products.Count != ids.Count)
        {
            var missing = ids.First(id => products.All(p => p.Id != id));
            throw new NotFoundException("ماژول", missing);
        }

        var lines = new List<ModuleQuoteLine>(products.Count);
        foreach (var selection in distinct)
        {
            var product = products.First(p => p.Id == selection.ProductId);
            var category = product.Category
                ?? throw new BadRequestException("این ماژول به هیچ دسته‌ای وصل نیست.", "category_missing");

            var price = Calculate(product, category, selection);

            lines.Add(new ModuleQuoteLine(
                product.Id,
                product.Name,
                product.Slug,
                category.Id.ToString(),
                category.Name,
                category.Slug,
                product.AccentColor,
                price,
                Describe(price)));
        }

        return new ModuleQuoteView(lines, lines.Count, lines.Sum(l => l.Price.TotalToman), "IRT");
    }

    /// <summary>خلاصهٔ فارسیِ پیکربندی؛ در سبد و فاکتور همان متن دیده می‌شود.</summary>
    public static string Describe(ModulePriceView price)
    {
        var parts = new List<string>
        {
            price.Users <= price.IncludedUsers
                ? "نسخهٔ دسکتاپ، ۱ کاربر"
                : $"نسخهٔ دسکتاپ، {FaDigits.Of(price.Users)} کاربر"
        };

        if (price.ExtraCompanies > 0)
            parts.Add($"{FaDigits.Of(price.ExtraCompanies)} شرکتِ اضافه (هر شرکت {FaDigits.Of(price.Users)} کاربر)");

        if (price.IncludeAndroid) parts.Add("اندروید");
        if (price.IncludeIos) parts.Add("iOS");

        return string.Join(" • ", parts);
    }
}
