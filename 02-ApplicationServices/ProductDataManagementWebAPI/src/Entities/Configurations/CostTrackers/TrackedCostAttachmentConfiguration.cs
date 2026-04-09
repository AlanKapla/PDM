using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.CostTrackers
{
    public class TrackedCostAttachmentConfiguration : IEntityTypeConfiguration<TrackedCostAttachment>
    {
        public void Configure(EntityTypeBuilder<TrackedCostAttachment> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(a => a.TrackedCostId)
                .IsRequired();

            builder.Property(a => a.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.BlobName)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(a => a.ContentType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.FileSize)
                .IsRequired();

            builder.Property(a => a.CreatedAt)
                .IsRequired();

            builder.Property(a => a.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.DeletedAt);

            builder.HasOne(a => a.TrackedCost)
                .WithMany(tc => tc.Attachments)
                .HasForeignKey(a => a.TrackedCostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.TrackedCostId);
            builder.HasIndex(a => a.IsDeleted);

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
