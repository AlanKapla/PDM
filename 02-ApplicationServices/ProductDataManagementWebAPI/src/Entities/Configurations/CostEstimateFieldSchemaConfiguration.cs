using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class CostEstimateFieldSchemaConfiguration : IEntityTypeConfiguration<CostEstimateFieldSchema>
    {
        public void Configure(EntityTypeBuilder<CostEstimateFieldSchema> builder)
        {
            builder.ToTable("CostEstimateFieldSchemas");

            builder.HasKey(f => f.Id);

            builder.Property(f => f.CostEstimateId)
                .IsRequired();

            builder.Property(f => f.FieldName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(f => f.FieldKey)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(f => f.FieldType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(f => f.IsBasicField)
                .IsRequired();

            builder.Property(f => f.IsAdditionalField)
                .IsRequired();

            builder.Property(f => f.Order)
                .IsRequired();

            builder.Property(f => f.CreatedAt)
                .IsRequired();

            builder.Property(f => f.UpdatedAt);

            builder.HasOne(f => f.CostEstimate)
                .WithMany(ce => ce.FieldSchemas)
                .HasForeignKey(f => f.CostEstimateId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Values)
                .WithOne(v => v.FieldSchema)
                .HasForeignKey(v => v.FieldSchemaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(f => f.CostEstimateId);
            builder.HasIndex(f => new { f.CostEstimateId, f.Order });
            builder.HasIndex(f => new { f.CostEstimateId, f.FieldKey }).IsUnique();
        }
    }
}
