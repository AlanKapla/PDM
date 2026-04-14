using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.CostTrackers
{
    public class CostTrackerConfiguration : IEntityTypeConfiguration<CostTracker>
    {
        public void Configure(EntityTypeBuilder<CostTracker> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(t => t.TenantId)
                .IsRequired();

            builder.Property(t => t.ProjectId)
                .IsRequired();

            builder.Property(t => t.BudgetNet)
                .HasPrecision(18, 2);

            builder.Property(t => t.BudgetGross)
                .HasPrecision(18, 2);

            builder.HasMany(t => t.TrackedCosts)
                .WithOne(tc => tc.Tracker)
                .HasForeignKey(tc => tc.TrackerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(t => t.ProjectId).IsUnique();
            builder.HasIndex(t => t.TenantId);
        }
    }
}
