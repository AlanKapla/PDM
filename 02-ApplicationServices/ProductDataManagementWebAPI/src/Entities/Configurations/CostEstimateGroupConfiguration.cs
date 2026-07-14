using Entities.Models.CostEstimates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimateGroup
    /// </summary>
    public class CostEstimateGroupConfiguration : IEntityTypeConfiguration<CostEstimateGroup>
    {
        public void Configure(EntityTypeBuilder<CostEstimateGroup> builder)
        {
            builder.HasKey(g => g.Id);
            
            builder.Property(g => g.CostEstimateId)
                .IsRequired();
            
            builder.Property(g => g.Name)
                .IsRequired()
                .HasMaxLength(500);
            
            builder.Property(g => g.ParentGroupId);
            
            builder.Property(g => g.Level)
                .IsRequired();
            
            builder.Property(g => g.Order)
                .IsRequired();
            
            builder.Property(g => g.TotalNet)
                .HasPrecision(18, 2);
            
            builder.Property(g => g.TotalGross)
                .HasPrecision(18, 2);
            
            builder.Property(g => g.TotalVat)
                .HasPrecision(18, 2);
            
            builder.Property(g => g.LastCalculatedAt);
            
            builder.Property(g => g.CreatedAt)
                .IsRequired();
            
            builder.Property(g => g.UpdatedAt);
            
            builder.Property(g => g.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(g => g.DeletedAt);
            
            // Relationship with CostEstimate
            builder.HasOne(g => g.CostEstimate)
                .WithMany(c => c.AllGroups)
                .HasForeignKey(g => g.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Self-referencing relationship for hierarchy
            builder.HasOne(g => g.ParentGroup)
                .WithMany(g => g.ChildGroups)
                .HasForeignKey(g => g.ParentGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with Items
            builder.HasMany(g => g.Items)
                .WithOne(w => w.Group)
                .HasForeignKey(w => w.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Indexes
            builder.HasIndex(g => g.CostEstimateId);
            builder.HasIndex(g => g.ParentGroupId);
            builder.HasIndex(g => new { g.CostEstimateId, g.ParentGroupId });
            builder.HasIndex(g => new { g.CostEstimateId, g.Level });
            builder.HasIndex(g => new { g.ParentGroupId, g.Order });
            builder.HasIndex(g => g.IsDeleted);
            
            // Global query filter for soft delete
            builder.HasQueryFilter(g => !g.IsDeleted);
        }
    }
    
}
