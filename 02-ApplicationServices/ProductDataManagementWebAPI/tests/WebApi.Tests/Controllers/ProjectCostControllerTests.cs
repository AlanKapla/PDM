using Business.Interfaces.Constants;
using CQRS.ProjectCosts.ApproveProjectCost;
using CQRS.ProjectCosts.CreateProjectCost;
using CQRS.ProjectCosts.DeleteProjectCost;
using CQRS.ProjectCosts.GetProjectCosts;
using CQRS.ProjectCosts.RejectProjectCost;
using CQRS.ProjectCosts.SubmitProjectCostForApproval;
using CQRS.ProjectCosts.UpdateProjectCost;
using CQRS.ProjectCosts.WithdrawProjectCost;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class ProjectCostControllerTests : ControllerTestBase
    {
        private readonly ProjectCostController sut;

        public ProjectCostControllerTests()
        {
            sut = new ProjectCostController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetProjectCosts_ReturnsOk_WithRouteParamsAndScope()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectCosts(tenantId, projectId, ResourceScope.Mine);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectCostsQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId && q.Scope == ResourceScope.Mine);
        }

        [Fact]
        public async Task CreateProjectCost_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            CreateProjectCostCommand command = new CreateProjectCostCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                Name = "X"
            };

            IActionResult result = await sut.CreateProjectCost(tenantId, projectId, command);

            result.Should().BeOfType<CreatedResult>();
            VerifyMediatorCalledOnce<CreateProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.Name == "X");
        }

        [Fact]
        public async Task UpdateProjectCost_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();
            UpdateProjectCostCommand command = new UpdateProjectCostCommand
            {
                TenantId = Guid.Empty,
                ProjectId = Guid.Empty,
                CostId = Guid.Empty,
                Name = "X"
            };

            IActionResult result = await sut.UpdateProjectCost(tenantId, projectId, costId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpdateProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task DeleteProjectCost_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.DeleteProjectCost(tenantId, projectId, costId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task SubmitForApproval_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.SubmitForApproval(tenantId, projectId, costId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<SubmitProjectCostForApprovalCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task WithdrawFromApproval_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.WithdrawFromApproval(tenantId, projectId, costId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<WithdrawProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task ApproveCost_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.ApproveCost(tenantId, projectId, costId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<ApproveProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task RejectCost_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.RejectCost(tenantId, projectId, costId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<RejectProjectCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }
    }
}
