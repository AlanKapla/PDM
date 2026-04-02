using Business.Interfaces.Constants;
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
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly INotificationSender notificationSender;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICostEstimateAccessService costEstimateAccessService;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IReadRepository<User> userRepo,
            IReadRepository<Notification> notificationRepo,
            INotificationSender notificationSender,
            IWorkScheduleSyncService workScheduleSyncService,
            ICostEstimateAccessService costEstimateAccessService,
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
            this.workScheduleSyncService = workScheduleSyncService;
            this.costEstimateAccessService = costEstimateAccessService;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            if (tenantId != currentUser.ActiveTenantId)
                throw new ForbiddenApiException("Access to this tenant is not allowed.");
            WorkSchedule workSchedule = new WorkSchedule
            {
                TenantId = tenantId,
                ProjectId = projectId,
                CostEstimateId = request.CostEstimateId,
                Name = request.Name,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            await workScheduleRepo.Insert(workSchedule);
            await workScheduleRepo.SaveChangesAsync();

            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            // Auto-generate stages from cost estimate when CostEstimateId is provided
            if (request.CostEstimateId.HasValue)
            {
                var accessLevel = await costEstimateAccessService.GetAccessLevelAsync(
                    currentUser, tenantId, projectId, request.CostEstimateId.Value, cancellationToken);

                if (accessLevel < CostEstimateAccessLevel.Full)
                {
                    throw new ForbiddenApiException("You do not have full access to the linked cost estimate.");
                }

                var syncedStages = await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
                var syncedStageWebs = BuildStageTree(syncedStages, null);

                return new WorkScheduleDetailsWeb(
                    workSchedule.Id,
                    workSchedule.TenantId,
                    workSchedule.ProjectId,
                    workSchedule.CostEstimateId,
                    workSchedule.Name,
                    workSchedule.CreatedAt,
                    workSchedule.CreatedByUserId,
                    createdByUserName,
                    syncedStageWebs);
            }

            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();
            Dictionary<Guid, string> userNameDict = new Dictionary<Guid, string>();

            // Only process stages if they exist
            if (request.Stages != null && request.Stages.Count > 0)
            {
                // Collect all unique user IDs from all works across all stages (including nested)
                var allUserIds = WorkScheduleValidationHelper.FlattenStages(request.Stages)
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

                // Create stages and works recursively (supporting nested child stages)
                stageWebs = await CreateStagesAsync(
                    request.Stages, null,
                    tenantId, projectId, workSchedule.Id,
                    userNameDict, cancellationToken);
            }

            // Get creator information from ICurrentUser (already set above)

            // Only send notifications if there are stages with assigned users
            if (request.Stages != null && request.Stages.Count > 0)
            {
                HashSet<Guid> allAssignedUsers = WorkScheduleValidationHelper.FlattenStages(request.Stages)
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

                        var payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                        await notificationSender.EnqueueAsync(payload, cancellationToken);
                    }
                }
            }

            return new WorkScheduleDetailsWeb(
                workSchedule.Id,
                workSchedule.TenantId,
                workSchedule.ProjectId,
                workSchedule.CostEstimateId,
                workSchedule.Name,
                workSchedule.CreatedAt,
                workSchedule.CreatedByUserId,
                createdByUserName,
                stageWebs
            );
        }

        private async Task<List<WorkScheduleStageWeb>> CreateStagesAsync(
            List<WorkScheduleStageDto> stageDtos,
            Guid? parentStageId,
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            Dictionary<Guid, string> userNameDict,
            CancellationToken cancellationToken)
        {
            var result = new List<WorkScheduleStageWeb>();

            foreach (WorkScheduleStageDto stageDto in stageDtos)
            {
                WorkScheduleStage stage = new WorkScheduleStage
                {
                    TenantId = tenantId,
                    ProjectId = projectId,
                    WorkScheduleId = workScheduleId,
                    ParentStageId = parentStageId,
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
                        List<WorkScheduleStageWorkPeriod> periods = workDto.Periods?.Select(p => new WorkScheduleStageWorkPeriod
                        {
                            StartDate = p.StartDate,
                            EndDate = p.EndDate,
                            IsClosed = workDto.IsClosed ? true : p.IsClosed
                        }).ToList() ?? new List<WorkScheduleStageWorkPeriod>();

                        bool isWorkClosed = false;
                        if (workDto.IsClosed)
                        {
                            foreach (var period in periods)
                                period.IsClosed = true;
                            isWorkClosed = true;
                        }
                        else if (periods.Any() && periods.All(p => p.IsClosed))
                        {
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
                        await workRepo.SaveChangesAsync(cancellationToken);

                        List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();
                        List<WorkScheduleStageWorkAssignment> assignments = new List<WorkScheduleStageWorkAssignment>();

                        if (workDto.AssignedUserIds != null)
                        {
                            foreach (Guid userId in workDto.AssignedUserIds)
                            {
                                assignments.Add(new WorkScheduleStageWorkAssignment
                                {
                                    WorkScheduleStageWorkId = work.Id,
                                    TenantId = tenantId,
                                    ProjectId = projectId,
                                    UserId = userId
                                });
                                string userName = userNameDict.GetValueOrDefault(userId, "Unknown User");
                                assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(userId, userName));
                            }
                        }

                        if (assignments.Any())
                            await assignmentRepo.InsertRange(assignments);

                        List<WorkScheduleStageWorkCommentWeb> commentWebs = new List<WorkScheduleStageWorkCommentWeb>();
                        List<WorkScheduleStageWorkComment> comments = new List<WorkScheduleStageWorkComment>();

                        if (workDto.Comments != null)
                        {
                            DateTime now = DateTime.UtcNow;
                            string commenterName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
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
                                commentWebs.Add(new WorkScheduleStageWorkCommentWeb(
                                    comment.Id, comment.Content, comment.CreatedByUserId,
                                    commenterName, comment.CreatedAt));
                            }
                        }

                        if (comments.Any())
                            await commentRepo.InsertRange(comments);

                        List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                            .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                            .ToList();

                        workWebs.Add(new WorkScheduleStageWorkWeb(
                            work.Id, work.Name, work.Order, work.ColorRgb,
                            work.IsClosed, periodWebs, assigneeWebs, commentWebs));
                    }
                }

                List<WorkScheduleStageWeb> childWebs = stageDto.Children != null && stageDto.Children.Count > 0
                    ? await CreateStagesAsync(stageDto.Children, stage.Id, tenantId, projectId, workScheduleId, userNameDict, cancellationToken)
                    : new List<WorkScheduleStageWeb>();

                result.Add(new WorkScheduleStageWeb(
                    stage.Id, stage.Name, stage.Order, parentStageId, null,
                    workWebs, childWebs));
            }

            return result;
        }

        private static List<WorkScheduleStageWeb> BuildStageTree(List<WorkScheduleStage> stages, Guid? parentStageId)
        {
            return stages
                .Where(s => s.ParentStageId == parentStageId && !s.IsDeleted)
                .OrderBy(s => s.Order)
                .Select(s => new WorkScheduleStageWeb(
                    s.Id,
                    s.Name,
                    s.Order,
                    s.ParentStageId,
                    s.CostEstimateGroupId,
                    new List<WorkScheduleStageWorkWeb>(),
                    BuildStageTree(stages, s.Id)))
                .ToList();
        }
    }
}
