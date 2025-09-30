using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("Users");

            builder.Property(u => u.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            // user : product 1:m
            builder.HasMany(u => u.Products)
                .WithOne(p => p.Admin)
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            // user : order 1:m
            builder.HasMany(u => u.Orders)
                .WithOne(o => o.Seller)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // user : cart 1:1
            builder.HasOne(u => u.Cart)
                .WithOne(c => c.AppUser)
                .HasForeignKey<Cart>(c => c.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(u => u.CreatedDate);
        }
    }
}