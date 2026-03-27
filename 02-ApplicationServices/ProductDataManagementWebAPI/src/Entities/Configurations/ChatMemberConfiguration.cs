using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ChatMemberConfiguration : IEntityTypeConfiguration<ChatMember>
    {
        public void Configure(EntityTypeBuilder<ChatMember> builder)
        {
            builder.HasKey(cm => cm.Id);

            builder.Property(cm => cm.JoinedAt).IsRequired();

            builder.HasOne(cm => cm.Chat)
                .WithMany(c => c.Members)
                .HasForeignKey(cm => cm.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(cm => new { cm.ChatId, cm.UserId }).IsUnique();
            builder.HasIndex(cm => cm.UserId);
        }
    }
}
