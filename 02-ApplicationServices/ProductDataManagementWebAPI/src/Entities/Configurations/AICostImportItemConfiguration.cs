using Entities.Models.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class AICostImportItemConfiguration : IEntityTypeConfiguration<AICostImportItem>
    {
        public void Configure(EntityTypeBuilder<AICostImportItem> builder)
        {
            builder.ToTable("AICostImportItems");

            builder.HasKey(i => i.Id);

            builder.Property(i => i.OriginalFileName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(i => i.ContentType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(i => i.FileHashSha256)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(i => i.BlobPath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(i => i.ParsedDataJson)
                .HasMaxLength(8000);

            builder.Property(i => i.LastError)
                .HasMaxLength(2000);

            builder.Property(i => i.CreatedAt).IsRequired();
            builder.Property(i => i.UpdatedAt).IsRequired();

            builder.HasIndex(i => new { i.TenantId, i.ProjectId, i.Status });
            builder.HasIndex(i => new { i.TenantId, i.ProjectId, i.FileHashSha256 });
            builder.HasIndex(i => new { i.AnalyzedAt, i.Status });
        }
    }
}
