using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.ToTable("Invoices");

            // Properties
            builder.Property(i => i.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(i => i.InvoiceDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(i => i.PdfUrl)
                .HasMaxLength(500);

            // Relationships handled in OrderConfiguration

            // Indexes
            builder.HasIndex(i => i.InvoiceDate);
            builder.HasIndex(i => i.OrderId);
        }
    }
}