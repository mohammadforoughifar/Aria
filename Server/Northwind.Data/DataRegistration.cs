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
                logger.LogWarning(
                    "رشتهٔ اتصالِ SQL Server تنظیم نشده — حالتِ نمایشی رویِ SQLite: {Path}",
                    ResolveSqlitePath(options));

                await db.Database.EnsureCreatedAsync(cancellationToken);
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

        if (!Path.IsPathRooted(path))
            path = Path.Combine(AppContext.BaseDirectory, path);

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        return path.Replace('\\', '/');
    }
}
