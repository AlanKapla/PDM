using Business.Interfaces.WebModels.Admin;
using Business.Interfaces.WebModels.Users;
using CQRS.Admin.ColdMails.GetColdMailHistory;
using CQRS.Admin.ColdMails.GetColdMailTemplate;
using CQRS.Admin.ColdMails.SendColdMails;
using CQRS.Admin.Users.GetAdminUsers;
using CQRS.Admin.Users.SendWelcomeEmailToUser;
using CQRS.Admin.WelcomeEmails.SendWelcomeEmailsToExistingUsers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class AdminControllerTests : ControllerTestBase
    {
        private readonly AdminController sut;

        public AdminControllerTests()
        {
            sut = new AdminController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetUsers_ReturnsOk_AndSendsQuery()
        {
            IReadOnlyList<AdminUserWeb> expected = Array.Empty<AdminUserWeb>();
            SetupMediatorReturns<GetAdminUsersQuery, IReadOnlyList<AdminUserWeb>>(expected);

            IActionResult result = await sut.GetUsers();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAdminUsersQuery>();
        }

        [Fact]
        public async Task SendWelcomeEmailToUser_ReturnsOk_AndSendsCommand()
        {
            Guid userId = Guid.NewGuid();
            AdminUserWeb expected = new(
                userId, "a@test.com", "Ada", "Nowak", true, "User",
                DateTime.UtcNow, DateTime.UtcNow, null, null, null, null, null, null, null);
            SetupMediatorReturns<SendWelcomeEmailToUserCommand, AdminUserWeb>(expected);

            IActionResult result = await sut.SendWelcomeEmailToUser(userId);

            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expected);
            VerifyMediatorCalledOnce<SendWelcomeEmailToUserCommand>();
        }

        [Fact]
        public async Task SendWelcomeEmailsToExistingUsers_ReturnsOk_AndSendsCommand()
        {
            SendWelcomeEmailsResultWeb expected = new(5, 2);
            SetupMediatorReturns<SendWelcomeEmailsToExistingUsersCommand, SendWelcomeEmailsResultWeb>(expected);

            IActionResult result = await sut.SendWelcomeEmailsToExistingUsers();

            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expected);
            VerifyMediatorCalledOnce<SendWelcomeEmailsToExistingUsersCommand>();
        }

        [Fact]
        public async Task GetColdMailTemplate_ReturnsOk_AndSendsQuery()
        {
            ColdMailTemplateWeb expected = new("<html>{subject}</html>", "https://app.brickly.pro", "Poznaj Brickly");
            SetupMediatorReturns<GetColdMailTemplateQuery, ColdMailTemplateWeb>(expected);

            IActionResult result = await sut.GetColdMailTemplate();

            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expected);
            VerifyMediatorCalledOnce<GetColdMailTemplateQuery>();
        }

        [Fact]
        public async Task SendColdMails_ReturnsOk_AndSendsCommand()
        {
            SendColdMailsCommand command = new()
            {
                Emails = new[] { "prospect@example.com" },
                Subject = "Hello",
                Body = "Body"
            };
            SendColdMailsResultWeb expected = new(
                Guid.NewGuid(),
                1,
                0,
                new[] { new ColdMailSendItemWeb("prospect@example.com", "Queued", null) });
            SetupMediatorReturns<SendColdMailsCommand, SendColdMailsResultWeb>(expected);

            IActionResult result = await sut.SendColdMails(command);

            OkObjectResult okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expected);
            VerifyMediatorCalledOnce<SendColdMailsCommand>();
        }

        [Fact]
        public async Task GetColdMailHistory_ReturnsOk_AndSendsQuery()
        {
            IReadOnlyList<ColdMailHistoryWeb> expected = Array.Empty<ColdMailHistoryWeb>();
            SetupMediatorReturns<GetColdMailHistoryQuery, IReadOnlyList<ColdMailHistoryWeb>>(expected);

            IActionResult result = await sut.GetColdMailHistory("acme");

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetColdMailHistoryQuery>(q => q.Email == "acme");
        }
    }
}
