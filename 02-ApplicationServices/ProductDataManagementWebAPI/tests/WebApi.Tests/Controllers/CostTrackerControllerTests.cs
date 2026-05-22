using Business.Interfaces.WebModels.CostTrackers;
using CQRS.CostTrackers.CreateTrackedCost;
using CQRS.CostTrackers.DeleteTrackedCost;
using CQRS.CostTrackers.UpdateTrackedCost;
using CQRS.Projects.UpdateProjectBudget;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class CostTrackerControllerTests : ControllerTestBase
    {
        private readonly CostTrackerController sut;

        public CostTrackerControllerTests()
        {
            sut = new CostTrackerController(MediatorMock.Object);
        }

        [Fact]
        public async Task CreateTrackedCost_OverridesIds_AndReturnsCreated()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid newCostId = Guid.NewGuid();
            CreateTrackedCostCommand command = new CreateTrackedCostCommand { Name = "X" };
            SetupMediatorReturns<CreateTrackedCostCommand, TrackedCostWeb>(
                WebModelFactory.TrackedCost(newCostId));

            IActionResult result = await sut.CreateTrackedCost(tenantId, projectId, command);

            result.Should().BeOfType<CreatedResult>();
            VerifyMediatorCalledOnce<CreateTrackedCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.Name == "X");
        }

        [Fact]
        public async Task UpdateTrackedCost_OverridesIds_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();
            UpdateTrackedCostCommand command = new UpdateTrackedCostCommand { Name = "X" };
            SetupMediatorReturns<UpdateTrackedCostCommand, TrackedCostWeb>(
                WebModelFactory.TrackedCost(costId));

            IActionResult result = await sut.UpdateTrackedCost(tenantId, projectId, costId, command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpdateTrackedCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }

        [Fact]
        public async Task UpdateTrackerBudget_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            UpdateProjectBudgetCommand command = new UpdateProjectBudgetCommand
            {
                BudgetNet = 100m
            };

            IActionResult result = await sut.UpdateTrackerBudget(tenantId, projectId, command);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateProjectBudgetCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.BudgetNet == 100m);
        }

        [Fact]
        public async Task DeleteTrackedCost_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();
            Guid costId = Guid.NewGuid();

            IActionResult result = await sut.DeleteTrackedCost(tenantId, projectId, costId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteTrackedCostCommand>(c =>
                c.TenantId == tenantId && c.ProjectId == projectId && c.CostId == costId);
        }
    }
}
