using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Northwind.Data.Common;
using Northwind.Data.Entities.Cart;
using Northwind.Data.Entities.Catalog;
using Northwind.Data.Entities.Commerce;
using Northwind.Data.Entities.Content;
using Northwind.Data.Entities.Identity;
using Northwind.Data.Entities.Support;

namespace Northwind.Data.Persistence;

/// <summary>
/// زمینهٔ دادهٔ سامانه.
///
/// <para>
/// نگاشت‌ها در کلاس‌هایِ <c>Configurations</c> جدا شده‌اند (الگویِ
/// <c>IEntityTypeConfiguration</c>) تا این کلاس فقط «کاتالوگِ موجودیت‌ها» باشد.
/// </para>
/// </summary>
public class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options) { }

    /* ---- Identity ---- */
    public DbSet<User> Users => Set<User>();

    /* ---- Catalog ---- */
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();
    public DbSet<Review> Reviews => Set<Review>();

    /* ---- Cart ---- */
    public DbSet<CartItem> CartItems => Set<CartItem>();

    /* ---- Commerce ---- */
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Download> Downloads => Set<Download>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    /* ---- Support ---- */
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();

    /* ---- Content ---- */
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // همهٔ پیکربندی‌هایِ IEntityTypeConfiguration در همین اسمبلی.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NorthwindDbContext).Assembly);

        var isSqlServer = Database.IsSqlServer();

        // همهٔ ستون‌هایِ decimal در این مدل مبلغِ پولی‌اند. یک قاعده برایِ همه:
        // decimal(18,2) یعنی تا ۱۶ رقمِ صحیح — حدودِ ۱۰^۱۶ تومان، که از سقفِ int
        // (۲٫۱ میلیارد) بسیار فراتر است و برایِ مبالغِ سنگینِ سازمانی کافی است.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entityType.GetProperties())
            {
                if (prop.ClrType != typeof(decimal)) continue;
                prop.SetPrecision(18);
                prop.SetScale(2);
            }
        }

        // SQLite نه شِما دارد و نه نوع‌هایِ datetime2؛ پس هرچه مخصوصِ SQL Server است
        // در حالتِ فالبک کنار گذاشته می‌شود تا همان مدل با نوع‌هایِ خودِ SQLite ساخته شود.
        if (!isSqlServer) ResetProviderSpecificMappings(modelBuilder);

        // توکنِ هم‌زمانی فقط رویِ SQL Server معنا دارد؛ SQLite نوعِ rowversion ندارد.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(EntityBase).IsAssignableFrom(entityType.ClrType)) continue;

            var prop = entityType.FindProperty(nameof(EntityBase.RowVersion));
            if (prop is null) continue;

            if (isSqlServer)
            {
                // همین ترکیب است که ستون را به rowversion تبدیل می‌کند.
                prop.IsConcurrencyToken = true;
                prop.ValueGenerated = ValueGenerated.OnAddOrUpdate;
            }
            else
            {
                // رویِ SQLite همان ستون یک BLOB ساده می‌شود و هیچ بررسی‌ای نمی‌کند.
                prop.IsConcurrencyToken = false;
                prop.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    private static void ResetProviderSpecificMappings(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            entityType.SetSchema(null);

            foreach (var prop in entityType.GetProperties())
            {
                if (prop.GetColumnType() is not null)
                    prop.SetColumnType(null);
                prop.SetPrecision(null);
                prop.SetScale(null);

                // SQLite نوعِ decimal را TEXT ذخیره می‌کند و آن‌وقت «مرتب‌سازی بر اساسِ
                // قیمت» و جمع‌زدنِ مبالغ کار نمی‌کند. در حالتِ نمایشی به double نگاشت
                // می‌شود که هم مرتب‌شدنی است هم جمع‌شدنی.
                if (prop.ClrType == typeof(decimal))
                    prop.SetProviderClrType(typeof(double));
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// زمان‌ها را سمتِ سرور تعیین می‌کند تا کلاینت نتواند <c>CreatedAt</c> را جعل کند.
    /// </summary>
    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is EntityBase e)
            {
                if (entry.State == EntityState.Added)
                {
                    if (e.CreatedAt == default) e.CreatedAt = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    e.UpdatedAt = now;
                    entry.Property(nameof(EntityBase.CreatedAt)).IsModified = false;
                }
            }
            else if (entry.Entity is SiteSettings settings && entry.State == EntityState.Modified)
            {
                settings.UpdatedAt = now;
            }
        }
    }
}
