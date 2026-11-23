using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities.Support;

namespace Northwind.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> b)
    {
        b.ToTable("Tickets", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Reference).HasMaxLength(32).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(240).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.Environment).HasMaxLength(400);

        b.Property(x => x.Priority).HasConversion<int>();
        b.Property(x => x.Status).HasConversion<int>();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.LastActivityAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.User)
            .WithMany(u => u.Tickets)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.Reference).IsUnique().HasDatabaseName("UX_Tickets_Reference");
        b.HasIndex(x => new { x.UserId, x.LastActivityAt }).HasDatabaseName("IX_Tickets_User_Activity");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Tickets_Status");
    }
}

public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> b)
    {
        b.ToTable("TicketMessages", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.AuthorName).HasMaxLength(160).IsRequired();
        b.Property(x => x.Body).HasMaxLength(8000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Ticket)
            .WithMany(t => t.Messages)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.TicketId, x.CreatedAt }).HasDatabaseName("IX_TicketMessages_Ticket_Date");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Title).HasMaxLength(240).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000);
        b.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        b.Property(x => x.Link).HasMaxLength(400);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // صندوقِ اعلان‌ها همیشه با فیلترِ «خوانده‌نشده» خوانده می‌شود.
        b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_User_Read_Date");
    }
}
