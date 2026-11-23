using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities.Content;

namespace Northwind.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> b)
    {
        b.ToTable("Articles", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Slug).HasMaxLength(160).IsRequired();
        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.Excerpt).HasMaxLength(600).IsRequired();
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80);
        b.Property(x => x.PublishedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("UX_Articles_Slug");
        b.HasIndex(x => new { x.IsPublished, x.PublishedAt }).HasDatabaseName("IX_Articles_Published_Date");
    }
}

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> b)
    {
        b.ToTable("Testimonials", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.AuthorName).HasMaxLength(160).IsRequired();
        b.Property(x => x.AuthorRole).HasMaxLength(160).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200).IsRequired();
        b.Property(x => x.Quote).HasMaxLength(1000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
    }
}

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> b)
    {
        b.ToTable("Faqs", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Question).HasMaxLength(400).IsRequired();
        b.Property(x => x.Answer).HasMaxLength(4000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasIndex(x => x.SortOrder).HasDatabaseName("IX_Faqs_SortOrder");
    }
}

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.ToTable("SiteSettings", "dbo");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();    // رکوردِ تکی، شناسه همیشه ۱

        b.Property(x => x.SiteName).HasMaxLength(160).IsRequired();
        b.Property(x => x.SupportEmail).HasMaxLength(256).IsRequired();
        b.Property(x => x.SalesEmail).HasMaxLength(256).IsRequired();
        b.Property(x => x.SupportPhone).HasMaxLength(32).IsRequired();
        b.Property(x => x.HeroHeadline).HasMaxLength(240).IsRequired();
        b.Property(x => x.HeroSubheading).HasMaxLength(600).IsRequired();
        b.Property(x => x.Announcement).HasMaxLength(400);
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
    }
}

public class DemoRequestConfiguration : IEntityTypeConfiguration<DemoRequest>
{
    public void Configure(EntityTypeBuilder<DemoRequest> b)
    {
        b.ToTable("DemoRequests", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200);
        b.Property(x => x.CompanySize).HasMaxLength(40);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.Message).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("IX_DemoRequests_Status_Date");
    }
}

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> b)
    {
        b.ToTable("ContactMessages", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200);
        b.Property(x => x.Topic).HasMaxLength(80).IsRequired();
        b.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasIndex(x => new { x.IsRead, x.CreatedAt }).HasDatabaseName("IX_ContactMessages_Read_Date");
    }
}
