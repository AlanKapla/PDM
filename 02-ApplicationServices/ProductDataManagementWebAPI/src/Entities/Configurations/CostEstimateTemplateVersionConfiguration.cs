using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateVersion
    /// </summary>
    public class CostEstimateTemplateVersionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateVersion>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateVersion> builder)
        {
            builder.HasKey(v => v.Id);
            
            builder.Property(v => v.VersionNumber)
                .IsRequired();
            
            builder.Property(v => v.VersionName)
                .HasMaxLength(200);
            
            builder.Property(v => v.ChangeDescription)
                .HasMaxLength(2000);
            
            builder.Property(v => v.Status)
                .IsRequired()
                .HasConversion<string>();
            
            builder.Property(v => v.Category)
                .HasMaxLength(100);
            
            builder.Property(v => v.CanAddGroups)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(v => v.CanBranchGroups)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(v => v.MaxGroupLevel);
            
            builder.Property(v => v.AutoNumberGroups)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(v => v.GroupNumberFormat)
                .HasMaxLength(50);
            
            builder.Property(v => v.CreatedAt)
                .IsRequired();
            
            builder.Property(v => v.ApprovedAt);
            
            builder.Property(v => v.ApprovedById);
            
            builder.Property(v => v.DeprecatedAt);
            
            builder.Property(v => v.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(v => v.DeletedAt);
            
            // Relationship with Template
            builder.HasOne(v => v.Template)
                .WithMany(t => t.Versions)
                .HasForeignKey(v => v.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with ApprovedBy User
            builder.HasOne(v => v.ApprovedBy)
                .WithMany()
                .HasForeignKey(v => v.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with GroupFieldDefinitions
            builder.HasMany(v => v.GroupFieldDefinitions)
                .WithOne(f => f.TemplateVersion)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with SystemFieldDefinitions
            builder.HasMany(v => v.SystemFieldDefinitions)
                .WithOne(f => f.TemplateVersion)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with CalculatedFieldDefinitions
            builder.HasMany(v => v.CalculatedFieldDefinitions)
                .WithOne(f => f.TemplateVersion)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with GenericFieldDefinitions
            builder.HasMany(v => v.GenericFieldDefinitions)
                .WithOne(f => f.TemplateVersion)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Unique constraint: One version number per template (excluding soft-deleted)
            builder.HasIndex(v => new { v.TemplateId, v.VersionNumber })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
            
            // Indexes for better query performance
            builder.HasIndex(v => v.TemplateId);
            builder.HasIndex(v => v.Status);
            builder.HasIndex(v => v.ApprovedById);
            builder.HasIndex(v => v.Category);
            builder.HasIndex(v => v.CreatedAt);
            builder.HasIndex(v => v.IsDeleted);
            
            // Global query filter for soft delete
            builder.HasQueryFilter(v => !v.IsDeleted);
        }
    }
}
