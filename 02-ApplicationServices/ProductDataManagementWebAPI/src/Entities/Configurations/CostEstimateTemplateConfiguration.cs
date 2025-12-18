using Entities.Models;
using Entities.Models.CostEstimateTemplateDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateTemplate
    /// </summary>
    public class CostEstimateTemplateConfiguration : IEntityTypeConfiguration<CostEstimateTemplate>
    {
        public void Configure(EntityTypeBuilder<CostEstimateTemplate> builder)
        {
            builder.HasKey(t => t.Id);
            
            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(t => t.Description)
                .HasMaxLength(1000);
            
            // ✅ Configure TemplateStructure as JSON column with value converter
            builder.Property(t => t.TemplateStructure)
                .HasColumnType("nvarchar(max)")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<CostEstimateTemplateStructure>(v, (JsonSerializerOptions?)null)!
                )
                .IsRequired();
            
            builder.Property(t => t.CreatedAt)
                .IsRequired();
            
            builder.Property(t => t.UpdatedAt);
            
            builder.Property(t => t.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(t => t.DeletedAt);
            
            // Relationship with User (Owner)
            builder.HasOne(t => t.Owner)
                .WithMany()
                .HasForeignKey(t => t.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Index for better query performance
            builder.HasIndex(t => t.OwnerId);
            builder.HasIndex(t => t.IsDeleted);
            builder.HasIndex(t => t.CreatedAt);
            
            // Global query filter for soft delete
            builder.HasQueryFilter(t => !t.IsDeleted);
        }
    }
}
