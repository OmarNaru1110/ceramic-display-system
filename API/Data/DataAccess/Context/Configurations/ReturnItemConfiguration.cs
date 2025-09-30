using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
    {
        public void Configure(EntityTypeBuilder<ReturnItem> builder)
        {
            builder.ToTable("ReturnItems");

            // Properties
            builder.Property(ri => ri.Quantity)
                .IsRequired();

            builder.Property(ri => ri.Reason)
                .IsRequired()
                .HasMaxLength(500);

            // Relationships handled in ReturnRequestConfiguration and ProductConfiguration

            // Indexes
            builder.HasIndex(ri => ri.ReturnRequestId);
            builder.HasIndex(ri => ri.ProductId);
        }
    }
}