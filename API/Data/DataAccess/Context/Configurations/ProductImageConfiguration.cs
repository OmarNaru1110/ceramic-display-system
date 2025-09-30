using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.ToTable("ProductImages");

            // Properties
            builder.Property(pi => pi.ImageURL)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(pi => pi.AltText)
                .HasMaxLength(200);

            builder.Property(pi => pi.DisplayOrder)
                .HasDefaultValue(0);

            builder.Property(pi => pi.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships handled in ProductConfiguration

            // Indexes
            builder.HasIndex(pi => pi.ProductId);
            builder.HasIndex(pi => pi.DisplayOrder);
        }
    }
}