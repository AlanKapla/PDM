using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class MessageHistoryConfiguration : IEntityTypeConfiguration<MessageHistory>
    {
        public void Configure(EntityTypeBuilder<MessageHistory> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.Content)
                .HasMaxLength(4000)
                .IsRequired();

            builder.Property(m => m.CreatedAt).IsRequired();
            builder.Property(m => m.EditedAt);
            builder.Property(m => m.ReplyToMessageId);

            builder.HasQueryFilter(m => !m.IsDeleted);

            builder.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.ReplyToMessage)
                .WithMany()
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.ChatId, m.CreatedAt });
            builder.HasIndex(m => m.ReplyToMessageId)
                .HasFilter("ReplyToMessageId IS NOT NULL");
        }
    }
}
