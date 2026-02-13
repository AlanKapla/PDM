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
            
            builder.Property(c => c.TemplateId)
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
            
            // Relationship with CostEstimateTemplate
            builder.HasOne(c => c.Template)
                .WithMany(t => t.Currencies)
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            builder.HasIndex(c => c.TemplateId);
            builder.HasIndex(c => new { c.TemplateId, c.Code }).IsUnique();
            builder.HasIndex(c => new { c.TemplateId, c.IsDefault });
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
