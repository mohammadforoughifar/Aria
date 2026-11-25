using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Data.Entities.Cart;
using Northwind.Data.Entities.Commerce;

namespace Northwind.Data.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.ToTable("CartItems", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.SessionKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.SessionKey).HasDatabaseName("IX_CartItems_Session");
        b.HasIndex(x => x.UserId).HasDatabaseName("IX_CartItems_User");
        // یک طرح نباید دو بار در یک سبد بیاید؛ کمّیت جدا شمرده می‌شود.
        b.HasIndex(x => new { x.SessionKey, x.PlanId, x.UserId })
            .IsUnique()
            .HasDatabaseName("UX_CartItems_Session_Plan_User");
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.OrderNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.Currency).HasMaxLength(8).IsRequired();
        b.Property(x => x.DiscountCode).HasMaxLength(64);

        b.Property(x => x.BillingName).HasMaxLength(200);
        b.Property(x => x.BillingCompany).HasMaxLength(200);
        b.Property(x => x.BillingEmail).HasMaxLength(256).IsRequired();
        b.Property(x => x.BillingCountry).HasMaxLength(80);
        b.Property(x => x.EconomicCode).HasMaxLength(32);
        b.Property(x => x.NationalId).HasMaxLength(32);
        b.Property(x => x.Notes).HasMaxLength(2000);

        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.AccountType).HasConversion<int>();

        b.Property(x => x.PlacedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);      // سفارشِ مالی با حذفِ کاربر از بین نمی‌رود

        b.HasIndex(x => x.OrderNumber).IsUnique().HasDatabaseName("UX_Orders_Number");
        b.HasIndex(x => new { x.UserId, x.PlacedAt }).HasDatabaseName("IX_Orders_User_Placed");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Orders_Status");
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("OrderItems", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
        b.Property(x => x.PlanName).HasMaxLength(120).IsRequired();
        b.Property(x => x.BillingType).HasConversion<int>();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.OrderId).HasDatabaseName("IX_OrderItems_Order");
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Provider).HasMaxLength(64).IsRequired();
        b.Property(x => x.Method).HasMaxLength(64).IsRequired();
        b.Property(x => x.TransactionRef).HasMaxLength(128);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.PaidAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Order)
            .WithOne(o => o.Payment)
            .HasForeignKey<Payment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("UX_Payments_Order");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_Payments_Status");
    }
}

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("Invoices", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.InvoiceNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.IssuedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.DueAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Order)
            .WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.InvoiceNumber).IsUnique().HasDatabaseName("UX_Invoices_Number");
        b.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("UX_Invoices_Order");
    }
}

public class LicenseConfiguration : IEntityTypeConfiguration<License>
{
    public void Configure(EntityTypeBuilder<License> b)
    {
        b.ToTable("Licenses", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.LicenseKey).HasMaxLength(64).IsRequired();
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.IssuedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.ExpiresAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.User)
            .WithMany(u => u.Licenses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.SetNull);       // حذفِ طرح، لایسنسِ صادرشده را باطل نمی‌کند

        b.HasIndex(x => x.LicenseKey).IsUnique().HasDatabaseName("UX_Licenses_Key");
        b.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("IX_Licenses_User_Status");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_Licenses_Expires");
    }
}

public class DownloadConfiguration : IEntityTypeConfiguration<Download>
{
    public void Configure(EntityTypeBuilder<Download> b)
    {
        b.ToTable("Downloads", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Version).HasMaxLength(32).IsRequired();
        b.Property(x => x.Kind).HasMaxLength(32).IsRequired();
        b.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.DownloadedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.UserId, x.DownloadedAt }).HasDatabaseName("IX_Downloads_User_Date");
        b.HasIndex(x => x.ProductId).HasDatabaseName("IX_Downloads_Product");
    }
}

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> b)
    {
        b.ToTable("DiscountCodes", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Description).HasMaxLength(400);
        b.Property(x => x.ExpiresAt).HasColumnType("datetime2(3)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");

        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_DiscountCodes_Code");
        b.HasIndex(x => new { x.IsActive, x.ExpiresAt }).HasDatabaseName("IX_DiscountCodes_Active");
    }
}
