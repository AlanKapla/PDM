using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
    {
        public void Configure(EntityTypeBuilder<ProjectFile> builder)
        {
            builder.HasKey(pf => pf.Id);
            
            builder.Property(pf => pf.FileName)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(pf => pf.PackageName)
                .IsRequired()
                .HasMaxLength(200);
            
            builder.Property(pf => pf.DisplayName)
                .IsRequired()
                .HasMaxLength(255);
            
            builder.Property(pf => pf.CreatedAt)
                .IsRequired();
            
            builder.Property(pf => pf.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            // Relacja z Project
            builder.HasOne(pf => pf.Project)
                .WithMany()
                .HasForeignKey(pf => pf.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja z User (Owner)
            builder.HasOne(pf => pf.Owner)
                .WithMany()
                .HasForeignKey(pf => pf.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z TenantMember (OwnerTenantMember)
            builder.HasOne(pf => pf.OwnerTenantMember)
                .WithMany()
                .HasForeignKey(pf => new { pf.TenantId, pf.OwnerId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z CurrentVersion (opcjonalna, 1:0..1)
            builder.HasOne(pf => pf.CurrentVersion)
                .WithMany()
                .HasForeignKey(pf => pf.CurrentVersionId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Indeks dla szybkiego wyszukiwania plików projektu
            builder.HasIndex(pf => new { pf.ProjectId, pf.TenantId });
            
            // Indeks dla wyszukiwania po nazwie paczki
            builder.HasIndex(pf => new { pf.ProjectId, pf.PackageName });
            
            // Indeks dla wyszukiwania plików właściciela
            builder.HasIndex(pf => pf.OwnerId);
            
            // Indeks dla wyszukiwania nieusuniętych plików
            builder.HasIndex(pf => new { pf.ProjectId, pf.IsDeleted });
        }
    }
}
