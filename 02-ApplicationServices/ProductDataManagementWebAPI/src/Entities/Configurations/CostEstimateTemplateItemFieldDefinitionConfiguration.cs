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
            // ❌ NIE ustawiaj HasKey - jest w base class!
            // ❌ NIE ustawiaj indeksów z base class - są dziedziczone!
            
            // Relationship with TemplateVersion
            builder.HasOne(f => f.TemplateVersion)
                .WithMany(v => v.SystemFieldDefinitions)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // ❌ USUNIĘTO Unique constraint - TPH (Table Per Hierarchy) nie pozwala na unique per-derived-type
            // W jednej tabeli mogą być różne FieldTypes z różnych Scopes
            // Zamiast tego - walidacja unikalności FieldType per FieldScope w Validator
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateItemCalculatedFieldDefinition
    /// </summary>
    public class CostEstimateTemplateItemCalculatedFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateItemCalculatedFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateItemCalculatedFieldDefinition> builder)
        {
            // Properties specific to calculated fields
            builder.Property(f => f.SumInGroup)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(f => f.SumInTotal)
                .IsRequired()
                .HasDefaultValue(false);
            
            // Relationship with TemplateVersion
            builder.HasOne(f => f.TemplateVersion)
                .WithMany(v => v.CalculatedFieldDefinitions)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Named index to avoid conflicts with base class
            builder.HasIndex(f => new { f.TemplateVersionId, f.FieldType })
                .HasDatabaseName("IX_CalculatedFieldDefinition_TemplateVersionId_FieldType");
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplateItemGenericFieldDefinition
    /// </summary>
    public class CostEstimateTemplateItemGenericFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateItemGenericFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateItemGenericFieldDefinition> builder)
        {          
            // Relationship with TemplateVersion
            builder.HasOne(f => f.TemplateVersion)
                .WithMany(v => v.GenericFieldDefinitions)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Named index to avoid conflicts with base class
            builder.HasIndex(f => new { f.TemplateVersionId, f.FieldType })
                .HasDatabaseName("IX_GenericFieldDefinition_TemplateVersionId_FieldType");
        }
    }
}
