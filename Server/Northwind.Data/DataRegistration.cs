using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Northwind.Data.Persistence;

namespace Northwind.Data;

public static class DataRegistration
{
    /// <summary>تنظیماتِ اتصال را از <c>Database</c> (یا <c>ConnectionStrings:Northwind</c>) می‌خواند.</summary>
    public static DatabaseOptions ReadDatabaseOptions(this IConfiguration configuration)
    {
        var options = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        // اگر بخشِ Database نبود، همان رشتهٔ اتصالِ متعارفِ .NET پذیرفته می‌شود.
        if (!options.HasSqlServer)
            options.ConnectionString = configuration.GetConnectionString("Northwind");

        return options;
    }

    /// <summary>
    /// ثبتِ زمینهٔ داده.
    ///
    /// <para>
    /// اولویت با SQL Server است. اگر رشتهٔ اتصال تنظیم نشده باشد و
    /// <see cref="DatabaseOptions.UseSqliteFallback"/> روشن باشد، برنامه رویِ SQLite
    /// بالا می‌آید تا سایت بدونِ پایگاه‌داده هم قابلِ دیدن باشد.
    /// </para>
    /// </summary>
    public static IServiceCollection AddNorthwindData(
        this IServiceCollection services,
        IConfiguration configuration,
        DatabaseOptions? options = null,
        Action<DatabaseOptions>? configure = null)
    {
        options ??= configuration.ReadDatabaseOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        services.AddDbContext<NorthwindDbContext>(db =>
        {
            if (options.Resolve() == DatabaseProvider.SqlServer)
            {
                db.UseSqlServer(options.ConnectionString, sql =>
                {
                    sql.MigrationsAssembly(typeof(NorthwindDbContext).Assembly.FullName);
                    sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(options.ConnectRetrySeconds),
                        errorNumbersToAdd: null);
                    sql.CommandTimeout(30);
                });
            }
            else
            {
                db.UseSqlite($"Data Source={ResolveSqlitePath(options)}");
            }
        });

        return services;
    }

    /// <summary>
    /// آماده‌سازیِ پایگاه‌داده هنگامِ شروع:
    /// رویِ SQL Server مهاجرت‌ها اعمال می‌شوند، رویِ SQLite طرح از رویِ مدل ساخته می‌شود.
    /// </summary>
    public static async Task<bool> InitializeNorthwindDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Northwind.Database");
        var options = scope.ServiceProvider.GetRequiredService<DatabaseOptions>();
        var db = scope.ServiceProvider.GetRequiredService<NorthwindDbContext>();

        try
        {
            if (db.Database.IsSqlServer())
            {
                if (options.ApplyMigrationsOnStartup)
                {
                    logger.LogInformation("اعمالِ مهاجرت‌هایِ SQL Server…");
                    await db.Database.MigrateAsync(cancellationToken);
                }
            }
            else
            {
                var path = ResolveSqlitePath(options);
                var existed = File.Exists(path);

                logger.LogWarning(
                    "رشتهٔ اتصالِ SQL Server تنظیم نشده — حالتِ نمایشی رویِ SQLite: {Path}", path);
                logger.LogInformation(
                    "فایلِ پایگاه‌داده {State}؛ این همان فایلی است که بینِ اجراهایِ برنامه می‌مانَد.",
                    existed ? "از پیش موجود بود" : "نبود و همین حالا ساخته می‌شود");

                await db.Database.EnsureCreatedAsync(cancellationToken);

                // شمارشِ رکوردها بهترین نشانه است که داده‌ها از همان فایل خوانده
                // می‌شوند؛ اگر پس از راه‌اندازی دوباره عددِ صفر دیدید، یعنی مسیرِ
                // فایل عوض شده است.
                var products = await db.Products.CountAsync(cancellationToken);
                logger.LogInformation("پایگاه‌داده آماده است — {Count} محصول از فایل خوانده شد.", products);
            }

            return true;
        }
        catch (Exception ex)
        {
            // پایگاه‌داده نباید سایت را از کار بیندازد؛ خطا ثبت و ادامه داده می‌شود.
            logger.LogError(ex, "آماده‌سازیِ پایگاه‌داده شکست خورد");
            return false;
        }
    }

    public static string ResolveSqlitePath(DatabaseOptions options)
    {
        var path = options.SqlitePath;

        // مسیرِ نسبی هرگز نباید داخلِ پوشهٔ ساخت (bin/obj/publish) بیفتد؛ وگرنه
        // با Clean/Rebuild یا انتشارِ تازه، فایلِ پایگاه‌داده پاک می‌شود و همهٔ
        // دادهٔ کاربر ناپدید به نظر می‌رسد.
        if (!Path.IsPathRooted(path))
            path = Path.Combine(StableRoot(), path);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        return path.Replace('\\', '/');
    }

    /// <summary>
    /// ریشه‌ای پایدار برایِ فایل‌هایِ داده: از مسیرِ اجرایِ فعلی به بالا می‌رود تا
    /// از پوشه‌هایِ ساخت (bin، obj، publish، Debug، Release، net9.0) بیرون بزند.
    /// </summary>
    public static string StableRoot()
    {
        var dir = FindStable(new DirectoryInfo(AppContext.BaseDirectory));
        return dir?.FullName ?? AppContext.BaseDirectory;
    }

    private static DirectoryInfo? FindStable(DirectoryInfo? dir)
    {
        while (dir is not null && IsBuildDirectory(dir.Name))
            dir = dir.Parent;

        return dir;
    }

    private static bool IsBuildDirectory(string name) =>
        name.Equals("bin", StringComparison.OrdinalIgnoreCase)
        || name.Equals("obj", StringComparison.OrdinalIgnoreCase)
        || name.Equals("publish", StringComparison.OrdinalIgnoreCase)
        || name.Equals("debug", StringComparison.OrdinalIgnoreCase)
        || name.Equals("release", StringComparison.OrdinalIgnoreCase)
        || (name.StartsWith("net", StringComparison.OrdinalIgnoreCase)
            && name.Length > 3
            && char.IsDigit(name[3]));
}
