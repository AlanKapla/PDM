using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class CostEstimateItemConfiguration : IEntityTypeConfiguration<CostEstimateItem>
    {
        public void Configure(EntityTypeBuilder<CostEstimateItem> builder)
        {
            builder.HasKey(w => w.Id);
            
            builder.Property(w => w.CostEstimateId)
                .IsRequired();
            
            builder.Property(w => w.GroupId)
                .IsRequired();
            
            builder.Property(w => w.ParentItemId);  // Nullable - tylko dla opcji
            
            builder.Property(w => w.Order)
                .IsRequired();
            
            builder.Property(w => w.CreatedAt)
                .IsRequired();
            
            builder.Property(w => w.UpdatedAt);
            
            builder.Property(w => w.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(w => w.DeletedAt);
            
            builder.HasOne(w => w.CostEstimate)
                .WithMany(c => c.AllItems)
                .HasForeignKey(w => w.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasOne(w => w.Group)
                .WithMany(g => g.Items)
                .HasForeignKey(w => w.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Self-referencing relationship: Item może mieć ParentItem (jeśli jest opcją)
            builder.HasOne(w => w.ParentItem)
                .WithMany(p => p.Options)
                .HasForeignKey(w => w.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(w => w.FieldValues)
                .WithOne(fv => fv.Item)
                .HasForeignKey(fv => fv.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(w => w.CostEstimateId);
            builder.HasIndex(w => w.GroupId);
            builder.HasIndex(w => w.ParentItemId);  // Index for options lookup
            builder.HasIndex(w => new { w.GroupId, w.Order });
            builder.HasIndex(w => w.IsDeleted);
            
            builder.HasQueryFilter(w => !w.IsDeleted);
        }
    }
    
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateItemFieldValue
    /// Używa pojedynczej relacji do CostEstimateTemplateFieldDefinitionBase (polimorfizm TPH)
    /// </summary>
    public class CostEstimateItemFieldValueConfiguration : IEntityTypeConfiguration<CostEstimateItemFieldValue>
    {
        public void Configure(EntityTypeBuilder<CostEstimateItemFieldValue> builder)
        {
            builder.HasKey(fv => fv.Id);
            
            builder.Property(fv => fv.ItemId)
                .IsRequired();
            
            builder.Property(fv => fv.FieldDefinitionId)
                .IsRequired();
            
            builder.Property(fv => fv.Value)
                .HasMaxLength(2000);
            
            builder.Property(fv => fv.CreatedAt)
                .IsRequired();
            
            builder.Property(fv => fv.UpdatedAt);
            
            // Relationship with CostEstimateItem
            builder.HasOne(fv => fv.Item)
                .WithMany(w => w.FieldValues)
                .HasForeignKey(fv => fv.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Relationship with FieldDefinition (base class) - EF Core obsługuje polimorfizm (TPH)
            // Może wskazywać na SystemFieldDefinition, CalculatedFieldDefinition lub GenericFieldDefinition
            builder.HasOne(fv => fv.FieldDefinition)
                .WithMany()
                .HasForeignKey(fv => fv.FieldDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for better query performance
            builder.HasIndex(fv => fv.ItemId);
            builder.HasIndex(fv => fv.FieldDefinitionId);
            builder.HasIndex(fv => new { fv.ItemId, fv.FieldDefinitionId });
        }
    }
}
