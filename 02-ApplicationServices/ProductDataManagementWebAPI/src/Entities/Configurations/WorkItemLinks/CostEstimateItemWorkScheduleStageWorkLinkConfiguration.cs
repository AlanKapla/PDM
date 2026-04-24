using Entities.Models.WorkItemLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.WorkItemLinks
{
    public class CostEstimateItemWorkScheduleStageWorkLinkConfiguration : IEntityTypeConfiguration<CostEstimateItemWorkScheduleStageWorkLink>
    {
        public void Configure(EntityTypeBuilder<CostEstimateItemWorkScheduleStageWorkLink> builder)
        {
            builder.HasKey(l => l.Id);

            builder.Property(l => l.ProjectId).IsRequired();

            builder.Property(l => l.DisplayName)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(l => l.BudgetNet).HasPrecision(18, 4);
            builder.Property(l => l.BudgetGross).HasPrecision(18, 4);

            builder.Property(l => l.IsWorkClosed).IsRequired().HasDefaultValue(false);

            builder.Ignore(l => l.ActualNet);
            builder.Ignore(l => l.ActualGross);
            builder.Ignore(l => l.Variance);

            builder.HasOne(l => l.CostEstimateItem)
                .WithMany(i => i.WorkItemLinks)
                .HasForeignKey(l => l.CostEstimateItemId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(l => l.WorkScheduleStageWork)
                .WithMany(w => w.WorkItemLinks)
                .HasForeignKey(l => l.WorkScheduleStageWorkId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasMany(l => l.TrackedCosts)
                .WithOne(tc => tc.CostEstimateItemWorkScheduleStageWorkLink)
                .HasForeignKey(tc => tc.WorkItemLinkId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(l => new { l.ProjectId, l.CostEstimateItemId });
            builder.HasIndex(l => new { l.ProjectId, l.WorkScheduleStageWorkId });
            builder.HasIndex(l => l.GroupStageLinkId);
        }
    }
}
