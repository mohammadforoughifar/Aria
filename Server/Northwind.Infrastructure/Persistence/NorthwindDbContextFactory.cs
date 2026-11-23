using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Northwind.Infrastructure.Persistence;

/// <summary>
/// فقط برایِ ابزارِ <c>dotnet ef</c>.
///
/// <para>
/// مهاجرت‌ها همیشه با موتورِ SQL Server ساخته می‌شوند — حتی اگر در محیطِ توسعه
/// SQL Server بالا نباشد — چون فایلِ مهاجرت یک متنِ ثابت است و برایِ اجرا به
/// اتصالِ زنده نیازی ندارد.
/// </para>
/// </summary>
public class NorthwindDbContextFactory : IDesignTimeDbContextFactory<NorthwindDbContext>
{
    public NorthwindDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("NORTHWIND_CONNECTION")
            ?? "Server=localhost,1433;Database=NorthwindStorefront;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

        var builder = new DbContextOptionsBuilder<NorthwindDbContext>()
            .UseSqlServer(connection, sql =>
            {
                sql.MigrationsAssembly(typeof(NorthwindDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            });

        return new NorthwindDbContext(builder.Options);
    }
}
