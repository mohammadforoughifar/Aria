using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Data.Entities.Catalog;

namespace Northwind.Data.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.Property(x => x.Description).HasMaxLength(600);
        b.Property(x => x.Icon).HasMaxLength(64).IsRequired();

        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_Categories_Slug");
        b.HasIndex(x => x.SortOrder).HasDatabaseName("IX_Categories_SortOrder");
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Tagline).HasMaxLength(240).IsRequired();
        b.Property(x => x.Description).IsRequired();                  // nvarchar(max)
        b.Property(x => x.Platform).HasMaxLength(40).IsRequired();
        b.Property(x => x.Version).HasMaxLength(32).IsRequired();
        b.Property(x => x.AccentColor).HasMaxLength(16).IsRequired();
        b.Property(x => x.HeroImage).HasMaxLength(400);
        b.Property(x => x.DemoVideoUrl).HasMaxLength(400);

        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Audience).HasConversion<int>();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");

        // لیست‌هایِ رشته‌ای در EF Core 9 به‌صورتِ ستونِ JSON ذخیره می‌شوند
        // (رویِ SQL Server با توابعِ OPENJSON قابلِ پرس‌وجو هستند).
        b.PrimitiveCollection(x => x.Features);
        b.PrimitiveCollection(x => x.Requirements);
        b.PrimitiveCollection(x => x.Gallery);

        b.HasOne(x => x.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);           // دستهٔ دارایِ محصول حذف نمی‌شود

        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_Products_Slug");
        b.HasIndex(x => new { x.CategoryId, x.IsActive }).HasDatabaseName("IX_Products_Category_Active");
        b.HasIndex(x => new { x.IsFeatured, x.Popularity }).HasDatabaseName("IX_Products_Featured_Popularity");
    }
}

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("Plans", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Description).HasMaxLength(600);
        b.Property(x => x.Tier).HasConversion<int>();
        b.Property(x => x.BillingType).HasConversion<int>();

        b.PrimitiveCollection(x => x.Features);

        b.HasOne(x => x.Product)
            .WithMany(p => p.Plans)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ProductId, x.SortOrder }).HasDatabaseName("IX_Plans_Product_Sort");
    }
}

public class ProductVersionConfiguration : IEntityTypeConfiguration<ProductVersion>
{
    public void Configure(EntityTypeBuilder<ProductVersion> b)
    {
        b.ToTable("ProductVersions", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Version).HasMaxLength(32).IsRequired();
        b.Property(x => x.Notes).IsRequired();
        b.Property(x => x.InstallerUrl).HasMaxLength(400);
        b.Property(x => x.Platform).HasMaxLength(40).IsRequired();
        b.Property(x => x.ReleasedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Product)
            .WithMany(p => p.Versions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ProductId, x.ReleasedAt }).HasDatabaseName("IX_ProductVersions_Product_Released");
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.ToTable("Reviews", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.AuthorName).HasMaxLength(160).IsRequired();
        b.Property(x => x.AuthorCompany).HasMaxLength(200);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ProductId, x.Status }).HasDatabaseName("IX_Reviews_Product_Status");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Reviews_Status");     // صفِ بررسیِ مدیر
    }
}
