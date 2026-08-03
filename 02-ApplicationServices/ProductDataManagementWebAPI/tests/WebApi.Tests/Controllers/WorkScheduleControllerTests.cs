using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.AddWorkScheduleStage;
using CQRS.WorkSchedules.AddWorkScheduleStageWork;
using CQRS.WorkSchedules.AddWorkScheduleStageWorkComment;
using CQRS.WorkSchedules.CreateWorkSchedule;
using CQRS.WorkSchedules.DeleteWorkSchedule;
using CQRS.WorkSchedules.DeleteWorkScheduleStage;
using CQRS.WorkSchedules.DeleteWorkScheduleStageWork;
using CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment;
using CQRS.WorkSchedules.GetMyWorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.GetWorkSchedules;
using CQRS.WorkSchedules.MoveWorkScheduleStage;
using CQRS.WorkSchedules.MoveWorkScheduleStageWork;
using CQRS.WorkSchedules.RenameWorkScheduleStage;
using CQRS.WorkSchedules.RenameWorkScheduleStageWork;
using CQRS.WorkSchedules.ReorderWorkScheduleStageWorks;
using CQRS.WorkSchedules.ReorderWorkScheduleStages;
using CQRS.WorkSchedules.SetWorkScheduleDependencies;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments;
using CQRS.WorkSchedules.GetWorkScheduleAssignableAssignees;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods;
using CQRS.WorkSchedules.SyncWorkScheduleWithEstimate;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class WorkScheduleControllerTests : ControllerTestBase
    {
        private readonly WorkScheduleController sut;

        public WorkScheduleControllerTests()
        {
            sut = new WorkScheduleController(MediatorMock.Object);
        }

        [Fact]
        public async Task CreateWorkSchedule_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            CreateWorkScheduleCommand command = new CreateWorkScheduleCommand();

            IActionResult result = await sut.CreateWorkSchedule(tenantId, projectId, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateWorkScheduleCommand>(c => c.TenantId == tenantId && c.ProjectId == projectId);
        }

        [Fact]
        public async Task UpdateWorkSchedule_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            UpdateWorkScheduleCommand command = new UpdateWorkScheduleCommand();

            IActionResult result = await sut.UpdateWorkSchedule(tenantId, projectId, workScheduleId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateWorkScheduleCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task GetWorkSchedules_ReturnsOk_WithScope()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetWorkSchedules(tenantId, projectId, ResourceScope.All);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetWorkSchedulesQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.Scope == ResourceScope.All);
        }

        [Fact]
        public async Task GetWorkSchedule_ReturnsOk_WithWorkScheduleId()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();

            IActionResult result = await sut.GetWorkSchedule(tenantId, projectId, workScheduleId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetWorkScheduleQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task GetMyWorkSchedules_ReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetMyWorkSchedules(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetMyWorkSchedulesQuery>(q => q.TenantId == tenantId && q.ProjectId == projectId);
        }

        [Fact]
        public async Task SyncWorkScheduleWithEstimate_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();

            IActionResult result = await sut.SyncWorkScheduleWithEstimate(tenantId, projectId, workScheduleId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SyncWorkScheduleWithEstimateCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task DeleteWorkSchedule_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();

            IActionResult result = await sut.DeleteWorkSchedule(tenantId, projectId, workScheduleId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteWorkScheduleCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task AddStage_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            AddWorkScheduleStageCommand command = new AddWorkScheduleStageCommand();

            IActionResult result = await sut.AddStage(tenantId, projectId, workScheduleId, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<AddWorkScheduleStageCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task DeleteStage_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();

            IActionResult result = await sut.DeleteStage(tenantId, projectId, workScheduleId, stageId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteWorkScheduleStageCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId && c.WorkScheduleStageId == stageId);
        }

        [Fact]
        public async Task RenameStage_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            RenameWorkScheduleStageCommand command = new RenameWorkScheduleStageCommand { Name = "S1" };

            IActionResult result = await sut.RenameStage(tenantId, projectId, workScheduleId, stageId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RenameWorkScheduleStageCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId && c.WorkScheduleStageId == stageId);
        }

        [Fact]
        public async Task ReorderStages_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            ReorderWorkScheduleStagesCommand command = new ReorderWorkScheduleStagesCommand();

            IActionResult result = await sut.ReorderStages(tenantId, projectId, workScheduleId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ReorderWorkScheduleStagesCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId);
        }

        [Fact]
        public async Task MoveStage_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            MoveWorkScheduleStageCommand command = new MoveWorkScheduleStageCommand();

            IActionResult result = await sut.MoveStage(tenantId, projectId, workScheduleId, stageId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MoveWorkScheduleStageCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId && c.WorkScheduleStageId == stageId);
        }

        [Fact]
        public async Task AddWork_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            AddWorkScheduleStageWorkCommand command = new AddWorkScheduleStageWorkCommand();

            IActionResult result = await sut.AddWork(tenantId, projectId, workScheduleId, stageId, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<AddWorkScheduleStageWorkCommand>(c =>
                c.TenantId == tenantId && c.WorkScheduleId == workScheduleId && c.WorkScheduleStageId == stageId);
        }

        [Fact]
        public async Task DeleteWork_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();

            IActionResult result = await sut.DeleteWork(tenantId, projectId, workScheduleId, stageId, workId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteWorkScheduleStageWorkCommand>(c =>
                c.WorkScheduleId == workScheduleId
                && c.WorkScheduleStageId == stageId
                && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task RenameWork_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            RenameWorkScheduleStageWorkCommand command = new RenameWorkScheduleStageWorkCommand { Name = "W1" };

            IActionResult result = await sut.RenameWork(tenantId, projectId, workScheduleId, stageId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RenameWorkScheduleStageWorkCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task SetWorkColorRgb_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            SetWorkScheduleStageWorkColorRgbCommand command = new SetWorkScheduleStageWorkColorRgbCommand();

            IActionResult result = await sut.SetWorkColorRgb(tenantId, projectId, workScheduleId, stageId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleStageWorkColorRgbCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task ReorderWorks_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid stageId = Guid.NewGuid();
            ReorderWorkScheduleStageWorksCommand command = new ReorderWorkScheduleStageWorksCommand();

            IActionResult result = await sut.ReorderWorks(tenantId, projectId, workScheduleId, stageId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ReorderWorkScheduleStageWorksCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageId == stageId);
        }

        [Fact]
        public async Task MoveWork_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            MoveWorkScheduleStageWorkCommand command = new MoveWorkScheduleStageWorkCommand();

            IActionResult result = await sut.MoveWork(tenantId, projectId, workScheduleId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MoveWorkScheduleStageWorkCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task SetPeriods_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            SetWorkScheduleStageWorkPeriodsCommand command = new SetWorkScheduleStageWorkPeriodsCommand();

            IActionResult result = await sut.SetPeriods(tenantId, projectId, workScheduleId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleStageWorkPeriodsCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task SetAssignments_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            SetWorkScheduleStageWorkAssignmentsCommand command = new SetWorkScheduleStageWorkAssignmentsCommand();

            IActionResult result = await sut.SetAssignments(tenantId, projectId, workScheduleId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleStageWorkAssignmentsCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task GetAssignableAssignees_ReturnsOk_WithRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetAssignableAssignees(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetWorkScheduleAssignableAssigneesQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId);
        }

        [Fact]
        public async Task SetWorkIsClosed_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            SetWorkScheduleStageWorkIsClosedCommand command = new SetWorkScheduleStageWorkIsClosedCommand();

            IActionResult result = await sut.SetWorkIsClosed(tenantId, projectId, workScheduleId, workId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleStageWorkIsClosedCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task SetPeriodIsClosed_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            Guid periodId = Guid.NewGuid();
            SetWorkScheduleStageWorkPeriodIsClosedCommand command = new SetWorkScheduleStageWorkPeriodIsClosedCommand();

            IActionResult result = await sut.SetPeriodIsClosed(tenantId, projectId, workScheduleId, workId, periodId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleStageWorkPeriodIsClosedCommand>(c =>
                c.WorkScheduleId == workScheduleId
                && c.WorkScheduleStageWorkId == workId
                && c.PeriodId == periodId);
        }

        [Fact]
        public async Task AddComment_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid workId = Guid.NewGuid();
            AddWorkScheduleStageWorkCommentCommand command = new AddWorkScheduleStageWorkCommentCommand { Content = "c" };

            IActionResult result = await sut.AddComment(tenantId, projectId, workScheduleId, workId, command);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<AddWorkScheduleStageWorkCommentCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.WorkScheduleStageWorkId == workId);
        }

        [Fact]
        public async Task UpdateComment_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid commentId = Guid.NewGuid();
            UpdateWorkScheduleStageWorkCommentCommand command = new UpdateWorkScheduleStageWorkCommentCommand();

            IActionResult result = await sut.UpdateComment(tenantId, projectId, workScheduleId, commentId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateWorkScheduleStageWorkCommentCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.CommentId == commentId);
        }

        [Fact]
        public async Task DeleteComment_BuildsCommand_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            Guid commentId = Guid.NewGuid();

            IActionResult result = await sut.DeleteComment(tenantId, projectId, workScheduleId, commentId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteWorkScheduleStageWorkCommentCommand>(c =>
                c.WorkScheduleId == workScheduleId && c.CommentId == commentId);
        }

        [Fact]
        public async Task SetDependencies_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid workScheduleId = Guid.NewGuid();
            SetWorkScheduleDependenciesCommand command = new SetWorkScheduleDependenciesCommand();

            IActionResult result = await sut.SetDependencies(tenantId, projectId, workScheduleId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<SetWorkScheduleDependenciesCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.WorkScheduleId == workScheduleId);
        }
    }
}
