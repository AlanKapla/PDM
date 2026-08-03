using Entities.Models.Activity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
    {
        public void Configure(EntityTypeBuilder<UserActivityLog> builder)
        {
            builder.ToTable("UserActivityLogs");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.EventType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(l => l.IpAddress)
                .IsRequired()
                .HasMaxLength(45);

            builder.Property(l => l.OccurredAtUtc)
                .IsRequired();

            builder.Property(l => l.Route)
                .HasMaxLength(500);

            builder.Property(l => l.UserId);

            builder.Property(l => l.AzureAdB2CObjectId)
                .HasMaxLength(64);

            builder.HasIndex(l => l.OccurredAtUtc)
                .IsDescending();
        }
    }
}
