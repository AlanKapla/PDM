using CQRS.Roles.GetAvailableRoles;
using Entities.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class RoleControllerTests : ControllerTestBase
    {
        private readonly RoleController sut;

        public RoleControllerTests()
        {
            sut = new RoleController(MediatorMock.Object);
        }

        [Theory]
        [InlineData(RoleScope.Tenant)]
        public async Task GetAvailableRoles_ReturnsOk_AndForwardsScope(RoleScope scope)
        {
            IActionResult result = await sut.GetAvailableRoles(scope);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAvailableRolesQuery>(q => q.Scope == scope);
        }

        [Fact]
        public async Task GetTenantRoles_ReturnsOk_WithTenantScope()
        {
            IActionResult result = await sut.GetTenantRoles();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAvailableRolesQuery>(q => q.Scope == RoleScope.Tenant);
        }
    }
}
