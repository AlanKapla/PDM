using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using Repositiories.Repository.Interfaces;
using CQRS.WorkSchedules.Shared;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IReadRepository<User> userRepo,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.assignmentRepo = assignmentRepo;
            this.commentRepo = commentRepo;
            this.userRepo = userRepo;
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
            Dictionary<Guid, string> userNameDict = new Dictionary<Guid, string>();

            // Only process stages if they exist
            if (request.Stages != null && request.Stages.Count > 0)
            {
                // Collect all unique user IDs from all works across all stages
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

                // Create stages and works
                foreach (WorkScheduleStageDto stageDto in request.Stages)
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

                    if (stageDto.Works != null)
                    {
                        foreach (WorkScheduleWorkDto workDto in stageDto.Works)
                        {
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

                            WorkScheduleStageWork work = new WorkScheduleStageWork
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
                            await workRepo.SaveChangesAsync();

                            // Create assignments in batch
                            List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();
                            List<WorkScheduleStageWorkAssignment> assignments = new List<WorkScheduleStageWorkAssignment>();

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

                                    assignments.Add(assignment);

                                    // Use pre-fetched user name from dictionary
                                    string userName = userNameDict.ContainsKey(userId) 
                                        ? userNameDict[userId] 
                                        : "Unknown User";

                                    assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(userId, userName));
                                }
                            }

                            // Insert all assignments for this work in one batch
                            if (assignments.Any())
                            {
                                await assignmentRepo.InsertRange(assignments);
                            }

                            // Create comments in batch
                            List<WorkScheduleStageWorkCommentWeb> commentWebs = new List<WorkScheduleStageWorkCommentWeb>();
                            List<WorkScheduleStageWorkComment> comments = new List<WorkScheduleStageWorkComment>();

                            if (workDto.Comments != null)
                            {
                                DateTime now = DateTime.UtcNow;
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

                                    comments.Add(comment);

                                    string userName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

                                    commentWebs.Add(new WorkScheduleStageWorkCommentWeb(
                                        comment.Id,
                                        comment.Content,
                                        comment.CreatedByUserId,
                                        userName,
                                        comment.CreatedAt
                                    ));
                                }
                            }

                            // Insert all comments for this work in one batch
                            if (comments.Any())
                            {
                                await commentRepo.InsertRange(comments);
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
            }

            // Get creator information from ICurrentUser
            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Only send notifications if there are stages with assigned users
            if (request.Stages != null && request.Stages.Count > 0)
            {
                HashSet<Guid> allAssignedUsers = request.Stages
                    .Where(s => s.Works != null)
                    .SelectMany(s => s.Works!)
                    .Where(w => w.AssignedUserIds != null)
                    .SelectMany(w => w.AssignedUserIds!)
                    .Distinct()
                    .ToHashSet();

                if (allAssignedUsers.Any())
                {
                    var notificationUsers = await userRepo.GetBySearch(u => allAssignedUsers.Contains(u.Id));
                    var userDict = notificationUsers.ToDictionary(u => u.Id);

                    foreach (Guid userId in allAssignedUsers)
                    {
                        userDict.TryGetValue(userId, out User? targetUser);

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
                                { "createdByUserId", currentUser.Id },
                                { "createdByUserName", createdByUserName }
                            }
                        };

                        await notificationSender.EnqueueAsync(notification, cancellationToken);
                    }
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
