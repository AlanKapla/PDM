using CQRS.Files.AddFileVersionComment;
using CQRS.Files.DeleteProjectFile;
using CQRS.Files.GetSharedFiles;
using CQRS.Files.GetUserUploadedFiles;
using CQRS.Files.ShareProjectFile;
using CQRS.Files.UploadProjectFiles;
using CQRS.Files.UploadProjectFileVersion;
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
            command = command with { TenantId = tenantId, ProjectId = projectId };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Upload a new version of an existing project file
        /// </summary>
        [HttpPost("versions")]
        [Authorize(Policy = Policies.ProjectMember)]
        [RequestSizeLimit(52428800)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UploadFileVersion(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] UploadProjectFileVersionCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };

            await Send(command);
            return NoContent();
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
        [Authorize(Policy = Policies.ProjectMember)]
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
            command = command with { TenantId = tenantId, ProjectId = projectId };

            var result = await Send(command);
            
            if (result.FailedCount > 0 && result.SuccessCount == 0)
            {
                return BadRequest(new
                {
                    Message = "File sharing failed",
                    result.Errors
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

        /// <summary>
        /// Upload a new version of an existing file with optional comment
        /// </summary>
        [HttpPost("{fileId}/versions")]
        [Authorize(Policy = Policies.ProjectMember)]
        [RequestSizeLimit(52428800)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> UploadNewVersion(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId,
            [FromForm] UploadProjectFileVersionCommand command)
        {
            command = command with 
            { 
                TenantId = tenantId, 
                ProjectId = projectId, 
                FileId = fileId 
            };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Add a comment to a specific file version
        /// </summary>
        [HttpPost("{fileId}/versions/{versionId}/comments")]
        [Authorize(Policy = Policies.ProjectMember)]
        public async Task<IActionResult> AddFileVersionComment(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId,
            [FromRoute] Guid versionId,
            [FromBody] AddFileVersionCommentCommand command)
        {
            command = command with 
            { 
                TenantId = tenantId, 
                ProjectId = projectId, 
                FileId = fileId,
                VersionId = versionId
            };

            await Send(command);
            return NoContent();
        }
    }
}
