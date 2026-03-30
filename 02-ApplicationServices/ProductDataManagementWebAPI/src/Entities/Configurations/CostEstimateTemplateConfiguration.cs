using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplate
    /// </summary>
    public class CostEstimateTemplateConfiguration : IEntityTypeConfiguration<CostEstimateTemplate>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplate> builder)
        {
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(t => t.Description)
                .HasMaxLength(1000);
            
            builder.Property(t => t.Category)
                .HasMaxLength(100);
            
            builder.Property(t => t.CanAddGroups)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(t => t.CanBranchGroups)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(t => t.MaxGroupLevel);
            
            builder.Property(t => t.AutoNumberGroups)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(t => t.GroupNumberFormat)
                .HasMaxLength(50);
            
            builder.Property(t => t.CreatedAt)
                .IsRequired();
            
            builder.Property(t => t.UpdatedAt);
            
            builder.Property(t => t.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(t => t.DeletedAt);
            
            // Relationship with User (Owner)
            builder.HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationships with field definitions, currencies and units
            builder.HasMany(t => t.Currencies)
                .WithOne(c => c.Template)
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.Units)
                .WithOne(u => u.Template)
                .HasForeignKey(u => u.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.GroupFieldDefinitions)
                .WithOne(f => f.Template)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.SystemFieldDefinitions)
                .WithOne(f => f.Template)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.CalculatedFieldDefinitions)
                .WithOne(f => f.Template)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasMany(t => t.GenericFieldDefinitions)
                .WithOne(f => f.Template)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for better query performance
            builder.HasIndex(t => t.OwnerId);
            builder.HasIndex(t => t.Category);
            builder.HasIndex(t => t.IsDeleted);
            builder.HasIndex(t => t.CreatedAt);
        }
    }
}
