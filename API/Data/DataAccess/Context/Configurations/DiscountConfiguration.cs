using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
    {
        public void Configure(EntityTypeBuilder<Discount> builder)
        {
            builder.ToTable("Discounts", d=>d.HasCheckConstraint("CK_Discount_Percentage", "Percentage >= 0 AND Percentage <= 100"));

            // Properties
            builder.Property(d => d.Percentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(d => d.DaysRequired)
                .IsRequired();

            builder.Property(d => d.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships handled in ProductConfiguration

            // Indexes
            builder.HasIndex(d => d.ProductId)
                .IsUnique(); // Ensures 1:1 relationship

            builder.HasIndex(d => d.CreatedDate);
        }
    }
}