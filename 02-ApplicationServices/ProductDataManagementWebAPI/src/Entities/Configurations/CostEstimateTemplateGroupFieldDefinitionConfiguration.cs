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
            // ✅ Klucz skonfigurowany TYLKO w base class
            builder.HasKey(f => f.Id);
            
            builder.Property(f => f.TemplateVersionId)
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
            
            builder.Property(f => f.ParentFieldId);
            
            builder.Property(f => f.Order)
                .IsRequired()
                .HasDefaultValue(0);
            
            // Hierarchical relationship: Parent-Child (self-referencing)
            builder.HasOne(f => f.ParentField)
                .WithMany(f => f.ChildFields)
                .HasForeignKey(f => f.ParentFieldId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // ✅ TPH Discriminator - EF Core rozróżnia typy po kolumnie
            builder.HasDiscriminator<string>("FieldDefinitionType")
                .HasValue<CostEstimateTemplateGroupFieldDefinition>("Group")
                .HasValue<CostEstimateTemplateItemSystemFieldDefinition>("ItemSystem")
                .HasValue<CostEstimateTemplateItemCalculatedFieldDefinition>("ItemCalculated")
                .HasValue<CostEstimateTemplateItemGenericFieldDefinition>("ItemGeneric");
            
            // ❌ NIE DEFINIUJ indeksu na TemplateVersionId - jest automatycznie tworzony przez FK w derived types!
            // ✅ Tylko indeks na FieldName (nie koliduje z FK)
            builder.HasIndex(f => new { f.TemplateVersionId, f.FieldName })
                .HasDatabaseName("IX_FieldDefinitionBase_TemplateVersionId_FieldName");
            
            // Index dla hierarchii
            builder.HasIndex(f => f.ParentFieldId);
            
            // Index dla UI Order (dla parent fields)
            builder.HasIndex(f => new { f.TemplateVersionId, f.FieldScope, f.ParentFieldId, f.Order })
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
            // ❌ NIE ustawiaj HasKey - jest w base class!
            // ❌ NIE ustawiaj indeksów z base class - są dziedziczone!
            
            // Relationship with TemplateVersion
            builder.HasOne(f => f.TemplateVersion)
                .WithMany(v => v.GroupFieldDefinitions)
                .HasForeignKey(f => f.TemplateVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
