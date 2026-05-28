using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectFileVersionConfiguration : IEntityTypeConfiguration<ProjectFileVersion>
    {
        public void Configure(EntityTypeBuilder<ProjectFileVersion> builder)
        {
            builder.HasKey(pfv => pfv.Id);

            builder.Property(pfv => pfv.TenantId)
                .IsRequired();

            builder.Property(pfv => pfv.ProjectId)
                .IsRequired();

            builder.Property(pfv => pfv.VersionNumber)
                .IsRequired();
            
            builder.Property(pfv => pfv.BlobFileName)
                .IsRequired()
                .HasMaxLength(300);
            
            builder.Property(pfv => pfv.BlobPath)
                .IsRequired()
                .HasMaxLength(1000);
            
            builder.Property(pfv => pfv.ContentType)
                .IsRequired()
                .HasMaxLength(100);
            
            builder.Property(pfv => pfv.FileSizeBytes)
                .IsRequired();
            
            builder.Property(pfv => pfv.CreatedAt)
                .IsRequired();
            
            builder.Property(pfv => pfv.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasQueryFilter(pfv => !pfv.IsDeleted);

            // Relacja z ProjectFile
            builder.HasOne(pfv => pfv.ProjectFile)
                .WithMany(pf => pf.Versions)
                .HasForeignKey(pfv => pfv.ProjectFileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacja z User (CreatedByUser)
            builder.HasOne(pfv => pfv.CreatedByUser)
                .WithMany()
                .HasForeignKey(pfv => pfv.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indeks dla szybkiego wyszukiwania wersji pliku
            builder.HasIndex(pfv => new { pfv.ProjectFileId, pfv.VersionNumber })
                .IsUnique();
            
            // Indeks dla wyszukiwania nieusuniętych wersji
            builder.HasIndex(pfv => new { pfv.ProjectFileId, pfv.IsDeleted });
            
            // Indeks dla wyszukiwania po autorze wersji
            builder.HasIndex(pfv => pfv.CreatedByUserId);

            // Indeks dla wyszukiwania wersji per tenant/projekt
            builder.HasIndex(pfv => new { pfv.TenantId, pfv.ProjectId });

            // Indeks dla sortowania po dacie
            builder.HasIndex(pfv => pfv.CreatedAt);
        }
    }
}
