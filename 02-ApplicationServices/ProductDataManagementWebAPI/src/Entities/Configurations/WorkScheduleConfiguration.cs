using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using Entities.Models.CostEstimates;
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
            builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(w => w.DeletedAt);

            builder.HasQueryFilter(w => !w.IsDeleted);

            builder.HasOne(w => w.CostEstimate)
                   .WithMany(c => c.WorkSchedules)
                   .HasForeignKey(w => w.CostEstimateId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

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
            builder.HasIndex(w => new { w.TenantId, w.ProjectId, w.IsDeleted });
            builder.HasIndex(w => w.CostEstimateId);
        }
    }

    public class WorkScheduleStageConfiguration : IEntityTypeConfiguration<WorkScheduleStage>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStage> builder)
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Order).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.ProjectId).IsRequired();
            builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(s => s.DeletedAt);

            builder.HasQueryFilter(s => !s.IsDeleted);

            builder.HasOne(s => s.CostEstimateGroup)
                   .WithMany(g => g.WorkScheduleStages)
                   .HasForeignKey(s => s.CostEstimateGroupId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(s => s.WorkSchedule)
                   .WithMany(w => w.Stages)
                   .HasForeignKey(s => s.WorkScheduleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.ParentStage)
                   .WithMany(s => s.ChildStages)
                   .HasForeignKey(s => s.ParentStageId)
                   .OnDelete(DeleteBehavior.Restrict)
                   .IsRequired(false);

            builder.HasIndex(s => new { s.WorkScheduleId, s.Order });
            builder.HasIndex(s => new { s.WorkScheduleId, s.IsDeleted });
            builder.HasIndex(s => new { s.TenantId, s.ProjectId });
            builder.HasIndex(s => s.ParentStageId);
            builder.HasIndex(s => s.CostEstimateGroupId);
        }
    }

    public class WorkScheduleStageWorkConfiguration : IEntityTypeConfiguration<WorkScheduleStageWork>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWork> builder)
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
            builder.Property(w => w.Order).IsRequired();
            builder.Property(w => w.CreatedAt).IsRequired();
            builder.Property(w => w.ProjectId).IsRequired();
            builder.Property(w => w.ColorRgb).IsRequired().HasMaxLength(20);
            builder.Property(w => w.CostEstimateItemId).IsRequired(false);
            builder.Property(w => w.IsDeleted).IsRequired().HasDefaultValue(false);
            builder.Property(w => w.DeletedAt);

            builder.HasQueryFilter(w => !w.IsDeleted);

            builder.HasOne(w => w.Stage)
                   .WithMany(s => s.Works)
                   .HasForeignKey(w => w.WorkScheduleStageId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(w => w.CostEstimateItem)
                   .WithMany()
                   .HasForeignKey(w => w.CostEstimateItemId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasMany(w => w.Periods)
                   .WithOne(p => p.Work)
                   .HasForeignKey(p => p.WorkScheduleStageWorkId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(w => new { w.WorkScheduleStageId, w.Order });
            builder.HasIndex(w => new { w.TenantId, w.ProjectId });
            builder.HasIndex(w => w.CostEstimateItemId);
            builder.HasIndex(w => new { w.WorkScheduleStageId, w.IsDeleted });
            builder.HasIndex(w => new { w.TenantId, w.ProjectId, w.IsDeleted });
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

            builder.HasOne(a => a.Tenant)
                   .WithMany()
                   .HasForeignKey(a => a.TenantId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Project)
                   .WithMany()
                   .HasForeignKey(a => a.ProjectId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.TenantMember)
                   .WithMany()
                   .HasForeignKey(a => new { a.TenantId, a.UserId })
                   .HasPrincipalKey(tm => new { tm.TenantId, tm.UserId })
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class WorkScheduleStageWorkCommentConfiguration : IEntityTypeConfiguration<WorkScheduleStageWorkComment>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWorkComment> builder)
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Content).IsRequired().HasMaxLength(2000);
            builder.Property(c => c.CreatedAt).IsRequired();
            builder.Property(c => c.ProjectId).IsRequired();

            builder.HasOne(c => c.Work)
                   .WithMany(w => w.Comments)
                   .HasForeignKey(c => c.WorkScheduleStageWorkId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.CreatedBy)
                   .WithMany()
                   .HasForeignKey(c => c.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(c => new { c.WorkScheduleStageWorkId, c.CreatedAt });
            builder.HasIndex(c => new { c.TenantId, c.ProjectId });
        }
    }

    public class WorkScheduleStageWorkDependencyConfiguration : IEntityTypeConfiguration<WorkScheduleStageWorkDependency>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWorkDependency> builder)
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.TenantId).IsRequired();
            builder.Property(d => d.ProjectId).IsRequired();
            builder.Property(d => d.WorkScheduleId).IsRequired();
            builder.Property(d => d.PredecessorWorkId).IsRequired();
            builder.Property(d => d.SuccessorWorkId).IsRequired();
            builder.Property(d => d.DependencyType).IsRequired();
            builder.Property(d => d.LagDays).IsRequired().HasDefaultValue(0);

            builder.HasOne(d => d.WorkSchedule)
                   .WithMany(ws => ws.Dependencies)
                   .HasForeignKey(d => d.WorkScheduleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.PredecessorWork)
                   .WithMany(w => w.PredecessorDependencies)
                   .HasForeignKey(d => d.PredecessorWorkId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.SuccessorWork)
                   .WithMany(w => w.SuccessorDependencies)
                   .HasForeignKey(d => d.SuccessorWorkId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Prevent duplicate dependency between the same pair
            builder.HasIndex(d => new { d.WorkScheduleId, d.PredecessorWorkId, d.SuccessorWorkId, d.DependencyType })
                   .IsUnique();

            builder.HasIndex(d => new { d.TenantId, d.WorkScheduleId });
            builder.HasIndex(d => new { d.TenantId, d.ProjectId });
            builder.HasIndex(d => d.PredecessorWorkId);
            builder.HasIndex(d => d.SuccessorWorkId);
        }
    }

    public class WorkScheduleStageWorkPeriodConfiguration : IEntityTypeConfiguration<WorkScheduleStageWorkPeriod>
    {
        public void Configure(EntityTypeBuilder<WorkScheduleStageWorkPeriod> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.TenantId).IsRequired();
            builder.Property(p => p.ProjectId).IsRequired();
            builder.Property(p => p.WorkScheduleStageWorkId).IsRequired();
            builder.Property(p => p.StartDate).IsRequired();
            builder.Property(p => p.EndDate).IsRequired();
            builder.Property(p => p.IsClosed).IsRequired().HasDefaultValue(false);

            builder.HasIndex(p => new { p.WorkScheduleStageWorkId, p.StartDate });
            builder.HasIndex(p => new { p.TenantId, p.ProjectId });
        }
    }
}
