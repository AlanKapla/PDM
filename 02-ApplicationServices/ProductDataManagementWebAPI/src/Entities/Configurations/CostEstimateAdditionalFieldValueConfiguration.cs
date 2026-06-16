using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateAdditionalFieldValue
    /// </summary>
    public class CostEstimateAdditionalFieldValueConfiguration : IEntityTypeConfiguration<CostEstimateAdditionalFieldValue>
    {
        public void Configure(EntityTypeBuilder<CostEstimateAdditionalFieldValue> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.FieldSchemaId)
                .IsRequired();

            builder.Property(v => v.GroupId);

            builder.Property(v => v.ItemId);

            // Typowane wartości
            builder.Property(v => v.StringValue)
                .HasMaxLength(4000);

            builder.Property(v => v.DecimalValue)
                .HasPrecision(18, 6);

            builder.Property(v => v.BoolValue);

            builder.Property(v => v.DateTimeValue);

            builder.Property(v => v.CreatedAt)
                .IsRequired();

            builder.Property(v => v.UpdatedAt);

            // Relationship with AdditionalField
            builder.HasOne(v => v.FieldSchema)
                .WithMany(f => f.Values)
                .HasForeignKey(v => v.FieldSchemaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Group (nullable)
            builder.HasOne(v => v.Group)
                .WithMany(g => g.AdditionalFieldValues)
                .HasForeignKey(v => v.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with Item (nullable)
            builder.HasOne(v => v.Item)
                .WithMany(i => i.AdditionalFieldValues)
                .HasForeignKey(v => v.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(v => v.FieldSchemaId);
            builder.HasIndex(v => v.GroupId);
            builder.HasIndex(v => v.ItemId);
        }
    }
}
