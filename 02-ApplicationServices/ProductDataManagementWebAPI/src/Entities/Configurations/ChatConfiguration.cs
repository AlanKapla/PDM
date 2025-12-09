using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(c => c.IsGroupChat).IsRequired();
            builder.Property(c => c.CreatedAt).IsRequired();

            builder.HasOne(c => c.Tenant)
                .WithMany()
                .HasForeignKey(c => c.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.Project)
                .WithMany()
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => new { c.TenantId, c.CreatedByUserId })
                .HasPrincipalKey(t => new { t.TenantId, t.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => c.TenantId);
            builder.HasIndex(c => c.ProjectId);
            builder.HasIndex(c => new { c.ProjectId, c.IsGroupChat });
        }
    }
}
