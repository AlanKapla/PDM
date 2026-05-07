using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostTrackers;
using Entities.Models.Costs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations.Costs
{
    public class BaseCostConfiguration : IEntityTypeConfiguration<BaseCost>
    {
        public void Configure(EntityTypeBuilder<BaseCost> builder)
        {
            builder.ToTable("Costs");

            builder.HasDiscriminator<string>("CostType")
                .HasValue<TrackedCost>("TrackedCost")
                .HasValue<ProjectCost>("ProjectCost");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Tenant)
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Attachments)
                .WithOne(x => x.Cost)
                .HasForeignKey(x => x.CostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Number)
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(2000);

            builder.Property(x => x.Contractor)
                .HasMaxLength(500);

            builder.Property(x => x.Net)
                .HasPrecision(18, 2);

            builder.Property(x => x.Gross)
                .HasPrecision(18, 2);

            builder.Property(x => x.IsDeleted)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(x => x.DeletedAt);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.UpdatedAt);

            builder.HasIndex(x => new { x.TenantId, x.ProjectId });
            builder.HasIndex(x => new { x.TenantId, x.ProjectId, x.IsDeleted });

            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.Property(tc => tc.CostEstimateItemId).IsRequired(false);
            builder.Property(tc => tc.WorkScheduleStageWorkId).IsRequired(false);

            builder.HasOne(tc => tc.CostEstimateItem)
                .WithMany()
                .HasForeignKey(tc => tc.CostEstimateItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne<WorkScheduleStageWork>()
                .WithMany()
                .HasForeignKey(tc => tc.WorkScheduleStageWorkId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(tc => tc.CostEstimateItemId);
            builder.HasIndex(tc => tc.WorkScheduleStageWorkId);
        }
    }
}
