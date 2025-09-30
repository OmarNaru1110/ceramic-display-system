using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            // Properties
            builder.Property(oi => oi.Quantity)
                .IsRequired();

            builder.Property(oi => oi.PricePerSqm)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(oi => oi.TotalPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // Relationships handled in OrderConfiguration and ProductConfiguration

            // Indexes
            builder.HasIndex(oi => oi.OrderId);
            builder.HasIndex(oi => oi.ProductId);
        }
    }
}