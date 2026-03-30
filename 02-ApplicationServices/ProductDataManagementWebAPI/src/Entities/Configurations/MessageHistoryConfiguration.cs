using Entities.Models;
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
            builder.Property(m => m.DeletedAt);
            builder.Property(m => m.ReplyToMessageId);

            builder.Ignore(m => m.IsDeleted);

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
