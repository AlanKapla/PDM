using Entities.Models.Costs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Costs
{
    public class BaseCostAttachmentConfiguration : IEntityTypeConfiguration<BaseCostAttachment>
    {
        public void Configure(EntityTypeBuilder<BaseCostAttachment> builder)
        {
            builder.ToTable("CostAttachments");

            builder.HasKey(a => a.Id);

            builder.Property(a => a.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(a => a.Cost)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.CostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(a => a.CostId).IsRequired();
            builder.Property(a => a.TenantId).IsRequired();
            builder.Property(a => a.ProjectId).IsRequired();

            builder.Property(a => a.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.BlobName)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(a => a.ContentType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(a => a.FileSize).IsRequired();
            builder.Property(a => a.CreatedAt).IsRequired();

            builder.Property(a => a.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(a => a.DeletedAt);

            builder.HasIndex(a => a.CostId);
            builder.HasIndex(a => new { a.TenantId, a.ProjectId });

            builder.HasQueryFilter(a => !a.IsDeleted);
        }
    }
}
