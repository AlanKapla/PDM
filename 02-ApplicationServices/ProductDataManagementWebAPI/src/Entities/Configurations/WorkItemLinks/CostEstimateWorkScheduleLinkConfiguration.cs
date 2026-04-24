using Entities.Models.WorkItemLinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.WorkItemLinks
{
    public class CostEstimateWorkScheduleLinkConfiguration : IEntityTypeConfiguration<CostEstimateWorkScheduleLink>
    {
        public void Configure(EntityTypeBuilder<CostEstimateWorkScheduleLink> builder)
        {
            builder.HasKey(l => l.Id);

            builder.HasOne(l => l.CostEstimate)
                .WithMany(c => c.WorkScheduleLinks)
                .HasForeignKey(l => l.CostEstimateId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasOne(l => l.WorkSchedule)
                .WithMany(w => w.CostEstimateLinks)
                .HasForeignKey(l => l.WorkScheduleId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasMany(l => l.GroupStageLinks)
                .WithOne(g => g.WorkScheduleLink)
                .HasForeignKey(g => g.WorkScheduleLinkId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indeks unikalny dla par kosztorys+harmonogram (filtrowany tylko gdy oba non-null)
            builder.HasIndex(l => new { l.CostEstimateId, l.WorkScheduleId })
                .IsUnique()
                .HasFilter("[CostEstimateId] IS NOT NULL AND [WorkScheduleId] IS NOT NULL");

            builder.HasIndex(l => l.CostEstimateId);
            builder.HasIndex(l => l.WorkScheduleId);
        }
    }
}
