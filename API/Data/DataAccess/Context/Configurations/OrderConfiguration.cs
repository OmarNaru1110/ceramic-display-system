using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            // Properties
            builder.Property(o => o.CustomerName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.CustomerContact)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.CustomerAddress)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(o => o.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(o => o.Notes)
                .HasMaxLength(1000);

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            // invoice : order 1:1
            builder.HasOne(o => o.Invoice)
                .WithOne(i => i.Order)
                .HasForeignKey<Invoice>(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // order : return_request 1:1
            builder.HasOne(o => o.ReturnRequest)
                .WithOne(rr => rr.Order)
                .HasForeignKey<ReturnRequest>(rr => rr.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // order : order_item 1:m
            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.OrderDate);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.SellerId);
        }
    }
}