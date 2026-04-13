using Entities.Models;
using Entities.Models.CostTrackers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.CostTrackers
{
    public class ProjectCostTrackedCostLinkConfiguration : IEntityTypeConfiguration<ProjectCostTrackedCostLink>
    {
        public void Configure(EntityTypeBuilder<ProjectCostTrackedCostLink> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(l => l.ProjectCostId)
                .IsRequired();

            builder.Property(l => l.TrackedCostId)
                .IsRequired();

            builder.Property(l => l.LinkedAt)
                .IsRequired();

            builder.HasIndex(l => l.ProjectCostId)
                .IsUnique();

            builder.HasIndex(l => l.TrackedCostId)
                .IsUnique();

            builder.HasOne(l => l.ProjectCost)
                .WithOne()
                .HasForeignKey<ProjectCostTrackedCostLink>(l => l.ProjectCostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(l => l.TrackedCost)
                .WithOne(tc => tc.ProjectCostLink)
                .HasForeignKey<ProjectCostTrackedCostLink>(l => l.TrackedCostId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
