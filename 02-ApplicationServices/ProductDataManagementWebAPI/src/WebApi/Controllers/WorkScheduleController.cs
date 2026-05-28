using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.WorkSchedules;
using CQRS.WorkSchedules.AddWorkScheduleStage;
using CQRS.WorkSchedules.AddWorkScheduleStageWork;
using CQRS.WorkSchedules.MoveWorkScheduleStage;
using CQRS.WorkSchedules.MoveWorkScheduleStageWork;
using CQRS.WorkSchedules.RenameWorkScheduleStage;
using CQRS.WorkSchedules.RenameWorkScheduleStageWork;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkColorRgb;
using CQRS.WorkSchedules.ReorderWorkScheduleStageWorks;
using CQRS.WorkSchedules.ReorderWorkScheduleStages;
using CQRS.WorkSchedules.AddWorkScheduleStageWorkComment;
using CQRS.WorkSchedules.CreateWorkSchedule;
using CQRS.WorkSchedules.DeleteWorkSchedule;
using CQRS.WorkSchedules.DeleteWorkScheduleStage;
using CQRS.WorkSchedules.DeleteWorkScheduleStageWork;
using CQRS.WorkSchedules.DeleteWorkScheduleStageWorkComment;
using CQRS.WorkSchedules.GetMyWorkSchedules;
using CQRS.WorkSchedules.GetWorkSchedule;
using CQRS.WorkSchedules.GetWorkSchedules;
using CQRS.WorkSchedules.SetWorkScheduleDependencies;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkAssignments;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkIsClosed;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriodIsClosed;
using CQRS.WorkSchedules.SetWorkScheduleStageWorkPeriods;
using CQRS.WorkSchedules.SyncWorkScheduleWithEstimate;
using CQRS.WorkSchedules.UpdateWorkSchedule;
using CQRS.WorkSchedules.UpdateWorkScheduleStageWorkComment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/projects/{projectId}/work-schedule")]
    [ApiController]
    public class WorkScheduleController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> CreateWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] CreateWorkScheduleCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };
            Guid id = await Send(command);
            return CreatedAtAction(nameof(GetWorkSchedule), new { tenantId, projectId, workScheduleId = id }, id);
        }

        [HttpPut("{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> UpdateWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromBody] UpdateWorkScheduleCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            await Send(command);
            return NoContent();
        }

        [HttpGet("{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> GetWorkSchedules(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            GetWorkSchedulesQuery query = new GetWorkSchedulesQuery { TenantId = tenantId, ProjectId = projectId, Scope = scope };
            object result = await Send(query);
            return Ok(result);
        }

        [HttpGet("details/{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> GetWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            GetWorkScheduleQuery query = new GetWorkScheduleQuery { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            WorkScheduleDetailsWeb result = await Send(query);
            return Ok(result);
        }

        [HttpGet("my")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> GetMyWorkSchedules(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            GetMyWorkSchedulesQuery query = new GetMyWorkSchedulesQuery { TenantId = tenantId, ProjectId = projectId };
            List<MyWorkSchedulesTenantDto> result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{workScheduleId}/sync-with-estimate")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SyncWorkScheduleWithEstimate(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            SyncWorkScheduleWithEstimateCommand command = new SyncWorkScheduleWithEstimateCommand { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            await Send(command);
            return NoContent();
        }

        [HttpDelete("{workScheduleId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> DeleteWorkSchedule(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId)
        {
            DeleteWorkScheduleCommand command = new DeleteWorkScheduleCommand { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            await Send(command);
            return NoContent();
        }

        // ─── Stages ──────────────────────────────────────────────────────────────

        [HttpPost("{workScheduleId}/stages")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> AddStage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromBody] AddWorkScheduleStageCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            Guid stageId = await Send(command);
            return CreatedAtAction(nameof(GetWorkSchedule), new { tenantId, projectId, workScheduleId }, stageId);
        }

        [HttpDelete("{workScheduleId}/stages/{stageId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> DeleteStage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId)
        {
            DeleteWorkScheduleStageCommand command = new DeleteWorkScheduleStageCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/name")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> RenameStage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromBody] RenameWorkScheduleStageCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPut("{workScheduleId}/stages/order")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> ReorderStages(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromBody] ReorderWorkScheduleStagesCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/parent")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> MoveStage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromBody] MoveWorkScheduleStageCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId
            };
            await Send(command);
            return NoContent();
        }

        // ─── Works ───────────────────────────────────────────────────────────────

        [HttpPost("{workScheduleId}/stages/{stageId}/works")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> AddWork(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromBody] AddWorkScheduleStageWorkCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId
            };
            Guid workId = await Send(command);
            return CreatedAtAction(nameof(GetWorkSchedule), new { tenantId, projectId, workScheduleId }, workId);
        }

        [HttpDelete("{workScheduleId}/stages/{stageId}/works/{workId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> DeleteWork(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromRoute] Guid workId)
        {
            DeleteWorkScheduleStageWorkCommand command = new DeleteWorkScheduleStageWorkCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/works/{workId}/name")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> RenameWork(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromRoute] Guid workId,
            [FromBody] RenameWorkScheduleStageWorkCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/works/{workId}/color-rgb")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetWorkColorRgb(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromRoute] Guid workId,
            [FromBody] SetWorkScheduleStageWorkColorRgbCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPut("{workScheduleId}/stages/{stageId}/works/order")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> ReorderWorks(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid stageId,
            [FromBody] ReorderWorkScheduleStageWorksCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageId = stageId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/works/{workId}/stage")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> MoveWork(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromBody] MoveWorkScheduleStageWorkCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        // ─── Periods ─────────────────────────────────────────────────────────────

        [HttpPut("{workScheduleId}/stages/{stageId}/works/{workId}/periods")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetPeriods(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromBody] SetWorkScheduleStageWorkPeriodsCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPut("{workScheduleId}/stages/{stageId}/works/{workId}/assignments")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetAssignments(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromBody] SetWorkScheduleStageWorkAssignmentsCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/works/{workId}/is-closed")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetWorkIsClosed(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromBody] SetWorkScheduleStageWorkIsClosedCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPatch("{workScheduleId}/stages/{stageId}/works/{workId}/periods/{periodId}/is-closed")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetPeriodIsClosed(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromRoute] Guid periodId,
            [FromBody] SetWorkScheduleStageWorkPeriodIsClosedCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId,
                PeriodId = periodId
            };
            await Send(command);
            return NoContent();
        }

        // ─── Comments ────────────────────────────────────────────────────────────

        [HttpPost("{workScheduleId}/stages/{stageId}/works/{workId}/comments")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> AddComment(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid workId,
            [FromBody] AddWorkScheduleStageWorkCommentCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                WorkScheduleStageWorkId = workId
            };
            Guid commentId = await Send(command);
            return CreatedAtAction(nameof(GetWorkSchedule), new { tenantId, projectId, workScheduleId }, commentId);
        }

        [HttpPut("{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> UpdateComment(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid commentId,
            [FromBody] UpdateWorkScheduleStageWorkCommentCommand command)
        {
            command = command with
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                CommentId = commentId
            };
            await Send(command);
            return NoContent();
        }

        [HttpDelete("{workScheduleId}/stages/{stageId}/works/{workId}/comments/{commentId}")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> DeleteComment(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromRoute] Guid commentId)
        {
            DeleteWorkScheduleStageWorkCommentCommand command = new DeleteWorkScheduleStageWorkCommentCommand
            {
                TenantId = tenantId,
                ProjectId = projectId,
                WorkScheduleId = workScheduleId,
                CommentId = commentId
            };
            await Send(command);
            return NoContent();
        }

        // ─── Dependencies ─────────────────────────────────────────────────────────

        [HttpPut("{workScheduleId}/dependencies")]
        [Authorize(Policy = PermissionCodes.ProjectSchedule)]
        public async Task<IActionResult> SetDependencies(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid workScheduleId,
            [FromBody] SetWorkScheduleDependenciesCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId, WorkScheduleId = workScheduleId };
            WorkScheduleDetailsWeb result = await Send(command);
            return Ok(result);
        }

            }
        }
