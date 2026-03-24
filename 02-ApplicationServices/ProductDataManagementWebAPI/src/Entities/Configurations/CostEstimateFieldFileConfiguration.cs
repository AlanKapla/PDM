using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class CostEstimateFieldFileConfiguration : IEntityTypeConfiguration<CostEstimateFieldFile>
    {
        public void Configure(EntityTypeBuilder<CostEstimateFieldFile> builder)
        {
            builder.HasKey(f => f.Id);
            
            builder.Property(f => f.FieldValueId)
                .IsRequired();
            
            builder.Property(f => f.CostEstimateId)
                .IsRequired();
            
            builder.Property(f => f.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);
            
            builder.Property(f => f.BlobName)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(100);
            
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
            
            builder.HasOne(f => f.FieldValue)
                .WithMany(fv => fv.Files)
                .HasForeignKey(f => f.FieldValueId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasOne(f => f.CostEstimate)
                .WithMany()
                .HasForeignKey(f => f.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(f => f.CreatedByUser)
                .WithMany()
                .HasForeignKey(f => f.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasIndex(f => f.FieldValueId);
            builder.HasIndex(f => f.CostEstimateId);
            builder.HasIndex(f => f.IsDeleted);
            builder.HasIndex(f => new { f.CostEstimateId, f.IsDeleted });
            
            builder.HasQueryFilter(f => !f.IsDeleted);
        }
    }
}
