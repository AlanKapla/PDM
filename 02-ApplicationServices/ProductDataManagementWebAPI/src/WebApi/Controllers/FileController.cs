using Business.Interfaces.Constants;
using CQRS.Files.AddFileVersionComment;
using CQRS.Files.CreatePackageAndUploadFiles;
using CQRS.Files.DeleteProjectFile;
using CQRS.Files.GetSharedFiles;
using CQRS.Files.GetUserUploadedFiles;
using CQRS.Files.ShareProjectFiles;
using CQRS.Files.UpdateFileShare;
using CQRS.Files.UploadProjectFiles;
using CQRS.Files.UploadProjectFileVersion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Dedicated controller for managing project files
    /// </summary>
    [Route("api/tenants/{tenantId}/project/{projectId}/file")]
    [ApiController]
    public class FileController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Create a new package and upload files to it
        /// </summary>
        [HttpPost("packages/create")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        [RequestSizeLimit(52428800)] // 50 MB
        [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
        public async Task<IActionResult> CreatePackageAndUploadFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromForm] CreatePackageAndUploadFilesCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };

            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Upload files to an existing package
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesReadShared)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> ShareFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] ShareProjectFilesCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };

            await Send(command);
            
            return NoContent();
        }

        /// <summary>
        /// Delete a file (owner or project admin only)
        /// </summary>
        [HttpDelete("{fileId}")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
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
        [Authorize(Policy = PermissionCodes.ProjectResourcesWriteShared)]
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

        /// <summary>
        /// Update file sharing - add or remove access for specific users
        /// </summary>
        [HttpPut("{fileId}/share")]
        [Authorize(Policy = PermissionCodes.ProjectResourcesWrite)]
        public async Task<IActionResult> UpdateFileShare(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId,
            [FromBody] UpdateFileShareCommand command)
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
    }
}
