using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateItemSystemFieldDefinition
    /// </summary>
    public class CostEstimateTemplateItemSystemFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateItemSystemFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateItemSystemFieldDefinition> builder)
        {
            // Relationship with Template
            builder.HasOne(f => f.Template)
                .WithMany(t => t.SystemFieldDefinitions)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateItemCalculatedFieldDefinition
    /// </summary>
    public class CostEstimateTemplateItemCalculatedFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateItemCalculatedFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateItemCalculatedFieldDefinition> builder)
        {
            builder.Property(f => f.SumInGroup)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(f => f.SumInTotal)
                .IsRequired()
                .HasDefaultValue(false);
            
            // Relationship with Template
            builder.HasOne(f => f.Template)
                .WithMany(t => t.CalculatedFieldDefinitions)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(f => new { f.TemplateId, f.FieldType })
                .HasDatabaseName("IX_CalculatedFieldDefinition_TemplateId_FieldType");
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateItemGenericFieldDefinition
    /// </summary>
    public class CostEstimateTemplateItemGenericFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateItemGenericFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateItemGenericFieldDefinition> builder)
        {          
            // Relationship with Template
            builder.HasOne(f => f.Template)
                .WithMany(t => t.GenericFieldDefinitions)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(f => new { f.TemplateId, f.FieldType })
                .HasDatabaseName("IX_GenericFieldDefinition_TemplateId_FieldType");
        }
    }
}
