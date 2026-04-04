using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.WorkSchedules;
using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositories.Repository.Interfaces;
using CQRS.WorkSchedules.Shared;

namespace CQRS.WorkSchedules.UpdateWorkSchedule
{
    public class UpdateWorkScheduleCommandHandler : IRequestHandler<UpdateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IWorkScheduleNotificationService workScheduleNotificationService;
        private readonly ICurrentUser currentUser;

        public UpdateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IReadRepository<User> userRepo,
            IWorkScheduleNotificationService workScheduleNotificationService,
            ICurrentUser currentUser)
        {
            this.workScheduleRepo = workScheduleRepo;
            this.stageRepo = stageRepo;
            this.workRepo = workRepo;
            this.assignmentRepo = assignmentRepo;
            this.commentRepo = commentRepo;
            this.dependencyRepo = dependencyRepo;
            this.userRepo = userRepo;
            this.workScheduleNotificationService = workScheduleNotificationService;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(UpdateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            WorkSchedule workSchedule = await LoadAndAuthorizeAsync(tenantId, projectId, request.WorkScheduleId, cancellationToken);

            workSchedule.Name = request.Name;
            await workScheduleRepo.Update(workSchedule);

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
            Dictionary<Guid, Guid> tempIdToWorkId = new Dictionary<Guid, Guid>();

            if (request.Stages != null && request.Stages.Count > 0)
            {
                Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork = BuildExistingUserIdsByWork(workSchedule.Stages);
                Dictionary<Guid, WorkScheduleStage> existingStagesById = workSchedule.Stages.ToDictionary(s => s.Id);
                HashSet<Guid> incomingStageIds = CollectStageIds(request.Stages);
                HashSet<Guid> incomingWorkIds = CollectWorkIds(request.Stages);

                await DeleteObsoleteStagesAndWorksAsync(workSchedule.Stages, incomingStageIds, incomingWorkIds, workSchedule.Id, removedUserIds, cancellationToken);

                userNameDict = await WorkScheduleQueryHelper.BuildUserNameDictAsync(userRepo, request.Stages, cancellationToken);

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
                    removedUserIds, addedUserIds, tempIdToWorkId, cancellationToken);

                await PersistBulkChangesAsync(allAssignmentsToDelete, allAssignmentsToInsert, allCommentsToDelete, allCommentsToInsert);
            }
            else
            {
                CollectRemovedUserIdsFromStages(workSchedule.Stages, removedUserIds);

                if (workSchedule.Stages.Count != 0)
                {
                    HashSet<Guid> allWorkIds = workSchedule.Stages
                        .SelectMany(s => s.Works)
                        .Select(w => w.Id)
                        .ToHashSet();

                    await DeleteDependenciesForWorksAsync(allWorkIds, workSchedule.Id, cancellationToken);
                    await stageRepo.DeleteRange(workSchedule.Stages.ToList());
                }
            }

            List<WorkScheduleWorkDependencyWeb> depWebs = await ReplaceDependenciesAsync(
                workSchedule, request.Dependencies, tempIdToWorkId, cancellationToken);

            await workScheduleRepo.SaveChangesAsync(cancellationToken);

            await workScheduleNotificationService.SendAssignmentChangedNotificationsAsync(
                removedUserIds, addedUserIds,
                workSchedule.Id, request.Name,
                tenantId, projectId, cancellationToken);

            string createdByUserName = WorkScheduleQueryHelper.FormatFullName(currentUser.FirstName, currentUser.LastName);
            return new WorkScheduleDetailsWeb(
                workSchedule.Id, workSchedule.TenantId, workSchedule.ProjectId,
                workSchedule.CostEstimateId, workSchedule.Name, workSchedule.CreatedAt,
                workSchedule.CreatedByUserId, createdByUserName, stageWebs,
                depWebs);
        }

        private static HashSet<Guid> CollectStageIds(IEnumerable<WorkScheduleStageDto> stages) =>
            WorkScheduleValidationHelper.FlattenStages(stages)
                .Where(s => s.Id.HasValue)
                .Select(s => s.Id!.Value)
                .ToHashSet();

        private static HashSet<Guid> CollectWorkIds(IEnumerable<WorkScheduleStageDto> stages) =>
            WorkScheduleValidationHelper.FlattenStages(stages)
                .Where(s => s.Works != null)
                .SelectMany(s => s.Works!)
                .Where(w => w.Id.HasValue)
                .Select(w => w.Id!.Value)
                .ToHashSet();

        private static Dictionary<Guid, HashSet<Guid>> BuildExistingUserIdsByWork(IEnumerable<WorkScheduleStage> stages) =>
            stages
                .SelectMany(s => s.Works)
                .SelectMany(w => w.Assignments)
                .GroupBy(a => a.WorkScheduleStageWorkId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).ToHashSet());

        private async Task DeleteObsoleteStagesAndWorksAsync(
            IEnumerable<WorkScheduleStage> existingStages,
            HashSet<Guid> incomingStageIds,
            HashSet<Guid> incomingWorkIds,
            Guid workScheduleId,
            HashSet<Guid> removedUserIds,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStage> stagesToDelete = existingStages.Where(s => !incomingStageIds.Contains(s.Id)).ToList();
            List<WorkScheduleStage> remainingStages = existingStages.Except(stagesToDelete).ToList();
            List<WorkScheduleStageWork> worksToDelete = remainingStages
                .SelectMany(s => s.Works)
                .Where(w => !incomingWorkIds.Contains(w.Id))
                .ToList();

            CollectRemovedUserIdsFromStages(stagesToDelete, removedUserIds);
            CollectRemovedUserIdsFromWorks(worksToDelete, removedUserIds);

            HashSet<Guid> deletedWorkIds = stagesToDelete
                .SelectMany(s => s.Works)
                .Select(w => w.Id)
                .Concat(worksToDelete.Select(w => w.Id))
                .ToHashSet();

            await DeleteDependenciesForWorksAsync(deletedWorkIds, workScheduleId, cancellationToken);

            if (stagesToDelete.Count != 0)
                await stageRepo.DeleteRange(stagesToDelete);

            if (worksToDelete.Count != 0)
                await workRepo.DeleteRange(worksToDelete);
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
            Dictionary<Guid, Guid> tempIdToWorkId,
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
                        workWebs.Add(await ProcessWorkInUpdateAsync(
                            workDto, stage, existingUserIdsByWork, tenantId, projectId, userNameDict,
                            allAssignmentsToDelete, allAssignmentsToInsert,
                            allCommentsToDelete, allCommentsToInsert,
                            removedUserIds, addedUserIds, tempIdToWorkId, cancellationToken));
                    }
                }

                List<WorkScheduleStageWeb> childWebs = stageDto.Children != null && stageDto.Children.Count > 0
                    ? await UpdateStagesAsync(
                        stageDto.Children, stage.Id, workSchedule,
                        existingStagesById, existingUserIdsByWork, userNameDict,
                        tenantId, projectId,
                        allAssignmentsToDelete, allAssignmentsToInsert,
                        allCommentsToDelete, allCommentsToInsert,
                        removedUserIds, addedUserIds, tempIdToWorkId, cancellationToken)
                    : new List<WorkScheduleStageWeb>();

                result.Add(new WorkScheduleStageWeb(
                    stage.Id, stage.Name, stage.Order, parentStageId, null,
                    workWebs, childWebs));
            }

            return result;
        }

        private async Task<WorkScheduleStageWorkWeb> ProcessWorkInUpdateAsync(
            WorkScheduleWorkDto workDto,
            WorkScheduleStage stage,
            Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, string> userNameDict,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToInsert,
            List<WorkScheduleStageWorkComment> allCommentsToDelete,
            List<WorkScheduleStageWorkComment> allCommentsToInsert,
            HashSet<Guid> removedUserIds,
            HashSet<Guid> addedUserIds,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            (WorkScheduleStageWork work, HashSet<Guid> previousAssignedUsers) = await UpsertWorkAsync(
                workDto, stage, existingUserIdsByWork, tenantId, projectId,
                allAssignmentsToDelete, allCommentsToDelete, cancellationToken);

            if (!workDto.Id.HasValue && workDto.TempId.HasValue)
                tempIdToWorkId[workDto.TempId.Value] = work.Id;

            List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = ProcessWorkAssignments(
                workDto, work, previousAssignedUsers, tenantId, projectId,
                userNameDict, allAssignmentsToInsert, removedUserIds, addedUserIds);

            List<WorkScheduleStageWorkCommentWeb> commentWebs = ProcessWorkComments(
                workDto, work, tenantId, allCommentsToInsert);

            List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                .ToList();

            return new WorkScheduleStageWorkWeb(
                work.Id, work.Name, work.Order, work.ColorRgb,
                work.IsClosed, periodWebs, assigneeWebs, commentWebs);
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

        private async Task<(WorkScheduleStageWork work, HashSet<Guid> previousAssignedUsers)> UpsertWorkAsync(
            WorkScheduleWorkDto workDto,
            WorkScheduleStage stage,
            Dictionary<Guid, HashSet<Guid>> existingUserIdsByWork,
            Guid tenantId,
            Guid projectId,
            List<WorkScheduleStageWorkAssignment> allAssignmentsToDelete,
            List<WorkScheduleStageWorkComment> allCommentsToDelete,
            CancellationToken cancellationToken)
        {
            (List<WorkScheduleStageWorkPeriod> periods, bool isWorkClosed) = WorkScheduleValidationHelper.BuildPeriods(workDto);

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
                ProjectId = projectId,
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
            string commenterName = WorkScheduleQueryHelper.FormatFullName(currentUser.FirstName, currentUser.LastName);

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

        private async Task<List<WorkScheduleWorkDependencyWeb>> ReplaceDependenciesAsync(
            WorkSchedule workSchedule,
            List<WorkScheduleWorkDependencyDto>? dependencies,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            IEnumerable<WorkScheduleStageWorkDependency> existing = await dependencyRepo.GetBySearch(
                d => d.WorkScheduleId == workSchedule.Id && d.TenantId == workSchedule.TenantId);

            List<WorkScheduleStageWorkDependency> existingList = existing.ToList();
            if (existingList.Count > 0)
                await dependencyRepo.DeleteRange(existingList);

            if (dependencies == null || dependencies.Count == 0)
                return new List<WorkScheduleWorkDependencyWeb>();

            var (entities, webs) = WorkScheduleQueryHelper.BuildDependencies(
                dependencies, workSchedule.Id, workSchedule.TenantId, workSchedule.ProjectId, tempIdToWorkId);

            if (entities.Count > 0)
                await dependencyRepo.InsertRange(entities);

            return webs;
        }

        private async Task DeleteDependenciesForWorksAsync(
            HashSet<Guid> deletedWorkIds,
            Guid workScheduleId,
            CancellationToken cancellationToken)
        {
            if (deletedWorkIds.Count == 0)
                return;

            IEnumerable<WorkScheduleStageWorkDependency> affected = await dependencyRepo.GetBySearch(
                d => d.WorkScheduleId == workScheduleId
                     && (deletedWorkIds.Contains(d.PredecessorWorkId) || deletedWorkIds.Contains(d.SuccessorWorkId)));

            List<WorkScheduleStageWorkDependency> affectedList = affected.ToList();
            if (affectedList.Count > 0)
                await dependencyRepo.DeleteRange(affectedList);
        }

            }
        }
