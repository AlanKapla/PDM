using Business.Interfaces.Constants;
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

namespace CQRS.WorkSchedules.CreateWorkSchedule
{
    public class CreateWorkScheduleCommandHandler : IRequestHandler<CreateWorkScheduleCommand, WorkScheduleDetailsWeb>
    {
        private readonly IRepository<WorkSchedule> workScheduleRepo;
        private readonly IRepository<WorkScheduleStage> stageRepo;
        private readonly IRepository<WorkScheduleStageWork> workRepo;
        private readonly IRepository<WorkScheduleStageWorkAssignment> assignmentRepo;
        private readonly IRepository<WorkScheduleStageWorkComment> commentRepo;
        private readonly IRepository<WorkScheduleStageWorkDependency> dependencyRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IWorkScheduleNotificationService workScheduleNotificationService;
        private readonly IWorkScheduleSyncService workScheduleSyncService;
        private readonly ICostEstimateAccessService costEstimateAccessService;
        private readonly ICurrentUser currentUser;

        public CreateWorkScheduleCommandHandler(
            IRepository<WorkSchedule> workScheduleRepo,
            IRepository<WorkScheduleStage> stageRepo,
            IRepository<WorkScheduleStageWork> workRepo,
            IRepository<WorkScheduleStageWorkAssignment> assignmentRepo,
            IRepository<WorkScheduleStageWorkComment> commentRepo,
            IRepository<WorkScheduleStageWorkDependency> dependencyRepo,
            IReadRepository<User> userRepo,
            IWorkScheduleNotificationService workScheduleNotificationService,
            IWorkScheduleSyncService workScheduleSyncService,
            ICostEstimateAccessService costEstimateAccessService,
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
            this.workScheduleSyncService = workScheduleSyncService;
            this.costEstimateAccessService = costEstimateAccessService;
            this.currentUser = currentUser;
        }

        public async Task<WorkScheduleDetailsWeb> Handle(CreateWorkScheduleCommand request, CancellationToken cancellationToken)
        {
            Guid tenantId = request.TenantId;
            Guid projectId = request.ProjectId;

            WorkSchedule workSchedule = await CreateWorkScheduleEntityAsync(request, tenantId, projectId, cancellationToken);
            string createdByUserName = WorkScheduleQueryHelper.FormatFullName(currentUser.FirstName, currentUser.LastName);

            if (request.CostEstimateId.HasValue)
                return await HandleCostEstimatePathAsync(workSchedule, request, tenantId, projectId, createdByUserName, cancellationToken);

            return await HandleManualPathAsync(workSchedule, request, tenantId, projectId, createdByUserName, cancellationToken);
        }

        private async Task<WorkSchedule> CreateWorkScheduleEntityAsync(
            CreateWorkScheduleCommand request,
            Guid tenantId,
            Guid projectId,
            CancellationToken cancellationToken)
        {
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
            await workScheduleRepo.SaveChangesAsync(cancellationToken);
            return workSchedule;
        }

        private async Task<WorkScheduleDetailsWeb> HandleCostEstimatePathAsync(
            WorkSchedule workSchedule,
            CreateWorkScheduleCommand request,
            Guid tenantId,
            Guid projectId,
            string createdByUserName,
            CancellationToken cancellationToken)
        {
            CostEstimateAccessLevel accessLevel = await costEstimateAccessService.GetAccessLevelAsync(
                currentUser, tenantId, projectId, request.CostEstimateId!.Value, cancellationToken);

            if (accessLevel < CostEstimateAccessLevel.Full)
                throw new ForbiddenApiException("You do not have full access to the linked cost estimate.");

            List<WorkScheduleStage> syncedStages = await workScheduleSyncService.SyncFromCostEstimateAsync(workSchedule, cancellationToken);
            List<WorkScheduleStageWeb> syncedStageWebs = BuildStageTree(syncedStages, null);

            return new WorkScheduleDetailsWeb(
                workSchedule.Id,
                workSchedule.TenantId,
                workSchedule.ProjectId,
                workSchedule.CostEstimateId,
                workSchedule.Name,
                workSchedule.CreatedAt,
                workSchedule.CreatedByUserId,
                createdByUserName,
                syncedStageWebs,
                new List<WorkScheduleWorkDependencyWeb>());
        }

        private async Task<WorkScheduleDetailsWeb> HandleManualPathAsync(
            WorkSchedule workSchedule,
            CreateWorkScheduleCommand request,
            Guid tenantId,
            Guid projectId,
            string createdByUserName,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWeb> stageWebs = new List<WorkScheduleStageWeb>();
            Dictionary<Guid, Guid> tempIdToWorkId = new Dictionary<Guid, Guid>();

            if (request.Stages != null && request.Stages.Count > 0)
            {
                Dictionary<Guid, string> userNameDict = await WorkScheduleQueryHelper.BuildUserNameDictAsync(userRepo, request.Stages, cancellationToken);

                stageWebs = await CreateStagesAsync(
                    request.Stages, null,
                    tenantId, projectId, workSchedule.Id,
                    userNameDict, tempIdToWorkId, cancellationToken);

                await workScheduleNotificationService.SendAssignmentCreatedNotificationsAsync(
                    userNameDict.Keys, workSchedule.Id, request.Name,
                    tenantId, projectId, cancellationToken);
            }

            List<WorkScheduleWorkDependencyWeb> depWebs = await PersistDependenciesAsync(
                workSchedule.Id, tenantId, projectId, request.Dependencies, tempIdToWorkId, cancellationToken);

            return new WorkScheduleDetailsWeb(
                workSchedule.Id,
                workSchedule.TenantId,
                workSchedule.ProjectId,
                workSchedule.CostEstimateId,
                workSchedule.Name,
                workSchedule.CreatedAt,
                workSchedule.CreatedByUserId,
                createdByUserName,
                stageWebs,
                depWebs);
        }

        private async Task<List<WorkScheduleStageWeb>> CreateStagesAsync(
            List<WorkScheduleStageDto> stageDtos,
            Guid? parentStageId,
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            Dictionary<Guid, string> userNameDict,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWeb> result = new List<WorkScheduleStageWeb>();

            foreach (WorkScheduleStageDto stageDto in stageDtos)
            {
                WorkScheduleStage stage = await CreateStageEntityAsync(stageDto, parentStageId, tenantId, projectId, workScheduleId, cancellationToken);

                List<WorkScheduleStageWorkWeb> workWebs = await CreateWorksAsync(
                    stageDto, stage, tenantId, projectId, userNameDict, tempIdToWorkId, cancellationToken);

                List<WorkScheduleStageWeb> childWebs = stageDto.Children != null && stageDto.Children.Count > 0
                    ? await CreateStagesAsync(stageDto.Children, stage.Id, tenantId, projectId, workScheduleId, userNameDict, tempIdToWorkId, cancellationToken)
                    : new List<WorkScheduleStageWeb>();

                result.Add(new WorkScheduleStageWeb(
                    stage.Id, stage.Name, stage.Order, parentStageId, null,
                    workWebs, childWebs));
            }

            return result;
        }

        private async Task<WorkScheduleStage> CreateStageEntityAsync(
            WorkScheduleStageDto stageDto,
            Guid? parentStageId,
            Guid tenantId,
            Guid projectId,
            Guid workScheduleId,
            CancellationToken cancellationToken)
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
            await stageRepo.SaveChangesAsync(cancellationToken);
            return stage;
        }

        private async Task<List<WorkScheduleStageWorkWeb>> CreateWorksAsync(
            WorkScheduleStageDto stageDto,
            WorkScheduleStage stage,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, string> userNameDict,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            List<WorkScheduleStageWorkWeb> workWebs = new List<WorkScheduleStageWorkWeb>();

            if (stageDto.Works == null)
                return workWebs;

            List<WorkScheduleStageWorkAssignment> allAssignments = new List<WorkScheduleStageWorkAssignment>();
            List<WorkScheduleStageWorkComment> allComments = new List<WorkScheduleStageWorkComment>();

            foreach (WorkScheduleWorkDto workDto in stageDto.Works)
            {
                WorkScheduleStageWork work = await CreateWorkEntityAsync(workDto, stage, tenantId, projectId, tempIdToWorkId, cancellationToken);

                (List<WorkScheduleStageWorkAssignment> assignments, List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs) =
                    BuildWorkAssignments(workDto, work, tenantId, projectId, userNameDict);
                allAssignments.AddRange(assignments);

                (List<WorkScheduleStageWorkComment> comments, List<WorkScheduleStageWorkCommentWeb> commentWebs) =
                    BuildWorkComments(workDto, work, tenantId);
                allComments.AddRange(comments);

                List<WorkScheduleStageWorkPeriodWeb> periodWebs = work.Periods
                    .Select(p => new WorkScheduleStageWorkPeriodWeb(p.StartDate, p.EndDate, p.IsClosed))
                    .ToList();

                workWebs.Add(new WorkScheduleStageWorkWeb(
                    work.Id, work.Name, work.Order, work.ColorRgb,
                    work.IsClosed, periodWebs, assigneeWebs, commentWebs));
            }

            if (allAssignments.Count > 0)
                await assignmentRepo.InsertRange(allAssignments);

            if (allComments.Count > 0)
                await commentRepo.InsertRange(allComments);

            return workWebs;
        }

        private async Task<WorkScheduleStageWork> CreateWorkEntityAsync(
            WorkScheduleWorkDto workDto,
            WorkScheduleStage stage,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            (List<WorkScheduleStageWorkPeriod> periods, bool isWorkClosed) = WorkScheduleValidationHelper.BuildPeriods(workDto);

            WorkScheduleStageWork work = new WorkScheduleStageWork
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

            await workRepo.Insert(work);
            await workRepo.SaveChangesAsync(cancellationToken);

            if (workDto.TempId.HasValue)
                tempIdToWorkId[workDto.TempId.Value] = work.Id;

            return work;
        }

        private static (List<WorkScheduleStageWorkAssignment> assignments, List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs) BuildWorkAssignments(
            WorkScheduleWorkDto workDto,
            WorkScheduleStageWork work,
            Guid tenantId,
            Guid projectId,
            Dictionary<Guid, string> userNameDict)
        {
            List<WorkScheduleStageWorkAssignment> assignments = new List<WorkScheduleStageWorkAssignment>();
            List<WorkScheduleStageWorkAssigneeWeb> assigneeWebs = new List<WorkScheduleStageWorkAssigneeWeb>();

            if (workDto.AssignedUserIds == null)
                return (assignments, assigneeWebs);

            foreach (Guid userId in workDto.AssignedUserIds)
            {
                assignments.Add(new WorkScheduleStageWorkAssignment
                {
                    WorkScheduleStageWorkId = work.Id,
                    TenantId = tenantId,
                    ProjectId = projectId,
                    UserId = userId
                });
                assigneeWebs.Add(new WorkScheduleStageWorkAssigneeWeb(userId, userNameDict.GetValueOrDefault(userId, "Unknown User")));
            }

            return (assignments, assigneeWebs);
        }

        private (List<WorkScheduleStageWorkComment> comments, List<WorkScheduleStageWorkCommentWeb> commentWebs) BuildWorkComments(
            WorkScheduleWorkDto workDto,
            WorkScheduleStageWork work,
            Guid tenantId)
        {
            List<WorkScheduleStageWorkComment> comments = new List<WorkScheduleStageWorkComment>();
            List<WorkScheduleStageWorkCommentWeb> commentWebs = new List<WorkScheduleStageWorkCommentWeb>();

            if (workDto.Comments == null)
                return (comments, commentWebs);

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
                comments.Add(comment);
                commentWebs.Add(new WorkScheduleStageWorkCommentWeb(
                    comment.Id, comment.Content, comment.CreatedByUserId,
                    commenterName, comment.CreatedAt));
            }

            return (comments, commentWebs);
        }

        private async Task<List<WorkScheduleWorkDependencyWeb>> PersistDependenciesAsync(
            Guid workScheduleId,
            Guid tenantId,
            Guid projectId,
            List<WorkScheduleWorkDependencyDto>? dependencies,
            Dictionary<Guid, Guid> tempIdToWorkId,
            CancellationToken cancellationToken)
        {
            if (dependencies == null || dependencies.Count == 0)
                return new List<WorkScheduleWorkDependencyWeb>();

            var (entities, webs) = WorkScheduleQueryHelper.BuildDependencies(
                dependencies, workScheduleId, tenantId, projectId, tempIdToWorkId);

            if (entities.Count > 0)
                await dependencyRepo.InsertRange(entities);

            return webs;
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
