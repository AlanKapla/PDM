using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateCurrency
    /// </summary>
    public class CostEstimateTemplateCurrencyConfiguration : IEntityTypeConfiguration<CostEstimateTemplateCurrency>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateCurrency> builder)
        {
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.TemplateVersionId)
                .IsRequired();
            
            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(10);
            
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(c => c.Symbol)
                .HasMaxLength(10);
            
            builder.Property(c => c.IsDefault)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(c => c.Order)
                .IsRequired();
            
            // Relationship with CostEstimateTemplateVersion
            builder.HasOne(c => c.TemplateVersion)
                .WithMany(t => t.Currencies)
                .HasForeignKey(c => c.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            builder.HasIndex(c => c.TemplateVersionId);
            builder.HasIndex(c => new { c.TemplateVersionId, c.Code }).IsUnique();
            builder.HasIndex(c => new { c.TemplateVersionId, c.IsDefault });
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateUnit
    /// </summary>
    public class CostEstimateTemplateUnitConfiguration : IEntityTypeConfiguration<CostEstimateTemplateUnit>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateUnit> builder)
        {
            builder.HasKey(u => u.Id);
            
            builder.Property(u => u.TemplateVersionId)
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
            
            // Relationship with CostEstimateTemplateVersion
            builder.HasOne(u => u.TemplateVersion)
                .WithMany(t => t.Units)
                .HasForeignKey(u => u.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            builder.HasIndex(u => u.TemplateVersionId);
            builder.HasIndex(u => new { u.TemplateVersionId, u.Code }).IsUnique();
            builder.HasIndex(u => new { u.TemplateVersionId, u.Category });
            builder.HasIndex(u => new { u.TemplateVersionId, u.IsDefault });
        }
    }
}
