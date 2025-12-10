using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<TenantMember> tenantMemberRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.assignmentRepo = assignmentRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            // Create work schedule
            WorkSchedule workSchedule = new WorkSchedule
            {
                TenantId = tenantId,
                ProjectId = projectId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await workScheduleRepo.Insert(workSchedule);
            await workScheduleRepo.SaveChangesAsync();

            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();

            // Create stages and works
            foreach (CreateStageDto stageDto in request.Stages)
            {
                WorkScheduleStage stage = new WorkScheduleStage
                {
                    TenantId = tenantId,
                    WorkScheduleId = workSchedule.Id,
                    Name = stageDto.Name,
                    Order = stageDto.Order
                };

                await stageRepo.Insert(stage);
                await stageRepo.SaveChangesAsync();

                List<WorkScheduleStageWorkWeb> workWebs = new List<WorkScheduleStageWorkWeb>();

                foreach (CreateWorkDto workDto in stageDto.Works)
                {
                    WorkScheduleStageWork work = new WorkScheduleStageWork
                    {
                        TenantId = tenantId,
                        WorkScheduleStageId = stage.Id,
                        Name = workDto.Name,
                        Order = workDto.Order,
                        ColorRgb = workDto.ColorRgb,
                        IsClosed = false,
                        Periods = workDto.Periods.Select(p => new WorkScheduleStageWorkPeriod
                        {
                            StartDate = p.StartDate,
                            EndDate = p.EndDate
                        }).ToList()
                    };

                    await workRepo.Insert(work);
                    await workRepo.SaveChangesAsync();

                    // Create assignments
                    List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();

                    foreach (Guid userId in workDto.AssignedUserIds)
                    {
                        WorkScheduleStageWorkAssignment assignment = new WorkScheduleStageWorkAssignment
                        {
                            WorkScheduleStageWorkId = work.Id,
                            TenantId = tenantId,
                            ProjectId = projectId,
                            UserId = userId
                        };

                        await assignmentRepo.Insert(assignment);

                        // Find user info for response
                        TenantMember? tenantMember = await tenantMemberRepo.GetFirstBySearch(
                            tm => tm.TenantId == tenantId && tm.UserId == userId,
                            include => include.Include(tm => tm.User));

                        string userName = tenantMember != null 
                            ? $"{tenantMember.User.FirstName} {tenantMember.User.LastName}".Trim() 
                            : "Unknown User";

                        assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(userId, userName));
                    }

                    List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                        .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate))
                        .ToList();

                    workWebs.Add(new WorkScheduleStageWorkWeb(
                        work.Id,
                        work.Name,
                        work.Order,
                        work.ColorRgb,
                        work.IsClosed,
                        periodWebs,
                        assigneeWebs
                    ));
                }

                stageWebs.Add(new WorkScheduleStageWeb(
                    stage.Id,
                    stage.Name,
                    stage.Order,
                    workWebs
                ));
            }

            // Get creator information from ICurrentUser
            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Collect all unique assigned users for notifications
            HashSet<Guid> allAssignedUsers = request.Stages
                .SelectMany(s => s.Works)
                .SelectMany(w => w.AssignedUserIds)
                .Distinct()
                .ToHashSet();

            // Send notification to each assigned user
            foreach (Guid userId in allAssignedUsers)
            {
                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProjectId = projectId,
                    UserId = userId,
                    Type = NotificationType.Info,
                    Title = "Przypisano do harmonogramu prac",
                    Message = $"Zostałeś przypisany do prac w harmonogramie: {request.Name}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "workScheduleId", workSchedule.Id },
                        { "workScheduleName", request.Name },
                        { "projectId", projectId },
                        { "createdByUserId", currentUser.Id },
                        { "createdByUserName", createdByUserName }
                    }
                };

                await notificationSender.EnqueueAsync(notification, cancellationToken);
            }

            return new WorkScheduleDetailsWeb(
                workSchedule.Id,
                workSchedule.TenantId,
                workSchedule.ProjectId,
                workSchedule.Name,
                workSchedule.CreatedAt,
                workSchedule.CreatedByUserId,
                createdByUserName,
                stageWebs
            );
        }
    }
}
