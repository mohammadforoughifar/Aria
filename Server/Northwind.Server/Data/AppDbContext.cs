using Microsoft.EntityFrameworkCore;
using Northwind.Server.Data;

namespace Northwind.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ProductVersion> ProductVersions => Set<ProductVersion>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<License> Licenses => Set<License>();
    public DbSet<Download> Downloads => Set<Download>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketMessage> TicketMessages => Set<TicketMessage>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Testimonial> Testimonials => Set<Testimonial>();
    public DbSet<Faq> Faqs => Set<Faq>();
    public DbSet<SiteSettings> Settings => Set<SiteSettings>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DemoRequest> DemoRequests => Set<DemoRequest>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Identity ----
        b.Entity<User>().HasIndex(x => x.Email).IsUnique();
        b.Entity<User>().Property(x => x.Role).HasConversion<int>();
        b.Entity<User>().Property(x => x.AccountType).HasConversion<int>();

        // ---- Catalog ----
        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<Product>().Property(x => x.Type).HasConversion<int>();
        b.Entity<Product>().Property(x => x.Audience).HasConversion<int>();
        b.Entity<Product>()
            .HasOne(x => x.Category).WithMany(c => c.Products)
            .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Plan>().Property(x => x.Tier).HasConversion<int>();
        b.Entity<Plan>().Property(x => x.BillingType).HasConversion<int>();
        b.Entity<Plan>()
            .HasOne(x => x.Product).WithMany(p => p.Plans)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<ProductVersion>()
            .HasOne(x => x.Product).WithMany(p => p.Versions)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Review>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Review>()
            .HasOne(x => x.Product).WithMany(p => p.Reviews)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        // ---- Cart ----
        b.Entity<CartItem>().HasIndex(x => x.SessionKey);
        b.Entity<CartItem>()
            .HasOne(x => x.Plan).WithMany()
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Cascade);

        // ---- Commerce ----
        b.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();
        b.Entity<Order>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Order>().Property(x => x.AccountType).HasConversion<int>();
        b.Entity<Order>()
            .HasOne(x => x.User).WithMany(u => u.Orders)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<OrderItem>()
            .HasOne(x => x.Order).WithMany(o => o.Items)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Payment>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Payment>()
            .HasOne(x => x.Order).WithOne(o => o.Payment)
            .HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Invoice>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Invoice>().HasIndex(x => x.InvoiceNumber).IsUnique();
        b.Entity<Invoice>()
            .HasOne(x => x.Order).WithOne(o => o.Invoice)
            .HasForeignKey<Invoice>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<License>().HasIndex(x => x.LicenseKey).IsUnique();
        b.Entity<License>().Property(x => x.Status).HasConversion<int>();
        b.Entity<License>()
            .HasOne(x => x.User).WithMany(u => u.Licenses)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Download>()
            .HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<DiscountCode>().HasIndex(x => x.Code).IsUnique();

        // ---- Support ----
        b.Entity<Ticket>().HasIndex(x => x.Reference).IsUnique();
        b.Entity<Ticket>().Property(x => x.Priority).HasConversion<int>();
        b.Entity<Ticket>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Ticket>()
            .HasOne(x => x.User).WithMany(u => u.Tickets)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<TicketMessage>()
            .HasOne(x => x.Ticket).WithMany(t => t.Messages)
            .HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);

        // ---- Content ----
        b.Entity<Article>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<SiteSettings>().HasKey(x => x.Id);
        b.Entity<DemoRequest>().Property(x => x.Status).HasConversion<int>();
        b.Entity<Notification>()
            .HasOne(x => x.User).WithMany(u => u.Notifications)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
