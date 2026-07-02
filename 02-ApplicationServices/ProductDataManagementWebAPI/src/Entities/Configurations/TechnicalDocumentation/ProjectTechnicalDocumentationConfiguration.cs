using Entities.Models.TechnicalDocumentation;
using Entities.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.TechnicalDocumentation;

public class ProjectTechnicalDocumentationConfiguration : IEntityTypeConfiguration<ProjectTechnicalDocumentation>
{
    public void Configure(EntityTypeBuilder<ProjectTechnicalDocumentation> builder)
    {
        builder.ToTable("ProjectTechnicalDocumentations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .HasMaxLength(2000);

        builder.Property(d => d.DetailsJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(d => d.Status)
            .IsRequired();

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.ProjectId).IsRequired();
        builder.Property(d => d.CreatedByUserId).IsRequired();
        builder.Property(d => d.CreatedAt).IsRequired();

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Files)
            .WithOne(f => f.TechnicalDocumentation)
            .HasForeignKey(f => f.TechnicalDocumentationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.TenantId, d.ProjectId });
    }
}
