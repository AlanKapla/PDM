using CQRS.Users.UserAuthStatus;
using CQRS.Users.UserDetails;
using CQRS.Users.UserSyncFromB2C;
using CQRS.Users.UserUpdate;
using CQRS.WorkSchedules.GetUserAssignedWorks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class UserControllerTests : ControllerTestBase
    {
        private readonly UserController sut;

        public UserControllerTests()
        {
            sut = new UserController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetAuthStatus_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetAuthStatus();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UserAuthStatusQuery>();
        }

        [Fact]
        public async Task SyncFromB2C_ReturnsOk_AndSendsCommand()
        {
            IActionResult result = await sut.SyncFromB2C();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UserSyncFromB2CCommand>();
        }

        [Fact]
        public async Task UpdateMe_ReturnsOk_AndSendsCommand()
        {
            UserUpdateCommand command = new UserUpdateCommand("First", "Last", null, null, null, null, null, null, null);

            IActionResult result = await sut.UpdateMe(command);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UserUpdateCommand>();
        }

        [Fact]
        public async Task GetUserDetails_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetUserDetails();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UserDetailsQuery>();
        }

        [Fact]
        public async Task GetMyAssignedWorks_ReturnsOk_WithRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid projectId = Guid.NewGuid();

            IActionResult result = await sut.GetMyAssignedWorks(tenantId, projectId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUserAssignedWorksQuery>(q =>
                q.TenantId == tenantId && q.ProjectId == projectId);
        }
    }
}
