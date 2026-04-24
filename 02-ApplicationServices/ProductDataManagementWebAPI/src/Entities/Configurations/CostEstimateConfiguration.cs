using Entities.Models.CostEstimates;
using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    /// <summary>
    /// Konfiguracja EF Core dla CostEstimate
    /// </summary>
    public class CostEstimateConfiguration : IEntityTypeConfiguration<CostEstimate>
    {
        public void Configure(EntityTypeBuilder<CostEstimate> builder)
        {
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.TenantId)
                .IsRequired();
            
            builder.Property(c => c.ProjectId)
                .IsRequired();
            
            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(c => c.Description)
                .HasMaxLength(1000);
            
            builder.Property(c => c.Status)
                .IsRequired()
                .HasConversion<string>();
            
            builder.Property(c => c.SelectedCurrencyId)
                .IsRequired();
            
            builder.Property(c => c.TotalNet)
                .HasPrecision(18, 2);
            
            builder.Property(c => c.TotalGross)
                .HasPrecision(18, 2);
            
            builder.Property(c => c.TotalVat)
                .HasPrecision(18, 2);
            
            builder.Property(c => c.CreatedAt)
                .IsRequired();
            
            builder.Property(c => c.UpdatedAt);
            
            builder.Property(c => c.LastCalculatedAt);
            
            builder.Property(c => c.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);
            
            builder.Property(c => c.DeletedAt);
            
            // Relationship with Tenant
            builder.HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with Project
            builder.HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with CostEstimateTemplate
            builder.HasOne(c => c.Template)
                .WithMany()
                .HasForeignKey(c => c.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with User (Owner)
            builder.HasOne(c => c.Owner)
                .WithMany()
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Relationship with SelectedCurrency
            builder.HasOne(c => c.SelectedCurrency)
                .WithMany()
                .HasForeignKey(c => c.SelectedCurrencyId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // RootGroups - ignoruj jako osobną nawigację, będzie to filtrowane zapytanie na AllGroups
            builder.Ignore(c => c.RootGroups);
            
            // Relationship with AllGroups (all groups in cost estimate) - GŁÓWNA RELACJA
            builder.HasMany(c => c.AllGroups)
                .WithOne(g => g.CostEstimate)
                .HasForeignKey(g => g.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relationship with AllItems
            builder.HasMany(c => c.AllItems)
                .WithOne(w => w.CostEstimate)
                .HasForeignKey(w => w.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Indexes for better query performance
            builder.HasIndex(c => c.TenantId);
            builder.HasIndex(c => c.ProjectId);
            builder.HasIndex(c => new { c.TenantId, c.ProjectId });
            builder.HasIndex(c => c.TemplateId);
            builder.HasIndex(c => c.OwnerId);
            builder.HasIndex(c => c.SelectedCurrencyId);
            builder.HasIndex(c => c.Status);
            builder.HasIndex(c => c.IsDeleted);
            builder.HasIndex(c => c.CreatedAt);

            // Global query filter for soft delete and tenant isolation
            builder.HasQueryFilter(c => !c.IsDeleted);
        }
    }
}
