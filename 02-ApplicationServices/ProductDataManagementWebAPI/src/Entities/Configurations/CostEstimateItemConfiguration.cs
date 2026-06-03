using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class CostEstimateItemConfiguration : IEntityTypeConfiguration<CostEstimateItem>
    {
        public void Configure(EntityTypeBuilder<CostEstimateItem> builder)
        {
            builder.HasKey(w => w.Id);

            // Options i Components to właściwości wyliczane z _childItems (filtrowane po RelationType).
            // Bez Ignore EF tworzy implicit many-to-many join table 'CostEstimateItemCostEstimateItem'
            // (ComponentsId, OptionsId), co powoduje PK violation przy SaveChanges.
            builder.Ignore(w => w.Options);
            builder.Ignore(w => w.Components);

            builder.Property(w => w.CostEstimateId)
                .IsRequired();
            
            builder.Property(w => w.GroupId)
                .IsRequired();
            
            builder.Property(w => w.ParentItemId);  // Nullable - dla opcji i komponentów
            
            builder.Property(w => w.RelationType)
                .IsRequired()
                .HasConversion<string>()
                .HasDefaultValue(ItemRelationType.None);
            
            builder.Property(w => w.Order)
                .IsRequired();
            
            builder.Property(w => w.NetValue)
                .HasPrecision(18, 2);
            
            builder.Property(w => w.GrossValue)
                .HasPrecision(18, 2);
            
            builder.Property(w => w.VatValue)
                .HasPrecision(18, 2);
            
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
            
            // Self-referencing relationship: ParentItem → ChildItems
            // UWAGA: Nie mapujemy osobno Options i Components w EF!
            // Rozróżnienie następuje przez RelationType w kodzie aplikacji
            builder.HasOne(w => w.ParentItem)
                .WithMany()  // ✅ Brak nawigacji z parent do children w EF
                .HasForeignKey(w => w.ParentItemId)
                .OnDelete(DeleteBehavior.Restrict);
            
            builder.HasMany(w => w.FieldValues)
                .WithOne(fv => fv.Item)
                .HasForeignKey(fv => fv.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            
            builder.HasIndex(w => w.CostEstimateId);
            builder.HasIndex(w => w.GroupId);
            builder.HasIndex(w => w.ParentItemId);
            builder.HasIndex(w => new { w.ParentItemId, w.RelationType });  // Index dla filtrowania Options vs Components
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
            
            // Typowane właściwości wartości
            builder.Property(fv => fv.StringValue)
                .HasMaxLength(2000);
            
            builder.Property(fv => fv.DecimalValue)
                .HasPrecision(18, 6);
            
            builder.Property(fv => fv.BoolValue);
            
            builder.Property(fv => fv.DateTimeValue);
            
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
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes for better query performance
            builder.HasIndex(fv => fv.ItemId);
            builder.HasIndex(fv => fv.FieldDefinitionId);
            builder.HasIndex(fv => new { fv.ItemId, fv.FieldDefinitionId });
        }
    }
}
