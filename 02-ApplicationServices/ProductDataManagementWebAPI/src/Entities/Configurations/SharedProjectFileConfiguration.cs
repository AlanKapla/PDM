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
    public class SharedProjectFileConfiguration : IEntityTypeConfiguration<SharedProjectFile>
    {
        public void Configure(EntityTypeBuilder<SharedProjectFile> builder)
        {
            builder.HasKey(spf => spf.Id);
            
            builder.Property(spf => spf.ProjectFilePackageId)
                .IsRequired();
            
            builder.Property(spf => spf.ProjectFileId);  // Nullable
            
            builder.Property(spf => spf.Access)
                .IsRequired()
                .HasConversion(
                    v => v.ToString(),  // ✅ Explicit: Enum → String
                    v => (ProjectFileAccess)Enum.Parse(typeof(ProjectFileAccess), v))  // ✅ String → Enum
                .HasColumnType("nvarchar(10)");  // ✅ Bez HasDefaultValue - zawsze ustawiamy explicit w kodzie
            
            builder.Property(spf => spf.SharedAt)
                .IsRequired();

            // Relacja z Project - NoAction aby uniknąć cyklicznych ścieżek kaskadowych
            builder.HasOne(spf => spf.Project)
                .WithMany()
                .HasForeignKey(spf => spf.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);
            
            // Relacja z ProjectFilePackage - ⚠️ RESTRICT instead of CASCADE
            // Reason: ProjectFile → ProjectFilePackage (CASCADE) already exists
            // When deleting Package → ProjectFiles are deleted → SharedProjectFiles are deleted (via ProjectFile FK)
            // Direct Package → SharedProjectFile CASCADE would create multiple cascade paths
            builder.HasOne(spf => spf.ProjectFilePackage)
                .WithMany()
                .HasForeignKey(spf => spf.ProjectFilePackageId)
                .OnDelete(DeleteBehavior.Restrict);  // ✅ Changed from Cascade to Restrict

            // Relacja z ProjectFile - Cascade OK (nullable bo może udostępniać całą paczkę)
            builder.HasOne(spf => spf.ProjectFile)
                .WithMany(pf => pf.SharedWith)
                .HasForeignKey(spf => spf.ProjectFileId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Relacja z User (SharedByUser)
            builder.HasOne(spf => spf.SharedByUser)
                .WithMany()
                .HasForeignKey(spf => spf.SharedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z User (SharedWithUser)
            builder.HasOne(spf => spf.SharedWithUser)
                .WithMany()
                .HasForeignKey(spf => spf.SharedWithUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z TenantMember (SharedByTenantMember)
            builder.HasOne(spf => spf.SharedByTenantMember)
                .WithMany()
                .HasForeignKey(spf => new { spf.TenantId, spf.SharedByUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja z TenantMember (SharedWithTenantMember)
            builder.HasOne(spf => spf.SharedWithTenantMember)
                .WithMany()
                .HasForeignKey(spf => new { spf.TenantId, spf.SharedWithUserId })
                .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            // Unikalność - jedna kombinacja PackageId + FileId + User może istnieć tylko raz
            builder.HasIndex(spf => new { spf.ProjectFilePackageId, spf.ProjectFileId, spf.SharedWithUserId })
                .IsUnique();

            // Indeks dla szybkiego wyszukiwania udostępnień użytkownika w projekcie
            builder.HasIndex(spf => new { spf.SharedWithUserId, spf.ProjectId });
            
            // Indeks dla wyszukiwania plików/paczek udostępnionych przez użytkownika
            builder.HasIndex(spf => new { spf.SharedByUserId, spf.ProjectId });
            
            // Indeks dla wyszukiwania udostępnień paczki
            builder.HasIndex(spf => new { spf.ProjectFilePackageId, spf.SharedWithUserId });
        }
    }
}
