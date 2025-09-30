using Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DATA.DataAccess.Context.Configurations
{
    public class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
    {
        public void Configure(EntityTypeBuilder<ReturnRequest> builder)
        {
            builder.ToTable("ReturnRequests");

            // Properties
            builder.Property(rr => rr.ReturnReason)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(rr => rr.Description)
                .HasMaxLength(1000);

            builder.Property(rr => rr.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.Property(rr => rr.RequestDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rr => rr.AdminNotes)
                .HasMaxLength(1000);

            // Relationships
            // return_request : return_item 1:m
            builder.HasMany(rr => rr.ReturnItems)
                .WithOne(ri => ri.ReturnRequest)
                .HasForeignKey(ri => ri.ReturnRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(rr => rr.RequestDate);
            builder.HasIndex(rr => rr.Status);
            builder.HasIndex(rr => rr.OrderId);
        }
    }
}