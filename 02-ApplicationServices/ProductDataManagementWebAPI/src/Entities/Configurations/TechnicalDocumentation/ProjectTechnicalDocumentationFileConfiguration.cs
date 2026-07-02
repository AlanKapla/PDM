using Entities.Models.TechnicalDocumentation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.TechnicalDocumentation;

public class ProjectTechnicalDocumentationFileConfiguration : IEntityTypeConfiguration<ProjectTechnicalDocumentationFile>
{
    public void Configure(EntityTypeBuilder<ProjectTechnicalDocumentationFile> builder)
    {
        builder.ToTable("ProjectTechnicalDocumentationFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginalFileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.BlobName)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.FileSize).IsRequired();
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.TenantId).IsRequired();
        builder.Property(f => f.ProjectId).IsRequired();
        builder.Property(f => f.TechnicalDocumentationId).IsRequired();

        builder.HasIndex(f => new { f.TenantId, f.ProjectId, f.TechnicalDocumentationId });
    }
}
