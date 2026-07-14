using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateItemFile
    /// Zastępuje starą konfigurację CostEstimateFieldFileConfiguration
    /// </summary>
    public class CostEstimateItemFileConfiguration : IEntityTypeConfiguration<CostEstimateItemFile>
    {
        public void Configure(EntityTypeBuilder<CostEstimateItemFile> builder)
        {
            builder.HasKey(f => f.Id);

            builder.Property(f => f.ItemId)
                .IsRequired();

            builder.Property(f => f.CostEstimateId)
                .IsRequired();

            builder.Property(f => f.OriginalFileName)
                .IsRequired()
                .HasMaxLength(512);

            builder.Property(f => f.BlobName)
                .IsRequired()
                .HasMaxLength(1024);

            builder.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(f => f.FileSize)
                .IsRequired();

            builder.Property(f => f.Order)
                .IsRequired();

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.Property(f => f.CreatedByUserId)
                .IsRequired();

            builder.Property(f => f.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(f => f.DeletedAt);

            // Relationship with Item
            builder.HasOne(f => f.Item)
                .WithMany(i => i.Files)
                .HasForeignKey(f => f.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with CostEstimate (denormalization)
            builder.HasOne(f => f.CostEstimate)
                .WithMany()
                .HasForeignKey(f => f.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with User
            builder.HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(f => f.ItemId);
            builder.HasIndex(f => f.CostEstimateId);
            builder.HasIndex(f => new { f.ItemId, f.CostEstimateId });
            builder.HasIndex(f => f.IsDeleted);

            // Global query filter for soft delete
            builder.HasQueryFilter(f => !f.IsDeleted);
        }
    }
}
