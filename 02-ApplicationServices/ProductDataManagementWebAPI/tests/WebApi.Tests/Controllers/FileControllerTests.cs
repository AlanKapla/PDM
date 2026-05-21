using Business.Interfaces.Constants;
using CQRS.Files.AddFileVersionComment;
using CQRS.Files.CreatePackageAndUploadFiles;
using CQRS.Files.DeleteProjectFile;
using CQRS.Files.GetFileVersions;
using CQRS.Files.GetPackageFiles;
using CQRS.Files.GetProjectFilePackages;
using CQRS.Files.GetVersionComments;
using CQRS.Files.SharePackages;
using CQRS.Files.UpdateFileShare;
using CQRS.Files.UploadProjectFiles;
using CQRS.Files.UploadProjectFileVersion;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class FileControllerTests : ControllerTestBase
    {
        private readonly FileController sut;

        public FileControllerTests()
        {
            sut = new FileController(MediatorMock.Object);
        }

        private static IFormFile FakeFormFile() => new Mock<IFormFile>().Object;

        [Fact]
        public async Task CreatePackageAndUploadFiles_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            CreatePackageAndUploadFilesCommand command = new CreatePackageAndUploadFilesCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                PackageName = "p"
            };

            IActionResult result = await sut.CreatePackageAndUploadFiles(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<CreatePackageAndUploadFilesCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.PackageName == "p");
        }

        [Fact]
        public async Task GetProjectFilePackages_ReturnsOk_WithRouteAndScope()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectFilePackages(tenantId, projectId, ResourceScope.All);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectFilePackagesQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.Scope == ResourceScope.All);
        }

        [Fact]
        public async Task GetPackageFiles_ReturnsOk_WithPackageId()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid packageId = Guid.NewGuid();

            IActionResult result = await sut.GetPackageFiles(tenantId, projectId, packageId, ResourceScope.Mine);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetPackageFilesQuery>(q =>
                q.TenantId == tenantId
                && q.ProjectId == projectId
                && q.PackageId == packageId
                && q.Scope == ResourceScope.Mine);
        }

        [Fact]
        public async Task GetFileVersions_ReturnsOk_WithFileId()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();

            IActionResult result = await sut.GetFileVersions(tenantId, projectId, fileId, ResourceScope.Shared);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetFileVersionsQuery>(q =>
                q.TenantId == tenantId
                && q.ProjectId == projectId
                && q.FileId == fileId
                && q.Scope == ResourceScope.Shared);
        }

        [Fact]
        public async Task GetVersionComments_ReturnsOk_WithFileAndVersionId()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            Guid versionId = Guid.NewGuid();

            IActionResult result = await sut.GetVersionComments(tenantId, projectId, fileId, versionId, ResourceScope.All);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetVersionCommentsQuery>(q =>
                q.TenantId == tenantId
                && q.ProjectId == projectId
                && q.FileId == fileId
                && q.VersionId == versionId
                && q.Scope == ResourceScope.All);
        }

        [Fact]
        public async Task UploadFiles_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            UploadProjectFilesCommand command = new UploadProjectFilesCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                ProjectFilePackageId = Guid.NewGuid()
            };

            IActionResult result = await sut.UploadFiles(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UploadProjectFilesCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId);
        }

        [Fact]
        public async Task UploadFileVersion_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            UploadProjectFileVersionCommand command = new UploadProjectFileVersionCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                FileId = fileId,
                File = FakeFormFile()
            };

            IActionResult result = await sut.UploadFileVersion(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UploadProjectFileVersionCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId);
        }

        [Fact]
        public async Task SharePackages_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            SharePackagesCommand command = new SharePackagesCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                PackageIds = new List<Guid>(),
                SharedWithUserIds = new List<Guid>()
            };

            IActionResult result = await sut.SharePackages(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SharePackagesCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId);
        }

        [Fact]
        public async Task DeleteFile_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();

            IActionResult result = await sut.DeleteFile(tenantId, projectId, fileId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteProjectFileCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.FileId == fileId);
        }

        [Fact]
        public async Task UploadNewVersion_OverridesIdsAndFileId_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            UploadProjectFileVersionCommand command = new UploadProjectFileVersionCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                FileId = Guid.Empty,
                File = FakeFormFile()
            };

            IActionResult result = await sut.UploadNewVersion(tenantId, projectId, fileId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UploadProjectFileVersionCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.FileId == fileId);
        }

        [Fact]
        public async Task AddFileVersionComment_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            Guid versionId = Guid.NewGuid();
            AddFileVersionCommentCommand command = new AddFileVersionCommentCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                FileId = Guid.Empty,
                VersionId = Guid.Empty,
                Comment = "ok"
            };

            IActionResult result = await sut.AddFileVersionComment(tenantId, projectId, fileId, versionId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<AddFileVersionCommentCommand>(c =>
                c.TenantId == tenantId
                && c.ProjectId == projectId
                && c.FileId == fileId
                && c.VersionId == versionId
                && c.Comment == "ok");
        }

        [Fact]
        public async Task UpdateFileShare_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid fileId = Guid.NewGuid();
            UpdateFileShareCommand command = new UpdateFileShareCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                FileId = Guid.Empty,
                SharedWithUserIds = new List<Guid>()
            };

            IActionResult result = await sut.UpdateFileShare(tenantId, projectId, fileId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateFileShareCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.FileId == fileId);
        }
    }
}
