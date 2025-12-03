using CQRS.Files.DeleteProjectFile;
using CQRS.Files.GetSharedFiles;
using CQRS.Files.GetUserUploadedFiles;
using CQRS.Files.ShareProjectFile;
using CQRS.Files.UploadProjectFiles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Constants;

namespace WebApi.Controllers
{
    /// <summary>
    /// Dedicated controller for managing project files
    /// </summary>
    [Route("api/tenants/{tenantId}/projects/{projectId}/[controller]")]
    [ApiController]
    public class FileController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpPost]
        [Authorize(Policy = Policies.ProjectMember)]
        [RequestSizeLimit(52428800)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UploadFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] UploadProjectFilesCommand command)
        {
            if (command.TenantId != tenantId)
            {
                return BadRequest("TenantId in URL does not match TenantId in request body");
            }

            if (command.ProjectId != projectId)
            {
                return BadRequest("ProjectId in URL does not match ProjectId in request body");
            }

            var result = await Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Get files uploaded by current user
        /// </summary>
        [HttpGet("my")]
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> GetMyFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetUserUploadedFilesQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get files shared with current user
        /// </summary>
        [HttpGet("shared")]
        [Authorize(Policy = Policies.TenantMember)]
        public async Task<IActionResult> GetSharedFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId)
        {
            var query = new GetSharedFilesQuery(tenantId, projectId);
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Share files with another project member
        /// </summary>
        [HttpPost("share")]
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> ShareFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] ShareProjectFileCommand command)
        {
            if (command.TenantId != tenantId)
            {
                return BadRequest("TenantId in URL does not match TenantId in request body");
            }

            if (command.ProjectId != projectId)
            {
                return BadRequest("ProjectId in URL does not match ProjectId in request body");
            }

            var result = await Send(command);
            
            if (result.FailedCount > 0 && result.SuccessCount == 0)
            {
                return BadRequest(new
                {
                    Message = "File sharing failed",
                    Errors = result.Errors
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Delete a file (owner or project admin only)
        /// </summary>
        [HttpDelete("{fileId}")]
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> DeleteFile(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId)
        {
            var command = new DeleteProjectFileCommand(tenantId, projectId, fileId);
            await Send(command);
            return NoContent();
        }
    }
}
