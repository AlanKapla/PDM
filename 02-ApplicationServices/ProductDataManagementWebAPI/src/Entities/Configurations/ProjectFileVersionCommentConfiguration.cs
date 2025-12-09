using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ProjectFileVersionCommentConfiguration : IEntityTypeConfiguration<ProjectFileVersionComment>
    {
        public void Configure(EntityTypeBuilder<ProjectFileVersionComment> builder)
        {
            builder.HasKey(c => c.Id);
            
            builder.Property(c => c.Content)
                .IsRequired()
                .HasMaxLength(2000);
            
            builder.Property(c => c.CreatedAt)
                .IsRequired();
            
            builder.Property(c => c.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasOne(c => c.ProjectFileVersion)
                .WithMany(v => v.Comments)
                .HasForeignKey(c => c.ProjectFileVersionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.ProjectFileVersionId, c.IsDeleted });
            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => c.CreatedAt);
        }
    }
}
