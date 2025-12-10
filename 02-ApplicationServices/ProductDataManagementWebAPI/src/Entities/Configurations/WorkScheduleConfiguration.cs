using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.Configurations
{
    public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
    {
        public void Configure(EntityTypeBuilder<WorkSchedule> builder)
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Name).IsRequired().HasMaxLength(200);

            builder.HasOne(w => w.Project)
                   .WithMany()
                   .HasForeignKey(w => w.ProjectId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.CreatedBy)
                   .WithMany()
                   .HasForeignKey(w => new { w.TenantId, w.CreatedByUserId })
                   .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(w => new { w.TenantId, w.ProjectId });
        }
    }

    public class WorkScheduleStageConfiguration : IEntityTypeConfiguration<WorkScheduleStage>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStage> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Order).IsRequired();

            builder.HasOne(s => s.WorkSchedule)
                   .WithMany(w => w.Stages)
                   .HasForeignKey(s => s.WorkScheduleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.WorkScheduleId, s.Order });
        }
    }

    public class WorkScheduleStageWorkConfiguration : IEntityTypeConfiguration<WorkScheduleStageWork>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWork> builder)
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
            builder.Property(w => w.Order).IsRequired();
            builder.Property(w => w.ColorRgb).IsRequired().HasMaxLength(20);
            builder.Property(w => w.IsClosed).IsRequired().HasDefaultValue(false);

            builder.HasOne(w => w.Stage)
                   .WithMany(s => s.Works)
                   .HasForeignKey(w => w.WorkScheduleStageId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.OwnsMany(w => w.Periods, p =>
            {
                p.Property(period => period.StartDate).IsRequired();
                p.Property(period => period.EndDate).IsRequired();
            });

            builder.HasIndex(w => new { w.WorkScheduleStageId, w.Order });
        }
    }

    public class WorkScheduleStageWorkAssignmentConfiguration : IEntityTypeConfiguration<WorkScheduleStageWorkAssignment>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWorkAssignment> builder)
        {
            builder.HasKey(a => new { a.WorkScheduleStageWorkId, a.TenantId, a.ProjectId, a.UserId });

            builder.HasOne(a => a.Work)
                   .WithMany(w => w.Assignments)
                   .HasForeignKey(a => a.WorkScheduleStageWorkId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.ProjectMember)
                   .WithMany()
                   .HasForeignKey(a => new { a.TenantId, a.ProjectId, a.UserId })
                   .HasPrincipalKey(pm => new { pm.TenantId, pm.ProjectId, pm.UserId })
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
