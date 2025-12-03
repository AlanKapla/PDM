using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class SharedProjectFileConfiguration : IEntityTypeConfiguration<SharedProjectFile>
    {
        public void Configure(EntityTypeBuilder<SharedProjectFile> builder)
        {
            builder.HasKey(spf => spf.Id);
            
            builder.Property(spf => spf.SharedAt)
                .IsRequired();

            // Relacja z Project - NoAction aby uniknąć cyklicznych ścieżek kaskadowych
            builder.HasOne(spf => spf.Project)
                .WithMany()
                .HasForeignKey(spf => spf.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relacja z ProjectFile - Cascade jest OK, bo ProjectFile też jest cascade z Project
            builder.HasOne(spf => spf.ProjectFile)
                .WithMany()
                .HasForeignKey(spf => spf.ProjectFileId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Unikalność - jeden plik może być udostępniony danemu użytkownikowi tylko raz
            builder.HasIndex(spf => new { spf.ProjectFileId, spf.SharedWithUserId })
                .IsUnique();

            // Indeks dla szybkiego wyszukiwania plików udostępnionych użytkownikowi
            builder.HasIndex(spf => new { spf.SharedWithUserId, spf.ProjectId });
            
            // Indeks dla wyszukiwania plików udostępnionych przez użytkownika
            builder.HasIndex(spf => new { spf.SharedByUserId, spf.ProjectId });
        }
    }
}
