using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            // Properties
            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.QualityGrade)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(p => p.Category)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(p => p.Type)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(p => p.PricePerSqm)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            // product : size relationship (already handled in Size configuration)
            
            // product : discount 1:1
            builder.HasOne(p => p.Discount)
                .WithOne()
                .HasForeignKey<Discount>(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // product : product_image 1:m
            builder.HasMany(p => p.ProductImages)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // product : cart_item 1:m
            builder.HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // product : order_item 1:m
            builder.HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // product : return_item 1:m
            builder.HasMany(p => p.ReturnItems)
                .WithOne(ri => ri.Product)
                .HasForeignKey(ri => ri.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(p => p.Name);
            builder.HasIndex(p => p.Category);
            builder.HasIndex(p => p.Type);
            builder.HasIndex(p => p.QualityGrade);
            builder.HasIndex(p => p.CreatedDate);
            builder.HasIndex(p => p.AdminId);
        }
    }
}