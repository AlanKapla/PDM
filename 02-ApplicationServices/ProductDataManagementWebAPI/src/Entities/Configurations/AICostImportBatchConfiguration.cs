using Entities.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class AICostImportBatchConfiguration : IEntityTypeConfiguration<AICostImportBatch>
    {
        public void Configure(EntityTypeBuilder<AICostImportBatch> builder)
        {
            builder.ToTable("AICostImportBatches");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.TrackedCostContextJson)
                .HasMaxLength(2000);

            builder.Property(b => b.CreatedAt).IsRequired();

            builder.HasIndex(b => new { b.TenantId, b.ProjectId });
            builder.HasIndex(b => new { b.TenantId, b.ProjectId, b.Status });

            builder.HasMany(b => b.Items)
                .WithOne(i => i.Batch)
                .HasForeignKey(i => i.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
