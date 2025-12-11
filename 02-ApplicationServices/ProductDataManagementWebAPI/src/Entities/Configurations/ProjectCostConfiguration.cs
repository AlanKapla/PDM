using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectCostConfiguration : IEntityTypeConfiguration<ProjectCost>
    {
        public void Configure(EntityTypeBuilder<ProjectCost> builder)
        {
            builder.HasKey(pc => pc.Id);

            builder.Property(pc => pc.TenantId).IsRequired();
            builder.Property(pc => pc.ProjectId).IsRequired();
            builder.Property(pc => pc.UserId).IsRequired();
            
            builder.Property(pc => pc.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(pc => pc.Place)
                .HasMaxLength(200);

            builder.Property(pc => pc.Date).IsRequired();

            builder.Property(pc => pc.Description)
                .HasMaxLength(2000);

            builder.Property(pc => pc.NetAmount)
                .HasPrecision(18, 2);

            builder.Property(pc => pc.VatRate)
                .HasPrecision(5, 2);

            builder.Property(pc => pc.GrossAmount)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(pc => pc.IsClosed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(pc => pc.HasDocument).IsRequired();

            builder.Property(pc => pc.DocumentFileName)
                .HasMaxLength(255);

            builder.Property(pc => pc.DocumentBlobPath)
                .HasMaxLength(500);

            builder.Property(pc => pc.DocumentContentType)
                .HasMaxLength(100);

            builder.Property(pc => pc.CreatedAt).IsRequired();
            builder.Property(pc => pc.IsDeleted).IsRequired();

            // Indexes
            builder.HasIndex(pc => new { pc.TenantId, pc.ProjectId, pc.IsDeleted });
            builder.HasIndex(pc => new { pc.TenantId, pc.UserId, pc.IsDeleted });
            builder.HasIndex(pc => pc.Date);

            // Relationships
            builder.HasOne(pc => pc.Project)
                .WithMany()
                .HasForeignKey(pc => pc.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pc => pc.TenantMember)
                .WithMany()
                .HasForeignKey(pc => new { pc.TenantId, pc.UserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
