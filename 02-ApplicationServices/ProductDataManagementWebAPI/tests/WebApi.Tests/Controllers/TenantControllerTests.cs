using Business.Interfaces.WebModels.Tenants;
using CQRS.Tenants.AcceptTenantInvitation;
using CQRS.Tenants.ActiveInvitations;
using CQRS.Tenants.ChangeActiveTenant;
using CQRS.Tenants.CreateTenant;
using CQRS.Tenants.GetAdminTenants;
using CQRS.Tenants.GetTenantDetails;
using CQRS.Tenants.GetTenantMembers;
using CQRS.Tenants.GetUserTenants;
using CQRS.Tenants.InviteTenantMember;
using CQRS.Tenants.RemoveTenantInvitation;
using CQRS.Tenants.RemoveTenantMember;
using CQRS.Tenants.UpdateTenant;
using CQRS.Tenants.UpdateTenantMemberRole;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class TenantControllerTests : ControllerTestBase
    {
        private readonly TenantController sut;

        public TenantControllerTests()
        {
            sut = new TenantController(MediatorMock.Object);
        }

        [Fact]
        public async Task CreateTenant_ReturnsCreatedAtAction_WithCreatedTenantId()
        {
            Guid newId = Guid.NewGuid();
            CreateTenantCommand request = new CreateTenantCommand { Name = "Acme" };
            SetupMediatorReturns<CreateTenantCommand, TenantDetailsWeb>(WebModelFactory.Tenant(newId));

            IActionResult result = await sut.CreateTenant(request);

            CreatedAtActionResult created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            created.RouteValues!["tenantId"].Should().Be(newId);
            VerifyMediatorCalledOnce<CreateTenantCommand>();
        }

        [Fact]
        public async Task GetMyTenants_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetMyTenants();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUserTenantsQuery>();
        }

        [Fact]
        public async Task GetAdminTenants_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetAdminTenants();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAdminTenantsQuery>();
        }

        [Fact]
        public async Task GetTenantDetails_ReturnsOk_WithTenantId()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetTenantDetails(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetTenantDetailsQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task ChangeActiveTenant_ReturnsOk_AndForwardsCommand()
        {
            ChangeActiveTenantCommand request = new ChangeActiveTenantCommand { TenantId = Guid.NewGuid() };

            IActionResult result = await sut.ChangeActiveTenant(request);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<ChangeActiveTenantCommand>(c => c.TenantId == request.TenantId);
        }

        [Fact]
        public async Task UpdateTenant_OverridesTenantId_AndReturnsOk()
        {
            Guid tenantId = Guid.NewGuid();
            UpdateTenantCommand request = new UpdateTenantCommand
            {
                TenantId = Guid.Empty,
                Name = "X"
            };
            SetupMediatorReturns<UpdateTenantCommand, TenantDetailsWeb>(WebModelFactory.Tenant(tenantId));

            IActionResult result = await sut.UpdateTenant(tenantId, request);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<UpdateTenantCommand>(c => c.TenantId == tenantId && c.Name == "X");
        }

        [Fact]
        public async Task InviteTenantMember_OverridesTenantId_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            InviteTenantMemberCommand request = new InviteTenantMemberCommand
            {
                TenantId = Guid.Empty,
                Email = "a@b.c"
            };

            IActionResult result = await sut.InviteTenantMember(tenantId, request);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<InviteTenantMemberCommand>(c =>
                c.TenantId == tenantId && c.Email == "a@b.c");
        }

        [Fact]
        public async Task GetActiveInvitations_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetActiveInvitations();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<ActiveTenantInvitationsQuery>();
        }

        [Fact]
        public async Task AcceptInvitation_ReturnsNoContent_AndForwardsToken()
        {
            AcceptTenantInvitationCommand request = new AcceptTenantInvitationCommand { Token = "tok" };

            IActionResult result = await sut.AcceptInvitation(request);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<AcceptTenantInvitationCommand>(c => c.Token == "tok");
        }

        [Fact]
        public async Task RemoveInvitation_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid invitationId = Guid.NewGuid();

            IActionResult result = await sut.RemoveInvitation(tenantId, invitationId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RemoveTenantInvitationCommand>(c =>
                c.TenantId == tenantId && c.InvitationId == invitationId);
        }

        [Fact]
        public async Task GetTenantMembers_ReturnsOk_WithTenantId()
        {
            Guid tenantId = Guid.NewGuid();

            IActionResult result = await sut.GetTenantMembers(tenantId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetTenantMembersQuery>(q => q.TenantId == tenantId);
        }

        [Fact]
        public async Task RemoveTenantMember_BuildsCommand_FromRouteParams()
        {
            Guid tenantId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();

            IActionResult result = await sut.RemoveTenantMember(tenantId, userId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<RemoveTenantMemberCommand>(c =>
                c.TenantId == tenantId && c.UserId == userId);
        }

        [Fact]
        public async Task UpdateTenantMemberAdmin_OverridesIds_AndReturnsNoContent()
        {
            Guid tenantId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            UpdateTenantMemberRoleCommand request = new UpdateTenantMemberRoleCommand
            {
                TenantId = Guid.Empty,
                UserId = Guid.Empty,
                IsAdmin = true
            };

            IActionResult result = await sut.UpdateTenantMemberAdmin(tenantId, userId, request);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<UpdateTenantMemberRoleCommand>(c =>
                c.TenantId == tenantId && c.UserId == userId && c.IsAdmin == true);
        }
    }
}
