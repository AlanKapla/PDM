using Business.Interfaces.Constants;
using Business.Interfaces.WebModels.ProjectDashboard;
using CQRS.ProjectDashboard.GetProjectDashboard;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class ProjectDashboardControllerTests : ControllerTestBase
    {
        private readonly ProjectDashboardController sut;

        public ProjectDashboardControllerTests()
        {
            sut = new ProjectDashboardController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetProjectDashboard_ReturnsOk_WithRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetProjectDashboard(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetProjectDashboardQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId);
        }
    }
}
