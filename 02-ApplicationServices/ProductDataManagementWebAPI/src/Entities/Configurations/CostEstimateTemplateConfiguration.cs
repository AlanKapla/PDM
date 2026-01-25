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
            
            // Relationship with Versions
            builder.HasMany(t => t.Versions)
                .WithOne(v => v.Template)
                .HasForeignKey(v => v.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Index for better query performance
            builder.HasIndex(t => t.OwnerId);
            builder.HasIndex(t => t.IsDeleted);
            builder.HasIndex(t => t.CreatedAt);
            
            // Global query filter for soft delete
            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
