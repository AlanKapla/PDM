using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateUnit
    /// </summary>
    public class CostEstimateTemplateUnitConfiguration : IEntityTypeConfiguration<CostEstimateTemplateUnit>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateUnit> builder)
        {
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.TemplateId)
                .IsRequired();
            
            builder.Property(u => u.Code)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.Property(u => u.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(u => u.Symbol)
                .IsRequired()
                .HasMaxLength(20);
            
            builder.Property(u => u.Category)
                .HasMaxLength(50);
            
            builder.Property(u => u.IsDefault)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(u => u.Order)
                .IsRequired();
            
            // Relationship with CostEstimateTemplate
            builder.HasOne(u => u.Template)
                .WithMany(t => t.Units)
                .HasForeignKey(u => u.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            builder.HasIndex(u => u.TemplateId);
            builder.HasIndex(u => new { u.TemplateId, u.Code }).IsUnique();
            builder.HasIndex(u => new { u.TemplateId, u.Category });
            builder.HasIndex(u => new { u.TemplateId, u.IsDefault });
        }
    }
}
