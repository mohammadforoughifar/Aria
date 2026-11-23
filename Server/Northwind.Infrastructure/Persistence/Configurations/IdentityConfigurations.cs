using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities.Identity;

namespace Northwind.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users", "dbo");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();   // شناسه سمتِ برنامه ساخته می‌شود

        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(x => x.Company).HasMaxLength(200);
        b.Property(x => x.Country).HasMaxLength(80);
        b.Property(x => x.EconomicCode).HasMaxLength(32);
        b.Property(x => x.NationalId).HasMaxLength(32);

        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.AccountType).HasConversion<int>();

        b.Property(x => x.CreatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(3)");
        b.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");

        b.HasIndex(x => x.Email).IsUnique().HasDatabaseName("UX_Users_Email");
        b.HasIndex(x => new { x.Role, x.Locked }).HasDatabaseName("IX_Users_Role_Locked");
    }
}
