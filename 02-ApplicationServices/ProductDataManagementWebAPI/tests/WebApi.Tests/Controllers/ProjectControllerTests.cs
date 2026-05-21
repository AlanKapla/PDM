using Business.Interfaces.WebModels.Projects;
using CQRS.Projects.AddProjectMember;
using CQRS.Projects.CreateProject;
using CQRS.Projects.GetProjectDetails;
using CQRS.Projects.GetProjectMembers;
using CQRS.Projects.GetProjectsDictionary;
using CQRS.Projects.GetTenantProjects;
using CQRS.Projects.RemoveProjectMember;
using CQRS.Projects.SetProjectCurrency;
using CQRS.Projects.ToggleProjectStatus;
using CQRS.Projects.UpdateProject;
using CQRS.Projects.UpdateProjectMemberRole;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class ProjectControllerTests : ControllerTestBase
    {
        private readonly ProjectController sut;

        public ProjectControllerTests()
        {
            sut = new ProjectController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetTenantProjects_ReturnsOk_WithTenantId()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetTenantProjects(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetTenantProjectsQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task GetProjectsDictionary_ReturnsOk_WithTenantId()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectsDictionary(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectsDictionaryQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task CreateProject_OverridesTenantId_AndReturnsCreatedAtAction()
        {
            Guid tenantId = Guid.NewGuid();
            Guid newProjectId = Guid.NewGuid();
            CreateProjectCommand command = new CreateProjectCommand
            {
                TenantId = Guid.Empty,
                Name = "New"
            };
            SetupMediatorReturns<CreateProjectCommand, ProjectDetailsWeb>(WebModelFactory.Project(newProjectId));

            IActionResult result = await sut.CreateProject(tenantId, command);

            CreatedAtActionResult created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.ActionName.Should().Be(nameof(ProjectController.GetProjectDetails));
            created.RouteValues!["projectId"].Should().Be(newProjectId);
            VerifyMediatorCalledOnce<CreateProjectCommand>(c => c.TenantId == tenantId && c.Name == "New");
        }

        [Fact]
        public async Task GetProjectDetails_ReturnsOk_WithRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectDetails(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectDetailsQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId);
        }

        [Fact]
        public async Task UpdateProject_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            UpdateProjectCommand command = new UpdateProjectCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                Name = "Updated"
            };
            SetupMediatorReturns<UpdateProjectCommand, ProjectDetailsWeb>(WebModelFactory.Project(projectId));

            IActionResult result = await sut.UpdateProject(tenantId, projectId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpdateProjectCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.Name == "Updated");
        }

        [Fact]
        public async Task GetProjectMembers_ReturnsOk_WithRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectMembers(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectMembersQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId);
        }

        [Fact]
        public async Task AddProjectMember_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            AddProjectMemberCommand command = new AddProjectMemberCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                UserId = userId
            };

            IActionResult result = await sut.AddProjectMember(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<AddProjectMemberCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.UserId == userId);
        }

        [Fact]
        public async Task RemoveProjectMember_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();

            IActionResult result = await sut.RemoveProjectMember(tenantId, projectId, userId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RemoveProjectMemberCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.UserId == userId);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ToggleProjectStatus_BuildsCommand_FromRouteAndQuery(bool isActive)
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.ToggleProjectStatus(tenantId, projectId, isActive);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<ToggleProjectStatusCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.IsActive == isActive);
        }

        [Fact]
        public async Task SetProjectCurrency_MapsRequestBody_ToCommand()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            SetProjectCurrencyRequest request = new SetProjectCurrencyRequest
            {
                Code = "EUR",
                Name = "Euro",
                Symbol = "€"
            };

            IActionResult result = await sut.SetProjectCurrency(tenantId, projectId, request);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<SetProjectCurrencyCommand>(c =>
                c.TenantId == tenantId
                && c.ProjectId == projectId
                && c.Code == "EUR"
                && c.Name == "Euro"
                && c.Symbol == "€");
        }

        [Fact]
        public async Task UpdateProjectMemberRole_OverridesRouteParams_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid roleId = Guid.NewGuid();
            UpdateProjectMemberRoleCommand request = new UpdateProjectMemberRoleCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                UserId = Guid.Empty,
                RoleId = roleId
            };

            IActionResult result = await sut.UpdateProjectMemberRole(tenantId, projectId, userId, request);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateProjectMemberRoleCommand>(c =>
                c.TenantId == tenantId
                && c.ProjectId == projectId
                && c.UserId == userId
                && c.RoleId == roleId);
        }
    }
}
