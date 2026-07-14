using Business.Interfaces.Constants;
using CQRS.Files.AddFileVersionComment;
using CQRS.Files.CreateDirectory;
using CQRS.Files.CreatePackageAndUploadFiles;
using CQRS.Files.DeleteProjectFile;
using CQRS.Files.GetPackageFiles;
using CQRS.Files.GetProjectFilePackages;
using CQRS.Files.GetFileVersions;
using CQRS.Files.GetVersionComments;
using CQRS.Files.SharePackages;
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
    [Route("api/tenants/{tenantId}/projects/{projectId}/file")]
    [ApiController]
    public class FileController(IMediator mediator) : BaseApiController(mediator)
    {
        /// <summary>
        /// Create a new package and upload files to it
        /// </summary>
        [HttpPost("packages/create")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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
        /// Get project file packages based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of file packages without Files collection</returns>
        [HttpGet("packages/{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> GetProjectFilePackages(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetProjectFilePackagesQuery { TenantId = tenantId, ProjectId = projectId, Scope = scope };
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get files in a specific package based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="packageId">Package ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of files with CurrentVersion but without Versions collection</returns>
        [HttpGet("packages/{packageId}/files/{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> GetPackageFiles(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid packageId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetPackageFilesQuery { TenantId = tenantId, ProjectId = projectId, PackageId = packageId, Scope = scope };
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all versions of a specific file based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of file versions without Comments collection</returns>
        [HttpGet("files/{fileId}/versions/{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> GetFileVersions(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetFileVersionsQuery { TenantId = tenantId, ProjectId = projectId, FileId = fileId, Scope = scope };
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get all comments for a specific file version based on scope (All, Mine, Shared)
        /// </summary>
        /// <param name="tenantId">Tenant ID</param>
        /// <param name="projectId">Project ID</param>
        /// <param name="fileId">File ID</param>
        /// <param name="versionId">Version ID</param>
        /// <param name="scope">Resource scope (All, Mine, Shared)</param>
        /// <returns>List of comments for the version</returns>
        [HttpGet("files/{fileId}/versions/{versionId}/comments/{scope}")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> GetVersionComments(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId,
            [FromRoute] Guid versionId,
            [FromRoute] ResourceScope scope)
        {
            var query = new GetVersionCommentsQuery { TenantId = tenantId, ProjectId = projectId, FileId = fileId, VersionId = versionId, Scope = scope };
            var result = await Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Upload files to an existing package
        /// </summary>
        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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
        /// Share package(s) with project members
        /// Always shares entire package with optional file exclusions (Deny)
        /// </summary>
        [HttpPost("packages/share")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> SharePackages(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] SharePackagesCommand command)
        {
            command = command with { TenantId = tenantId, ProjectId = projectId };

            await Send(command);
            
            return NoContent();
        }

        /// <summary>
        /// Delete a file (owner or project admin only)
        /// </summary>
        [HttpDelete("{fileId}")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> DeleteFile(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromRoute] Guid fileId)
        {
            var command = new DeleteProjectFileCommand { TenantId = tenantId, ProjectId = projectId, FileId = fileId };
            await Send(command);
            return NoContent();
        }

        /// <summary>
        /// Upload a new version of an existing file with optional comment
        /// </summary>
        [HttpPost("{fileId}/versions")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
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

        /// <summary>
        /// Creates a new empty directory
        /// </summary>
        [HttpPost("directories")]
        [Authorize(Policy = PermissionCodes.ProjectFiles)]
        public async Task<IActionResult> CreateDirectory(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid projectId,
            [FromBody] CreateDirectoryRequest request)
        {
            CreateDirectoryCommand command = new()
            {
                TenantId = tenantId,
                ProjectId = projectId,
                DirectoryName = request.DirectoryName,
                ParentId = request.ParentId
            };
            await Send(command);
            return NoContent();
        }
    }
}
