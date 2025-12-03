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
            
            builder.Property(pf => pf.ContentType)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(pf => pf.BlobPath)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(pf => pf.FileSizeBytes)
                .IsRequired();
            
            builder.Property(pf => pf.UploadedAt)
                .IsRequired();

            // Relacja z Project
            builder.HasOne(pf => pf.Project)
                .WithMany()
                .HasForeignKey(pf => pf.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja z User (UploadedByUser)
            builder.HasOne(pf => pf.UploadedByUser)
                .WithMany()
                .HasForeignKey(pf => pf.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z TenantMember (UploadedByTenantMember)
            builder.HasOne(pf => pf.UploadedByTenantMember)
                .WithMany()
                .HasForeignKey(pf => new { pf.TenantId, pf.UploadedByUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Indeks dla szybkiego wyszukiwania plików projektu
            builder.HasIndex(pf => new { pf.ProjectId, pf.TenantId });
            
            // Indeks dla wyszukiwania po nazwie paczki
            builder.HasIndex(pf => new { pf.ProjectId, pf.PackageName });
            
            // Indeks dla wyszukiwania plików użytkownika
            builder.HasIndex(pf => pf.UploadedByUserId);
        }
    }
}
