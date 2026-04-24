using Entities.Models.CostEstimates;
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

            builder.Property(tc => tc.TenantId).IsRequired();
            builder.Property(tc => tc.ProjectId).IsRequired();
            builder.Property(tc => tc.WorkItemLinkId);
            builder.Property(tc => tc.CostEstimateItemId).IsRequired(false);
            builder.Property(tc => tc.WorkScheduleStageWorkId).IsRequired(false);

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

            builder.Property(tc => tc.CreatedAt).IsRequired();
            builder.Property(tc => tc.UpdatedAt);

            builder.Property(tc => tc.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(tc => tc.DeletedAt);

            builder.HasMany(tc => tc.Attachments)
                .WithOne(a => a.TrackedCost)
                .HasForeignKey(a => a.TrackedCostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(tc => tc.CostEstimateItem)
                .WithMany()
                .HasForeignKey(tc => tc.CostEstimateItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(tc => tc.WorkScheduleStageWork)
                .WithMany()
                .HasForeignKey(tc => tc.WorkScheduleStageWorkId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(tc => new { tc.TenantId, tc.ProjectId });
            builder.HasIndex(tc => tc.WorkItemLinkId);
            builder.HasIndex(tc => tc.CostEstimateItemId);
            builder.HasIndex(tc => tc.WorkScheduleStageWorkId);
            builder.HasIndex(tc => tc.IsDeleted);

            builder.HasQueryFilter(tc => !tc.IsDeleted);
        }
    }
}
