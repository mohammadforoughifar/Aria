namespace Northwind.Infrastructure.Persistence;

/// <summary>موتورِ پایگاه‌دادهٔ فعال.</summary>
public enum DatabaseProvider
{
    /// <summary>SQL Server — حالتِ اصلی و تولیدی.</summary>
    SqlServer,

    /// <summary>SQLite — فقط برایِ دیدنِ سایت وقتی SQL Server در دسترس نیست.</summary>
    Sqlite
}

/// <summary>تنظیماتِ اتصال که از <c>appsettings</c> خوانده می‌شود.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>رشتهٔ اتصالِ SQL Server. اگر خالی باشد، فالبکِ SQLite فعال می‌شود.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>اجازهٔ فالبک به SQLite وقتی رشتهٔ اتصال نبود.</summary>
    public bool UseSqliteFallback { get; set; } = true;

    /// <summary>مسیرِ فایلِ SQLite در حالتِ فالبک.</summary>
    public string SqlitePath { get; set; } = "App_Data/northwind-preview.db";

    /// <summary>اعمالِ مهاجرت‌ها هنگامِ شروع — فقط برایِ SQL Server.</summary>
    public bool ApplyMigrationsOnStartup { get; set; } = true;

    /// <summary>پرکردنِ دادهٔ نمایشی در شروع.</summary>
    public bool SeedDemoData { get; set; } = true;

    /// <summary>آستانهٔ تلاشِ دوباره هنگامِ اتصال به SQL Server.</summary>
    public int ConnectRetrySeconds { get; set; } = 3;

    public bool HasSqlServer => !string.IsNullOrWhiteSpace(ConnectionString);

    public DatabaseProvider Resolve()
        => HasSqlServer ? DatabaseProvider.SqlServer : DatabaseProvider.Sqlite;
}
