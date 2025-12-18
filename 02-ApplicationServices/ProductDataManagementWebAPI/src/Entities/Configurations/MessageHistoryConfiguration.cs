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

            builder.HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => new { m.TenantId, m.UserId })
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(m => new { m.ChatId, m.CreatedAt });
            builder.HasIndex(m => new { m.TenantId, m.UserId });
        }
    }
}
