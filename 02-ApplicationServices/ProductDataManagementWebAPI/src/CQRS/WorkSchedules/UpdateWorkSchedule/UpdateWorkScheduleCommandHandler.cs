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
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICostEstimateAccessService costEstimateAccessService;
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

        public async Task<WorkScheduleDetailsWeb> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            WorkSchedule workSchedule = await LoadAndAuthorizeAsync(tenantId, projectId, request.WorkScheduleId, cancellationToken);

            workSchedule.Name = request.Name;
            await workScheduleRepo.Update(workSchedule);

            if (workSchedule.CostEstimateId.HasValue)
            {
                CostEstimateAccessLevel accessLevel = await costEstimateAccessService.GetAccessLevelAsync(
                    currentUser, tenantId, projectId, workSchedule.CostEstimateId.Value, cancellationToken);

                if (accessLevel < CostEstimateAccessLevel.Full)
                {
                    throw new ForbiddenApiException("You do not have full access to the linked cost estimate.");
                }

                return await HandleSyncedUpdateAsync(workSchedule, request, tenantId, projectId, cancellationToken);
            }

            return await ExecuteManualUpdateAsync(workSchedule, request, tenantId, projectId, cancellationToken);
        }

        private async Task<WorkSchedule> LoadAndAuthorizeAsync(
            Guid tenantId, Guid projectId, Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            WorkSchedule? workSchedule = await workScheduleRepo.GetFirstBySearch(
                ws => ws.Id == workScheduleId && ws.TenantId == tenantId && ws.ProjectId == projectId && !ws.IsDeleted,
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
                ?? throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());

            bool isAdmin = await currentUser.IsTenantOrProjectAdminAsync(tenantId, projectId, cancellationToken);

            if (!isAdmin && workSchedule.CreatedByUserId != currentUser.Id)
            {
                throw new NotFoundApiException(nameof(WorkSchedule), workScheduleId.ToString());
            }

            return workSchedule;
        }

        private async Task<WorkScheduleDetailsWeb> ExecuteManualUpdateAsync(
            WorkSchedule workSchedule,
            UpdateWorkScheduleCommand request,
            Guid tenantId, Guid projectId,
            CancellationToken cancellationToken)
        {
            HashSet<Guid> removedUserIds = new HashSet<Guid>();
            HashSet<Guid> addedUserIds = new HashSet<Guid>();
            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();
            Dictionary<Guid, string> userNameDict = new Dictionary<Guid, string>();

            if (request.Stages != null && request.Stages.Count > 0)
            {
                Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork = workSchedule.Stages
                    .SelectMany(s => s.Works)
                    .SelectMany(w => w.Assignments)
                    .GroupBy(a => a.WorkScheduleStageWorkId)
                    .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).ToHashSet());

                Dictionary<Guid, WorkScheduleStage> existingStagesById = workSchedule.Stages.ToDictionary(s => s.Id);
                HashSet<Guid> incomingStageIds = CollectStageIds(request.Stages);
                HashSet<Guid> incomingWorkIds = CollectWorkIds(request.Stages);

                List<WorkScheduleStage> stagesToDelete = workSchedule.Stages.Where(s => !incomingStageIds.Contains(s.Id)).ToList();
                List<WorkScheduleStage> remainingStages = workSchedule.Stages.Except(stagesToDelete).ToList();
                List<WorkScheduleStageWork> worksToDelete = remainingStages
                    .SelectMany(s => s.Works)
                    .Where(w => !incomingWorkIds.Contains(w.Id))
                    .ToList();

                CollectRemovedUserIdsFromStages(stagesToDelete, removedUserIds);
                CollectRemovedUserIdsFromWorks(worksToDelete, removedUserIds);

                if (stagesToDelete.Count != 0)
                {
                    await stageRepo.DeleteRange(stagesToDelete);
                }

                if (worksToDelete.Count != 0)
                {
                    await workRepo.DeleteRange(worksToDelete);
                }

                userNameDict = await BuildUserNameDictAsync(request.Stages, cancellationToken);

                List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete = new List<WorkScheduleStageWorkAssignment>();
                List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert = new List<WorkScheduleStageWorkAssignment>();
                List<WorkScheduleStageWorkComment> allCommentsToDelete = new List<WorkScheduleStageWorkComment>();
                List<WorkScheduleStageWorkComment> allCommentsToInsert = new List<WorkScheduleStageWorkComment>();

                stageWebs = await UpdateStagesAsync(
                    request.Stages, null,
                    workSchedule, existingStagesById, existingUserIdsByWork, userNameDict,
                    tenantId, projectId,
                    allAssignmentsToDelete, allAssignmentsToInsert,
                    allCommentsToDelete, allCommentsToInsert,
                    removedUserIds, addedUserIds, cancellationToken);

                await PersistBulkChangesAsync(allAssignmentsToDelete, allAssignmentsToInsert, allCommentsToDelete, allCommentsToInsert);
            }
            else
            {
                CollectRemovedUserIdsFromStages(workSchedule.Stages, removedUserIds);

                if (workSchedule.Stages.Count != 0)
                {
                    await stageRepo.DeleteRange(workSchedule.Stages.ToList());
                }
            }

            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            await SendAssignmentNotificationsAsync(
                removedUserIds, addedUserIds,
                workSchedule.Id, request.Name,
                tenantId, projectId, cancellationToken);

            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            return new WorkScheduleDetailsWeb(
                workSchedule.Id, workSchedule.TenantId, workSchedule.ProjectId,
                workSchedule.CostEstimateId, workSchedule.Name, workSchedule.CreatedAt,
                workSchedule.CreatedByUserId, createdByUserName, stageWebs);
        }

        private async Task<Dictionary<Guid, string>> BuildUserNameDictAsync(
            IEnumerable<WorkScheduleStageDto> stages,
            CancellationToken cancellationToken)
        {
            List<Guid> allUserIds = WorkScheduleValidationHelper.FlattenStages(stages)
                .Where(s => s.Works != null)
                .SelectMany(s => s.Works!)
                .Where(w => w.AssignedUserIds != null)
                .SelectMany(w => w.AssignedUserIds!)
                .Distinct()
                .ToList();

            if (allUserIds.Count == 0)
            {
                return new Dictionary<Guid, string>();
            }

            IEnumerable<User> users = await userRepo.GetBySearch(u => allUserIds.Contains(u.Id));
            return users.ToDictionary(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim());
        }

        private async Task SendAssignmentNotificationsAsync(
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            Guid workScheduleId,
            string workScheduleName,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            if (removedUserIds.Count == 0 && addedUserIds.Count == 0)
            {
                return;
            }

            removedUserIds.ExceptWith(addedUserIds);

            List<Guid> allNotificationUserIds = removedUserIds.Union(addedUserIds).ToList();
            if (!allNotificationUserIds.Any())
            {
                return;
            }

            IEnumerable<User> notificationUsers = await userRepo.GetBySearch(u => allNotificationUserIds.Contains(u.Id));
            Dictionary<Guid, User> notificationUserDict = notificationUsers.ToDictionary(u => u.Id);
            string updatedByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            foreach (Guid userId in removedUserIds)
            {
                notificationUserDict.TryGetValue(userId, out User? targetUser);
                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, ProjectId = projectId, UserId = userId,
                    AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                    Type = NotificationType.Info,
                    Title = "Usunięto z harmonogramu prac",
                    Message = $"Zostałeś usunięty z prac w harmonogramie: {workScheduleName}",
                    CreatedAt = DateTimeOffset.UtcNow, Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "workScheduleId", workScheduleId },
                        { "workScheduleName", workScheduleName },
                        { "projectId", projectId },
                        { "updatedByUserId", currentUser.Id },
                        { "updatedByUserName", updatedByUserName }
                    }
                };
                NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }

            foreach (Guid userId in addedUserIds)
            {
                notificationUserDict.TryGetValue(userId, out User? targetUser);
                NotificationDto notification = new NotificationDto
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, ProjectId = projectId, UserId = userId,
                    AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                    Type = NotificationType.Info,
                    Title = "Przypisano do harmonogramu prac",
                    Message = $"Zostałeś przypisany do prac w harmonogramie: {workScheduleName}",
                    CreatedAt = DateTimeOffset.UtcNow, Readed = false,
                    Metadata = new Dictionary<string, object?>
                    {
                        { "workScheduleId", workScheduleId },
                        { "workScheduleName", workScheduleName },
                        { "projectId", projectId },
                        { "updatedByUserId", currentUser.Id },
                        { "updatedByUserName", updatedByUserName }
                    }
                };
                NotificationPayloadDto payload = await NotificationPayloadHelper.CreatePayloadAsync(notification, notificationRepo, cancellationToken);
                await notificationSender.EnqueueAsync(payload, cancellationToken);
            }
        }

        private static HashSet<Guid> CollectStageIds(IEnumerable<WorkScheduleStageDto> stages)
        {
            HashSet<Guid> ids = new HashSet<Guid>();
            foreach (WorkScheduleStageDto s in stages)
            {
                if (s.Id.HasValue)
                {
                    ids.Add(s.Id.Value);
                }

                if (s.Children != null)
                {
                    foreach (Guid id in CollectStageIds(s.Children))
                    {
                        ids.Add(id);
                    }
                }
            }
            return ids;
        }

        private static HashSet<Guid> CollectWorkIds(IEnumerable<WorkScheduleStageDto> stages)
        {
            HashSet<Guid> ids = [];
            foreach (WorkScheduleStageDto s in stages)
            {
                if (s.Works != null)
                {
                    foreach (WorkScheduleWorkDto w in s.Works.Where(w => w.Id.HasValue))
                    {
                        ids.Add(w.Id!.Value);
                    }
                }

                if (s.Children != null)
                {
                    foreach (Guid id in CollectWorkIds(s.Children))
                    {
                        ids.Add(id);
                    }
                }
            }
            return ids;
        }

        private async Task<List<WorkScheduleStageWeb>> UpdateStagesAsync(
            List<WorkScheduleStageDto> stageDtos,
            Guid? parentStageId,
            WorkSchedule workSchedule,
            Dictionary<Guid, WorkScheduleStage> existingStagesById,
            Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork,
            Dictionary<Guid, string> userNameDict,
            Guid tenantId,
            Guid projectId,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert,
            List<WorkScheduleStageWorkComment> allCommentsToDelete,
            List<WorkScheduleStageWorkComment> allCommentsToInsert,
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWeb> result = new List<WorkScheduleStageWeb>();

            foreach (WorkScheduleStageDto stageDto in stageDtos)
            {
                WorkScheduleStage stage = await UpsertStageAsync(
                    stageDto, parentStageId, workSchedule, existingStagesById,
                    tenantId, projectId, cancellationToken);

                List<WorkScheduleStageWorkWeb> workWebs = new List<WorkScheduleStageWorkWeb>();

                if (stageDto.Works != null)
                {
                    foreach (WorkScheduleWorkDto workDto in stageDto.Works)
                    {
                        (WorkScheduleStageWork work, HashSet<Guid> previousAssignedUsers) = await UpsertWorkAsync(
                            workDto, stage, existingUserIdsByWork, tenantId,
                            allAssignmentsToDelete, allCommentsToDelete, cancellationToken);

                        List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = ProcessWorkAssignments(
                            workDto, work, previousAssignedUsers, tenantId, projectId,
                            userNameDict, allAssignmentsToInsert, removedUserIds, addedUserIds);

                        List<WorkScheduleStageWorkCommentWeb> commentWebs = ProcessWorkComments(
                            workDto, work, tenantId, allCommentsToInsert);

                        List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                            .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                            .ToList();

                        workWebs.Add(new WorkScheduleStageWorkWeb(
                            work.Id, work.Name, work.Order, work.ColorRgb,
                            work.IsClosed, periodWebs, assigneeWebs, commentWebs));
                    }
                }

                List<WorkScheduleStageWeb> childWebs = stageDto.Children != null && stageDto.Children.Count > 0
                    ? await UpdateStagesAsync(
                        stageDto.Children, stage.Id, workSchedule,
                        existingStagesById, existingUserIdsByWork, userNameDict,
                        tenantId, projectId,
                        allAssignmentsToDelete, allAssignmentsToInsert,
                        allCommentsToDelete, allCommentsToInsert,
                        removedUserIds, addedUserIds, cancellationToken)
                    : new List<WorkScheduleStageWeb>();

                result.Add(new WorkScheduleStageWeb(
                    stage.Id, stage.Name, stage.Order, parentStageId, null,
                    workWebs, childWebs));
            }

            return result;
        }

        private async Task<WorkScheduleStage> UpsertStageAsync(
            WorkScheduleStageDto stageDto,
            Guid? parentStageId,
            WorkSchedule workSchedule,
            Dictionary<Guid, WorkScheduleStage> existingStagesById,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            if (stageDto.Id.HasValue && existingStagesById.TryGetValue(stageDto.Id.Value, out WorkScheduleStage existingStage))
            {
                if (existingStage.CostEstimateGroupId == null)
                {
                    existingStage.Name = stageDto.Name;
                    existingStage.Order = stageDto.Order;
                    existingStage.ParentStageId = parentStageId;
                    await stageRepo.Update(existingStage);
                }
                return existingStage;
            }

            WorkScheduleStage stage = new WorkScheduleStage
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workSchedule.Id,
                ParentStageId = parentStageId,
                Name = stageDto.Name,
                Order = stageDto.Order
            };
            await stageRepo.Insert(stage);
            await stageRepo.SaveChangesAsync(cancellationToken);
            return stage;
        }

        private static List<WorkScheduleStageWorkPeriod> BuildPeriods(WorkScheduleWorkDto workDto)
        {
            List<WorkScheduleStageWorkPeriod> periods = workDto.Periods?.Select(p => new WorkScheduleStageWorkPeriod
            {
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsClosed = workDto.IsClosed || p.IsClosed
            }).ToList() ?? new List<WorkScheduleStageWorkPeriod>();

            if (workDto.IsClosed)
            {
                foreach (WorkScheduleStageWorkPeriod period in periods)
                {
                    period.IsClosed = true;
                }
            }

            return periods;
        }

        private async Task<(WorkScheduleStageWork work, HashSet<Guid> previousAssignedUsers)> UpsertWorkAsync(
            WorkScheduleWorkDto workDto,
            WorkScheduleStage stage,
            Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork,
            Guid tenantId,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete,
            List<WorkScheduleStageWorkComment> allCommentsToDelete,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWorkPeriod> periods = BuildPeriods(workDto);
            bool isWorkClosed = workDto.IsClosed || (periods.Any() && periods.All(p => p.IsClosed));
            HashSet<Guid> previousAssignedUsers = new HashSet<Guid>();

            WorkScheduleStageWork? existingWork = workDto.Id.HasValue
                ? stage.Works.FirstOrDefault(w => w.Id == workDto.Id.Value)
                : null;

            if (existingWork != null)
            {
                existingWork.Name = workDto.Name;
                existingWork.Order = workDto.Order;
                existingWork.ColorRgb = workDto.ColorRgb;
                existingWork.IsClosed = isWorkClosed;
                existingWork.Periods.Clear();
                existingWork.Periods = periods;
                await workRepo.Update(existingWork);

                if (existingUserIdsByWork.TryGetValue(existingWork.Id, out HashSet<Guid> previousUsers))
                {
                    previousAssignedUsers = previousUsers;
                }

                allAssignmentsToDelete.AddRange(existingWork.Assignments);
                allCommentsToDelete.AddRange(existingWork.Comments);

                return (existingWork, previousAssignedUsers);
            }

            WorkScheduleStageWork newWork = new WorkScheduleStageWork
            {
                TenantId = tenantId,
                WorkScheduleStageId = stage.Id,
                Name = workDto.Name,
                Order = workDto.Order,
                ColorRgb = workDto.ColorRgb,
                IsClosed = isWorkClosed,
                Periods = periods
            };
            await workRepo.Insert(newWork);
            await workRepo.SaveChangesAsync(cancellationToken);
            return (newWork, previousAssignedUsers);
        }

        private static List<WorkScheduleStageWorkAssigneeWeb> ProcessWorkAssignments(
            WorkScheduleWorkDto workDto,
            WorkScheduleStageWork work,
            HashSet<Guid> previousAssignedUsers,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, string> userNameDict,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert,
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds)
        {
            List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();

            if (workDto.AssignedUserIds != null)
            {
                foreach (Guid userId in workDto.AssignedUserIds)
                {
                    allAssignmentsToInsert.Add(new WorkScheduleStageWorkAssignment
                    {
                        WorkScheduleStageWorkId = work.Id,
                        TenantId = tenantId,
                        ProjectId = projectId,
                        UserId = userId
                    });

                    if (!previousAssignedUsers.Contains(userId))
                    {
                        addedUserIds.Add(userId);
                    }

                    assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(
                        userId, userNameDict.GetValueOrDefault(userId, "Unknown User")));
                }

                HashSet<Guid> currentAssignedUsers = workDto.AssignedUserIds.ToHashSet();
                foreach (Guid userId in previousAssignedUsers.Where(id => !currentAssignedUsers.Contains(id)))
                {
                    removedUserIds.Add(userId);
                }
            }
            else
            {
                foreach (Guid userId in previousAssignedUsers)
                {
                    removedUserIds.Add(userId);
                }
            }

            return assigneeWebs;
        }

        private List<WorkScheduleStageWorkCommentWeb> ProcessWorkComments(
            WorkScheduleWorkDto workDto,
            WorkScheduleStageWork work,
            Guid tenantId,
            List<WorkScheduleStageWorkComment> allCommentsToInsert)
        {
            List<WorkScheduleStageWorkCommentWeb> commentWebs = new List<WorkScheduleStageWorkCommentWeb>();

            if (workDto.Comments == null)
            {
                return commentWebs;
            }

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
                allCommentsToInsert.Add(comment);
                commentWebs.Add(new WorkScheduleStageWorkCommentWeb(
                    comment.Id, comment.Content, comment.CreatedByUserId,
                    commenterName, comment.CreatedAt));
            }

            return commentWebs;
        }

        private async Task<List<WorkScheduleStage>> ReloadStagesWithWorksAsync(
            Guid workScheduleId, CancellationToken cancellationToken)
        {
            return (await stageRepo.GetBySearch(
                s => s.WorkScheduleId == workScheduleId && !s.IsDeleted,
                include => include
                    .Include(s => s.Works)
                    .ThenInclude(w => w.Periods),
                include => include
                    .Include(s => s.Works)
                    .ThenInclude(w => w.Assignments),
                include => include
                    .Include(s => s.Works)
                    .ThenInclude(w => w.Comments)))
                .ToList();
        }

        private async Task EnrichUserNameDictAsync(
            List<WorkScheduleStage> stages,
            Dictionary<Guid, string> userNameDict,
            CancellationToken cancellationToken)
        {
            List<Guid> missingUserIds = stages
                .SelectMany(s => s.Works)
                .SelectMany(w => w.Assignments)
                .Select(a => a.UserId)
                .Distinct()
                .Where(id => !userNameDict.ContainsKey(id))
                .ToList();

            if (!missingUserIds.Any())
            {
                return;
            }

            IEnumerable<User> additionalUsers = await userRepo.GetBySearch(u => missingUserIds.Contains(u.Id));
            foreach (User u in additionalUsers)
            {
                userNameDict[u.Id] = $"{u.FirstName} {u.LastName}".Trim();
            }
        }

        private async Task PersistBulkChangesAsync(
            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert,
            List<WorkScheduleStageWorkComment> allCommentsToDelete,
            List<WorkScheduleStageWorkComment> allCommentsToInsert)
        {
            if (allAssignmentsToDelete.Count != 0)
            {
                await assignmentRepo.DeleteRange(allAssignmentsToDelete);
            }

            if (allAssignmentsToInsert.Count != 0)
            {
                await assignmentRepo.InsertRange(allAssignmentsToInsert);
            }

            if (allCommentsToDelete.Count != 0)
            {
                await commentRepo.DeleteRange(allCommentsToDelete);
            }

            if (allCommentsToInsert.Count != 0)
            {
                await commentRepo.InsertRange(allCommentsToInsert);
            }
        }

        private static void CollectRemovedUserIdsFromStages(
            IEnumerable<WorkScheduleStage> stages, HashSet<Guid> removedUserIds)
        {
            foreach (WorkScheduleStage stage in stages)
            {
                CollectRemovedUserIdsFromWorks(stage.Works, removedUserIds);
            }
        }

        private static void CollectRemovedUserIdsFromWorks(
            IEnumerable<WorkScheduleStageWork> works, HashSet<Guid> removedUserIds)
        {
            foreach (WorkScheduleStageWork work in works)
            {
                foreach (WorkScheduleStageWorkAssignment assignment in work.Assignments)
                {
                    removedUserIds.Add(assignment.UserId);
                }
            }
        }

        private async Task<WorkScheduleDetailsWeb> HandleSyncedUpdateAsync(
            WorkSchedule workSchedule,
            UpdateWorkScheduleCommand request,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
            await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);

            List<WorkScheduleStage> currentStages = await ReloadStagesWithWorksAsync(workSchedule.Id, cancellationToken);
            Dictionary<Guid, WorkScheduleStage> stagesById = currentStages.ToDictionary(s => s.Id);

            HashSet<Guid> removedUserIds = new HashSet<Guid>();
            HashSet<Guid> addedUserIds = new HashSet<Guid>();
            Dictionary<Guid, string> userNameDict = new Dictionary<Guid, string>();

            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete = new List<WorkScheduleStageWorkAssignment>();
            List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert = new List<WorkScheduleStageWorkAssignment>();
            List<WorkScheduleStageWorkComment> allCommentsToDelete = new List<WorkScheduleStageWorkComment>();
            List<WorkScheduleStageWorkComment> allCommentsToInsert = new List<WorkScheduleStageWorkComment>();

            if (request.Stages != null && request.Stages.Count > 0)
            {
                userNameDict = await BuildUserNameDictAsync(request.Stages, cancellationToken);

                HashSet<Guid> incomingStageIds = CollectStageIds(request.Stages);
                HashSet<Guid> incomingWorkIds = CollectWorkIds(request.Stages);

                // Stages without CostEstimateGroupId are fully user-managed even in synced mode.
                List<WorkScheduleStage> existingManualStages = currentStages.Where(s => s.CostEstimateGroupId == null).ToList();
                List<WorkScheduleStage> manualStagesToDelete = existingManualStages.Where(s => !incomingStageIds.Contains(s.Id)).ToList();
                CollectRemovedUserIdsFromStages(manualStagesToDelete, removedUserIds);

                if (manualStagesToDelete.Count != 0)
                {
                    await stageRepo.DeleteRange(manualStagesToDelete);
                }

                List<WorkScheduleStage> remainingStages = currentStages.Except(manualStagesToDelete).ToList();
                List<WorkScheduleStageWork> worksToDelete = remainingStages
                    .SelectMany(s => s.Works)
                    .Where(w => w.CostEstimateItemId == null && !incomingWorkIds.Contains(w.Id))
                    .ToList();
                CollectRemovedUserIdsFromWorks(worksToDelete, removedUserIds);

                if (worksToDelete.Count != 0)
                {
                    await workRepo.DeleteRange(worksToDelete);
                }

                Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork = remainingStages
                    .SelectMany(s => s.Works)
                    .Where(w => !worksToDelete.Contains(w))
                    .ToDictionary(w => w.Id, w => w.Assignments.Select(a => a.UserId).ToHashSet());

                Dictionary<Guid, WorkScheduleStage> remainingStagesById = remainingStages.ToDictionary(s => s.Id);

                await UpdateStagesAsync(
                    request.Stages, null, workSchedule,
                    remainingStagesById, existingUserIdsByWork, userNameDict,
                    tenantId, projectId,
                    allAssignmentsToDelete, allAssignmentsToInsert,
                    allCommentsToDelete, allCommentsToInsert,
                    removedUserIds, addedUserIds, cancellationToken);

                await PersistBulkChangesAsync(allAssignmentsToDelete, allAssignmentsToInsert, allCommentsToDelete, allCommentsToInsert);
            }

            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            List<WorkScheduleStage> finalStages = await ReloadStagesWithWorksAsync(workSchedule.Id, cancellationToken);
            await EnrichUserNameDictAsync(finalStages, userNameDict, cancellationToken);

            await SendAssignmentNotificationsAsync(
                removedUserIds, addedUserIds,
                workSchedule.Id, request.Name,
                tenantId, projectId, cancellationToken);

            string createdByUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            return new WorkScheduleDetailsWeb(
                workSchedule.Id, workSchedule.TenantId, workSchedule.ProjectId,
                workSchedule.CostEstimateId, workSchedule.Name, workSchedule.CreatedAt,
                workSchedule.CreatedByUserId, createdByUserName,
                BuildStageTreeWithWorks(finalStages, null, userNameDict));
        }

        private static List<WorkScheduleStageWeb> BuildStageTreeWithWorks(
            List<WorkScheduleStage> stages,
            Guid? parentStageId,
            Dictionary<Guid, string> userNameDict)
        {
            return stages
                .Where(s => s.ParentStageId == parentStageId)
                .OrderBy(s => s.Order)
                .Select(s => new WorkScheduleStageWeb(
                    s.Id,
                    s.Name,
                    s.Order,
                    s.ParentStageId,
                    s.CostEstimateGroupId,
                    s.Works.OrderBy(w => w.Order).Select(w => new WorkScheduleStageWorkWeb(
                        w.Id, w.Name, w.Order, w.ColorRgb, w.IsClosed,
                        w.Periods.OrderBy(p => p.StartDate)
                            .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                            .ToList(),
                        w.Assignments
                            .Select(a => new WorkScheduleStageWorkAssigneeWeb(
                                a.UserId,
                                userNameDict.GetValueOrDefault(a.UserId, "Unknown User")))
                            .ToList(),
                        w.Comments.OrderBy(c => c.CreatedAt)
                            .Select(c => new WorkScheduleStageWorkCommentWeb(
                                c.Id, c.Content, c.CreatedByUserId,
                                userNameDict.GetValueOrDefault(c.CreatedByUserId, "Unknown"),
                                c.CreatedAt))
                            .ToList()
                    )).ToList(),
                    BuildStageTreeWithWorks(stages, s.Id, userNameDict)))
                .ToList();
        }
    }
}
