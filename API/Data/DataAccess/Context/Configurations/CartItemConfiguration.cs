using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
    {
        public void Configure(EntityTypeBuilder<CartItem> builder)
        {
            builder.ToTable("CartItems");

            // Properties
            builder.Property(ci => ci.Quantity)
                .IsRequired();

            builder.Property(ci => ci.AddedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships handled in CartConfiguration and ProductConfiguration

            // Indexes
            builder.HasIndex(ci => ci.CartId);
            builder.HasIndex(ci => ci.ProductId);
            builder.HasIndex(ci => ci.AddedDate);
        }
    }
}