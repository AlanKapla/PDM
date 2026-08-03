using Entities.Models.ColdMails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class ColdMailHistoryConfiguration : IEntityTypeConfiguration<ColdMailHistory>
    {
        public void Configure(EntityTypeBuilder<ColdMailHistory> builder)
        {
            builder.ToTable("ColdMailHistories");

            builder.HasKey(h => h.Id);

            builder.Property(h => h.BatchId)
                .IsRequired();

            builder.Property(h => h.RecipientEmail)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(h => h.Subject)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(h => h.Body)
                .IsRequired()
                .HasMaxLength(100_000);

            builder.Property(h => h.HtmlBody)
                .IsRequired()
                .HasMaxLength(150_000);

            builder.Property(h => h.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(h => h.ErrorMessage)
                .HasMaxLength(2000);

            builder.Property(h => h.SentByUserId)
                .IsRequired();

            builder.Property(h => h.SentAt)
                .IsRequired();

            builder.HasOne(h => h.SentByUser)
                .WithMany()
                .HasForeignKey(h => h.SentByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(h => h.RecipientEmail);
            builder.HasIndex(h => h.SentAt)
                .IsDescending();
            builder.HasIndex(h => h.BatchId);
        }
    }
}
