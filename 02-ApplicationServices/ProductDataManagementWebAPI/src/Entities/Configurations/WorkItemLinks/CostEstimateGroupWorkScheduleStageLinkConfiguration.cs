using Entities.Models.WorkItemLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.WorkItemLinks
{
    public class CostEstimateGroupWorkScheduleStageLinkConfiguration : IEntityTypeConfiguration<CostEstimateGroupWorkScheduleStageLink>
    {
        public void Configure(EntityTypeBuilder<CostEstimateGroupWorkScheduleStageLink> builder)
        {
            builder.HasKey(l => l.Id);

            // Relacja do WorkScheduleLink jest konfigurowana od strony principal-a
            // w CostEstimateWorkScheduleLinkConfiguration.HasMany(GroupStageLinks) — tutaj nie powielamy.

            builder.HasOne(l => l.CostEstimateGroup)
                .WithMany(g => g.WorkScheduleStageLinks)
                .HasForeignKey(l => l.CostEstimateGroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(l => l.WorkScheduleStage)
                .WithMany(s => s.CostEstimateGroupLinks)
                .HasForeignKey(l => l.WorkScheduleStageId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasMany(l => l.WorkItemLinks)
                .WithOne(w => w.GroupStageLink)
                .HasForeignKey(w => w.GroupStageLinkId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(l => new { l.WorkScheduleLinkId, l.CostEstimateGroupId });
            builder.HasIndex(l => new { l.WorkScheduleLinkId, l.WorkScheduleStageId });
        }
    }
}
