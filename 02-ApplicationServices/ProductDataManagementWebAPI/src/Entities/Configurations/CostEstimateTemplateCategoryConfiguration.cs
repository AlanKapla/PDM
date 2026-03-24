using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateCategory
    /// </summary>
    public class CostEstimateTemplateCategoryConfiguration : IEntityTypeConfiguration<CostEstimateTemplateCategory>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateCategory> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.TemplateId)
                .IsRequired();

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Symbol)
                .HasMaxLength(50);

            builder.Property(c => c.Order)
                .IsRequired();

            // Relationship with CostEstimateTemplate
            builder.HasOne(c => c.Template)
                .WithMany(t => t.Categories)
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(c => c.TemplateId);
            builder.HasIndex(c => new { c.TemplateId, c.Name }).IsUnique();
        }
    }
}
