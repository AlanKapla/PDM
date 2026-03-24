using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using CQRS.WorkSchedules.Shared;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandHandler : IRequestHandler<UpdateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.assignmentRepo = assignmentRepo;
            this.commentRepo = commentRepo;
            this.userRepo = userRepo;
            this.notificationRepo = notificationRepo;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            // 1. Load existing work schedule with all relations
            WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == request.WorkScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId,
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Periods),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Assignments),
                include => include
                    .Include(ws => ws.Stages)
                        .ThenInclude(s => s.Works)
                            .ThenInclude(w => w.Comments)
                                .ThenInclude(c => c.CreatedBy))
                ?? throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());

            // 2. Authorization check: tenant admin OR project admin OR work schedule owner
            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);
            bool isOwner = workSchedule.CreatedByUserId == currentUser.Id;
            
            if (!isAdmin && !isOwner)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), request.WorkScheduleId.ToString());
            }

            // 3. Update work schedule name
            workSchedule.Name = request.Name;
            await workScheduleRepo.Update(workSchedule);

            // Track assignment changes for notifications
            HashSet<Guid> removedUserIds = new HashSet<Guid>();
            HashSet<Guid> addedUserIds = new HashSet<Guid>();

            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();
            Dictionary<Guid, string> userNameDict = new Dictionary<Guid, string>();

            // Only process stages if they exist
            if (request.Stages != null && request.Stages.Count > 0)
            {
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
                    .Where(s => s.Works != null)
                    .SelectMany(s => s.Works!)
                    .Where(w => w.Id.HasValue)
                    .Select(w => w.Id!.Value)
                    .ToHashSet();

                // Collect all stages and works to delete
                var stagesToDelete = workSchedule.Stages.Where(s => !incomingStageIds.Contains(s.Id)).ToList();
                var worksToDelete = workSchedule.Stages
                    .SelectMany(s => s.Works)
                    .Where(w => !incomingWorkIds.Contains(w.Id))
                    .ToList();

                // Track removed users from deleted stages/works
                foreach (var stage in stagesToDelete)
                {
                    foreach (var work in stage.Works)
                    {
                        foreach (var assignment in work.Assignments)
                        {
                            removedUserIds.Add(assignment.UserId);
                        }
                    }
                }

                foreach (var work in worksToDelete)
                {
                    foreach (var assignment in work.Assignments)
                    {
                        removedUserIds.Add(assignment.UserId);
                    }
                }

                // Delete stages and works in batch
                if (stagesToDelete.Any())
                {
                    await stageRepo.DeleteRange(stagesToDelete);
                }

                if (worksToDelete.Any())
                {
                    await workRepo.DeleteRange(worksToDelete);
                }

                // Collect all unique user IDs from incoming works
                var allUserIds = request.Stages
                    .Where(s => s.Works != null)
                    .SelectMany(s => s.Works!)
                    .Where(w => w.AssignedUserIds != null)
                    .SelectMany(w => w.AssignedUserIds!)
                    .Distinct()
                    .ToList();

                // Fetch users directly - no need for TenantMember join just to get names
                if (allUserIds.Any())
                {
                    var users = await userRepo.GetBySearch(u => allUserIds.Contains(u.Id));

                    userNameDict = users.ToDictionary(
                        u => u.Id,
                        u => $"{u.FirstName} {u.LastName}".Trim()
                    );
                }

                List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete = new List<WorkScheduleStageWorkAssignment>();
                List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert = new List<WorkScheduleStageWorkAssignment>();
                List<WorkScheduleStageWorkComment> allCommentsToDelete = new List<WorkScheduleStageWorkComment>();
                List<WorkScheduleStageWorkComment> allCommentsToInsert = new List<WorkScheduleStageWorkComment>();

                // Process stages
                foreach (WorkScheduleStageDto stageDto in request.Stages)
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
                        await stageRepo.SaveChangesAsync(cancellationToken); // Need ID for works
                    }

                    List<WorkScheduleStageWorkWeb> workWebs = new List<WorkScheduleStageWorkWeb>();

                    // Process works
                    if (stageDto.Works != null)
                    {
                        foreach (WorkScheduleWorkDto workDto in stageDto.Works)
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

                                // Build periods collection
                                List<WorkScheduleStageWorkPeriod> periods = workDto.Periods?.Select(p => new WorkScheduleStageWorkPeriod
                                {
                                    StartDate = p.StartDate,
                                    EndDate = p.EndDate,
                                    IsClosed = workDto.IsClosed ? true : p.IsClosed
                                }).ToList() ?? new List<WorkScheduleStageWorkPeriod>();

                                // Apply automatic closure logic
                                if (workDto.IsClosed)
                                {
                                    // If work is closed, all periods must be closed
                                    foreach (var period in periods)
                                    {
                                        period.IsClosed = true;
                                    }
                                    work.IsClosed = true;
                                }
                                else
                                {
                                    // If all periods are closed, work should be closed
                                    if (periods.Any() && periods.All(p => p.IsClosed))
                                    {
                                        work.IsClosed = true;
                                    }
                                    else
                                    {
                                        work.IsClosed = false;
                                    }
                                }

                                // Clear and rebuild periods collection
                                work.Periods.Clear();
                                work.Periods = periods;

                                await workRepo.Update(work);

                                // Track previous assignments
                                if (existingUserIdsByWork.TryGetValue(work.Id, out var previousUsers))
                                {
                                    previousAssignedUsers = previousUsers;
                                }

                                // Collect existing assignments for deletion
                                var existingWorkAssignments = work.Assignments.ToList();
                                if (existingWorkAssignments.Any())
                                {
                                    allAssignmentsToDelete.AddRange(existingWorkAssignments);
                                }

                                // Handle comments - collect existing comments for deletion
                                var existingWorkComments = work.Comments.ToList();
                                if (existingWorkComments.Any())
                                {
                                    allCommentsToDelete.AddRange(existingWorkComments);
                                }
                            }
                            else
                            {
                                // Create new work
                                // Build periods collection
                                List<WorkScheduleStageWorkPeriod> periods = workDto.Periods?.Select(p => new WorkScheduleStageWorkPeriod
                                {
                                    StartDate = p.StartDate,
                                    EndDate = p.EndDate,
                                    IsClosed = workDto.IsClosed ? true : p.IsClosed
                                }).ToList() ?? new List<WorkScheduleStageWorkPeriod>();

                                // Determine work closure status
                                bool isWorkClosed = false;
                                if (workDto.IsClosed)
                                {
                                    // If work is closed, all periods must be closed
                                    foreach (var period in periods)
                                    {
                                        period.IsClosed = true;
                                    }
                                    isWorkClosed = true;
                                }
                                else if (periods.Any() && periods.All(p => p.IsClosed))
                                {
                                    // If all periods are closed, work should be closed
                                    isWorkClosed = true;
                                }

                                work = new WorkScheduleStageWork
                                {
                                    TenantId = tenantId,
                                    WorkScheduleStageId = stage.Id,
                                    Name = workDto.Name,
                                    Order = workDto.Order,
                                    ColorRgb = workDto.ColorRgb,
                                    IsClosed = isWorkClosed,
                                    Periods = periods
                                };
                                await workRepo.Insert(work);
                                await workRepo.SaveChangesAsync(cancellationToken); // Need ID for assignments
                            }

                            // Prepare new assignments
                            List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();

                            if (workDto.AssignedUserIds != null)
                            {
                                foreach (Guid userId in workDto.AssignedUserIds)
                                {
                                    WorkScheduleStageWorkAssignment assignment = new WorkScheduleStageWorkAssignment
                                    {
                                        WorkScheduleStageWorkId = work.Id,
                                        TenantId = tenantId,
                                        ProjectId = projectId,
                                        UserId = userId
                                    };

                                    allAssignmentsToInsert.Add(assignment);

                                    // Track assignment changes
                                    if (!previousAssignedUsers.Contains(userId))
                                    {
                                        addedUserIds.Add(userId);
                                    }

                                    // Use pre-fetched user name from dictionary
                                    string userName = userNameDict.ContainsKey(userId) 
                                        ? userNameDict[userId] 
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
                            }
                            else
                            {
                                // All previous users were removed if no assigned users provided
                                foreach (var userId in previousAssignedUsers)
                                {
                                    removedUserIds.Add(userId);
                                }
                            }

                            // Prepare new comments
                            List<WorkScheduleStageWorkCommentWeb> commentWebs = new List<WorkScheduleStageWorkCommentWeb>();

                            if (workDto.Comments != null)
                            {
                                DateTime now = DateTime.UtcNow;
                                string userName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

                                foreach (WorkScheduleWorkCommentDto commentDto in workDto.Comments)
                                {
                                    WorkScheduleStageWorkComment comment = new WorkScheduleStageWorkComment
                                    {
                                        WorkScheduleStageWorkId = work.Id,
                                        TenantId = tenantId,
                                        Content = commentDto.Content,
                                        CreatedByUserId = currentUser.Id,
                                        CreatedAt = now
                                    };

                                    allCommentsToInsert.Add(comment);

                                    commentWebs.Add(new WorkScheduleStageWorkCommentWeb(
                                        comment.Id,
                                        comment.Content,
                                        comment.CreatedByUserId,
                                        userName,
                                        comment.CreatedAt
                                    ));
                                }
                            }

                            List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                                .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                                .ToList();

                            workWebs.Add(new WorkScheduleStageWorkWeb(
                                work.Id,
                                work.Name,
                                work.Order,
                                work.ColorRgb,
                                work.IsClosed,
                                periodWebs,
                                assigneeWebs,
                                commentWebs
                            ));
                        }
                    }

                    stageWebs.Add(new WorkScheduleStageWeb(
                        stage.Id,
                        stage.Name,
                        stage.Order,
                        workWebs
                    ));
                }

                // Delete all old assignments in batch
                if (allAssignmentsToDelete.Any())
                {
                    await assignmentRepo.DeleteRange(allAssignmentsToDelete);
                }

                // Insert all new assignments in batch
                if (allAssignmentsToInsert.Any())
                {
                    await assignmentRepo.InsertRange(allAssignmentsToInsert);
                }

                // Delete all old comments in batch
                if (allCommentsToDelete.Any())
                {
                    await commentRepo.DeleteRange(allCommentsToDelete);
                }

                // Insert all new comments in batch
                if (allCommentsToInsert.Any())
                {
                    await commentRepo.InsertRange(allCommentsToInsert);
                }
            }
            else
            {
                // If no stages provided, delete all existing stages
                if (workSchedule.Stages.Any())
                {
                    // Track removed users
                    foreach (var stage in workSchedule.Stages)
                    {
                        foreach (var work in stage.Works)
                        {
                            foreach (var assignment in work.Assignments)
                            {
                                removedUserIds.Add(assignment.UserId);
                            }
                        }
                    }

                    await stageRepo.DeleteRange(workSchedule.Stages.ToList());
                }
            }

            // Save all changes once
            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            // Get creator information
            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Send notifications only if there are changes
            if (removedUserIds.Any() || addedUserIds.Any())
            {
                // Remove users who are both removed and added (net zero change)
                removedUserIds.ExceptWith(addedUserIds);

                var allNotificationUserIds = removedUserIds.Union(addedUserIds).ToList();
                var notificationUsers = await userRepo.GetBySearch(u => allNotificationUserIds.Contains(u.Id));
                var notificationUserDict = notificationUsers.ToDictionary(u => u.Id);

                foreach (Guid userId in removedUserIds)
                {
                    notificationUserDict.TryGetValue(userId, out User? targetUser);

                    NotificationDto notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProjectId = projectId,
                        UserId = userId,
                        AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
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

                    var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                    await notificationSender.EnqueueAsync(payload, cancellationToken);
                }

                foreach (Guid userId in addedUserIds)
                {
                    notificationUserDict.TryGetValue(userId, out User? targetUser);

                    NotificationDto notification = new NotificationDto
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ProjectId = projectId,
                        UserId = userId,
                        AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
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

                    var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                    await notificationSender.EnqueueAsync(payload, cancellationToken);
                }
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
