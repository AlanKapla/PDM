using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.CostTrackers
{
    public class TrackedCostConfiguration : IEntityTypeConfiguration<TrackedCost>
    {
        public void Configure(EntityTypeBuilder<TrackedCost> builder)
        {
            builder.HasKey(tc => tc.Id);

            builder.Property(tc => tc.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(tc => tc.TrackerId)
                .IsRequired();

            builder.Property(tc => tc.CostEstimateId);

            builder.Property(tc => tc.CostEstimateItemId);

            builder.Property(tc => tc.Name)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(tc => tc.Description)
                .HasMaxLength(2000);

            builder.Property(tc => tc.Net)
                .HasColumnType("decimal(15,2)");

            builder.Property(tc => tc.Gross)
                .HasColumnType("decimal(15,2)");

            builder.Property(tc => tc.Contractor)
                .HasMaxLength(300);

            builder.Property(tc => tc.Date);

            builder.Property(tc => tc.CreatedAt)
                .IsRequired();

            builder.Property(tc => tc.UpdatedAt);

            builder.Property(tc => tc.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(tc => tc.DeletedAt);

            builder.HasOne(tc => tc.Tracker)
                .WithMany(t => t.TrackedCosts)
                .HasForeignKey(tc => tc.TrackerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(tc => tc.CostEstimateItem)
                .WithMany(i => i.TrackedCosts)
                .HasForeignKey(tc => tc.CostEstimateItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(tc => tc.Attachments)
                .WithOne(a => a.TrackedCost)
                .HasForeignKey(a => a.TrackedCostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(tc => tc.TrackerId);
            builder.HasIndex(tc => tc.CostEstimateItemId);
            builder.HasIndex(tc => tc.IsDeleted);

            builder.HasQueryFilter(tc => !tc.IsDeleted);
        }
    }
}
