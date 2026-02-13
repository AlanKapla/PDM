using Entities.Models.CostEstimateTemplates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja dla CostEstimateTemplateFieldDefinitionBase (TPH - Table-Per-Hierarchy)
    /// Wszystkie typy (Group, System, Calculated, Generic) są w jednej tabeli
    /// </summary>
    public class CostEstimateTemplateFieldDefinitionBaseConfiguration : IEntityTypeConfiguration<CostEstimateTemplateFieldDefinitionBase>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateFieldDefinitionBase> builder)
        {
            builder.HasKey(f => f.Id);
            
            builder.Property(f => f.TemplateId)
                .IsRequired();
            
            builder.Property(f => f.FieldName)
                .IsRequired();
            
            builder.Property(f => f.FieldScope)
                .IsRequired()
                .HasConversion<string>();
            
            builder.Property(f => f.FieldType)
                .IsRequired()
                .HasConversion<string>();
            
            builder.Property(f => f.Label)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(f => f.IsSortable)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(f => f.IsFilterable)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(f => f.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);
            
            builder.Property(f => f.ParentFieldId);
            
            builder.Property(f => f.Order)
                .IsRequired()
                .HasDefaultValue(0);
            
            // Hierarchical relationship: Parent-Child (self-referencing)
            // ⚠️ RESTRICT instead of CASCADE to avoid multiple cascade paths with Template FK
            // When deleting Template → all fields are deleted automatically (via Template FK)
            // When deleting Field with children → must delete children first (RESTRICT)
            builder.HasOne(f => f.ParentField)
                .WithMany(f => f.ChildFields)
                .HasForeignKey(f => f.ParentFieldId)
                .OnDelete(DeleteBehavior.Restrict);  // ✅ Changed from Cascade to Restrict
            
            // TPH Discriminator
            builder.HasDiscriminator<string>("FieldDefinitionType")
                .HasValue<CostEstimateTemplateGroupFieldDefinition>("Group")
                .HasValue<CostEstimateTemplateItemSystemFieldDefinition>("ItemSystem")
                .HasValue<CostEstimateTemplateItemCalculatedFieldDefinition>("ItemCalculated")
                .HasValue<CostEstimateTemplateItemGenericFieldDefinition>("ItemGeneric");
            
            builder.HasIndex(f => new { f.TemplateId, f.FieldName })
                .HasDatabaseName("IX_FieldDefinitionBase_TemplateId_FieldName");
            
            builder.HasIndex(f => f.ParentFieldId);
            
            builder.HasIndex(f => new { f.TemplateId, f.FieldScope, f.ParentFieldId, f.Order })
                .HasDatabaseName("IX_FieldDefinitionBase_Order");
        }
    }
    
    /// <summary>
    /// Konfiguracja dla CostEstimateTemplateGroupFieldDefinition
    /// </summary>
    public class CostEstimateTemplateGroupFieldDefinitionConfiguration : IEntityTypeConfiguration<CostEstimateTemplateGroupFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplateGroupFieldDefinition> builder)
        {
            // Relationship with Template
            builder.HasOne(f => f.Template)
                .WithMany(t => t.GroupFieldDefinitions)
                .HasForeignKey(f => f.TemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
