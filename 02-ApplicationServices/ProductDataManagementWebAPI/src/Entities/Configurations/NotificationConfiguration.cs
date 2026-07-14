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
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(n => n.Id);

            builder.Property(n => n.Title)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(n => n.Message)
                .HasMaxLength(2000)
                .IsRequired();
            
            builder.Property(n => n.CreatedAt).IsRequired();
            builder.Property(n => n.IsRead).IsRequired();

            builder.Property(n => n.MetadataJson)
                .HasMaxLength(4000);

            builder.HasIndex(n => new { n.UserId, n.IsRead });
            builder.HasIndex(n => new { n.TenantId, n.IsRead });
            builder.HasIndex(n => n.TenantId);
            builder.HasIndex(n => n.ProjectId);
        }
    }
}
