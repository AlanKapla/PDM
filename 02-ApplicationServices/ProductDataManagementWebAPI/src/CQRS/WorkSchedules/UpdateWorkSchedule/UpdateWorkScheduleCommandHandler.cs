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

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandHandler : IRequestHandler<UpdateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateWorkScheduleCommandHandler(
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

        public async Task<WorkScheduleDetailsWeb> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            // Load existing work schedule with all relations including periods
            WorkSchedule workSchedule = (await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId && ws.CreatedByUserId == currentUser.Id,
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Periods),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments)))!;

            // Update work schedule name
            workSchedule.Name = request.Name;
            await workScheduleRepo.Update(workSchedule);
            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            // Track assignment changes for notifications
            HashSet<Guid> removedUserIds = new HashSet<Guid>();
            HashSet<Guid> addedUserIds = new HashSet<Guid>();

            // Get existing stage IDs, work IDs, and assignment user IDs
            var existingStageIds = workSchedule.Stages.Select(s => s.Id).ToHashSet();
            var existingWorkIds = workSchedule.Stages.SelectMany(s => s.Works).Select(w => w.Id).ToHashSet();
            var existingAssignments = workSchedule.Stages
                .SelectMany(s => s.Works)
                .SelectMany(w => w.Assignments)
                .ToList();

            // Track all existing user assignments
            var existingUserIdsByWork = existingAssignments
                .GroupBy(a => a.WorkScheduleStageWorkId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).ToHashSet());

            // Process incoming stages
            var incomingStageIds = request.Stages.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            var incomingWorkIds = request.Stages
                .SelectMany(s => s.Works)
                .Where(w => w.Id.HasValue)
                .Select(w => w.Id!.Value)
                .ToHashSet();

            // Delete stages that are not in the incoming request
            var stagesToDelete = workSchedule.Stages.Where(s => !incomingStageIds.Contains(s.Id)).ToList();
            foreach (var stage in stagesToDelete)
            {
                // Track removed users from deleted works
                foreach (var work in stage.Works)
                {
                    foreach (var assignment in work.Assignments)
                    {
                        removedUserIds.Add(assignment.UserId);
                    }
                }
                await stageRepo.Delete(stage);
                await stageRepo.SaveChangesAsync(cancellationToken);
            }

            // Delete works that are not in the incoming request
            var worksToDelete = workSchedule.Stages
                .SelectMany(s => s.Works)
                .Where(w => !incomingWorkIds.Contains(w.Id))
                .ToList();
            foreach (var work in worksToDelete)
            {
                // Track removed users from deleted works
                foreach (var assignment in work.Assignments)
                {
                    removedUserIds.Add(assignment.UserId);
                }
                await workRepo.Delete(work);
                await workRepo.SaveChangesAsync(cancellationToken);
            }

            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();

            // Process stages
            foreach (UpdateStageDto stageDto in request.Stages)
            {
                WorkScheduleStage stage;

                if (stageDto.Id.HasValue)
                {
                    // Update existing stage
                    stage = workSchedule.Stages.First(s => s.Id == stageDto.Id.Value);
                    stage.Name = stageDto.Name;
                    stage.Order = stageDto.Order;
                    await stageRepo.Update(stage);
                }
                else
                {
                    // Create new stage
                    stage = new WorkScheduleStage
                    {
                        TenantId = tenantId,
                        WorkScheduleId = workSchedule.Id,
                        Name = stageDto.Name,
                        Order = stageDto.Order
                    };
                    await stageRepo.Insert(stage);
                    await stageRepo.SaveChangesAsync(cancellationToken);
                }

                List<WorkScheduleStageWorkWeb> workWebs = new List<WorkScheduleStageWorkWeb>();

                // Process works
                foreach (UpdateWorkDto workDto in stageDto.Works)
                {
                    WorkScheduleStageWork work;
                    HashSet<Guid> previousAssignedUsers = new HashSet<Guid>();

                    if (workDto.Id.HasValue)
                    {
                        // Update existing work
                        work = stage.Works.First(w => w.Id == workDto.Id.Value);
                        work.Name = workDto.Name;
                        work.Order = workDto.Order;
                        work.ColorRgb = workDto.ColorRgb;
                        work.IsClosed = workDto.IsClosed;
                        
                        // Clear and rebuild periods collection
                        work.Periods.Clear();
                        work.Periods = workDto.Periods.Select(p => new WorkScheduleStageWorkPeriod
                        {
                            StartDate = p.StartDate,
                            EndDate = p.EndDate
                        }).ToList();

                        await workRepo.Update(work);

                        // Track previous assignments
                        if (existingUserIdsByWork.TryGetValue(work.Id, out var previousUsers))
                        {
                            previousAssignedUsers = previousUsers;
                        }

                        // Delete existing assignments for this work
                        var existingWorkAssignments = work.Assignments.ToList();
                        foreach (var assignment in existingWorkAssignments)
                        {
                            await assignmentRepo.Delete(assignment);
                            await assignmentRepo.SaveChangesAsync(cancellationToken);
                        }
                    }
                    else
                    {
                        // Create new work
                        work = new WorkScheduleStageWork
                        {
                            TenantId = tenantId,
                            WorkScheduleStageId = stage.Id,
                            Name = workDto.Name,
                            Order = workDto.Order,
                            ColorRgb = workDto.ColorRgb,
                            IsClosed = workDto.IsClosed,
                            Periods = workDto.Periods.Select(p => new WorkScheduleStageWorkPeriod
                            {
                                StartDate = p.StartDate,
                                EndDate = p.EndDate
                            }).ToList()
                        };
                        await workRepo.Insert(work);
                        await workRepo.SaveChangesAsync(cancellationToken);
                    }

                    // Create new assignments
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
                        await assignmentRepo.SaveChangesAsync(cancellationToken);

                        // Track assignment changes
                        if (!previousAssignedUsers.Contains(userId))
                        {
                            addedUserIds.Add(userId);
                        }

                        // Find user info for response
                        TenantMember? tenantMember = await tenantMemberRepo.GetFirstBySearch(
                            tm => tm.TenantId == tenantId && tm.UserId == userId,
                            include => include.Include(tm => tm.User));

                        string userName = tenantMember != null 
                            ? $"{tenantMember.User.FirstName} {tenantMember.User.LastName}".Trim() 
                            : "Unknown User";

                        assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(userId, userName));
                    }

                    // Track removed users from this work
                    var currentAssignedUsers = workDto.AssignedUserIds.ToHashSet();
                    foreach (var userId in previousAssignedUsers)
                    {
                        if (!currentAssignedUsers.Contains(userId))
                        {
                            removedUserIds.Add(userId);
                        }
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

            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            // Get creator information
            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Remove users who are both removed and added (net zero change)
            removedUserIds.ExceptWith(addedUserIds);

            // Send notifications to removed users
            foreach (Guid userId in removedUserIds)
            {
                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ProjectId = projectId,
                    UserId = userId,
                    Type = NotificationType.Info,
                    Title = "Usunięto z harmonogramu prac",
                    Message = $"Zostałeś usunięty z prac w harmonogramie: {request.Name}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "workScheduleId", workSchedule.Id },
                        { "workScheduleName", request.Name },
                        { "projectId", projectId },
                        { "updatedByUserId", currentUser.Id },
                        { "updatedByUserName", createdByUserName }
                    }
                };

                await notificationSender.EnqueueAsync(notification, cancellationToken);
            }

            // Send notifications to newly added users
            foreach (Guid userId in addedUserIds)
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
                        { "updatedByUserId", currentUser.Id },
                        { "updatedByUserName", createdByUserName }
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
