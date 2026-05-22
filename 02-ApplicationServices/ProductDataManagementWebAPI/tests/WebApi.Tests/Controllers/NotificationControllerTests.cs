using CQRS.Notifications.GetAllNotifications;
using CQRS.Notifications.GetUnreadCounter;
using CQRS.Notifications.GetUnreadNotifications;
using CQRS.Notifications.MarkAllNotificationsAsRead;
using CQRS.Notifications.MarkNotificationAsRead;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class NotificationControllerTests : ControllerTestBase
    {
        private readonly NotificationController sut;

        public NotificationControllerTests()
        {
            sut = new NotificationController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetAll_PassesPagingArgs_ToQuery()
        {
            IActionResult result = await sut.GetAll(take: 25, skip: 10);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetAllNotificationsQuery>(q => q.Take == 25 && q.Skip == 10);
        }

        [Fact]
        public async Task GetAll_UsesDefaultPaging_WhenNotProvided()
        {
            await sut.GetAll();

            VerifyMediatorCalledOnce<GetAllNotificationsQuery>(q => q.Take == 50 && q.Skip == 0);
        }

        [Fact]
        public async Task GetUnread_PassesPagingArgs_ToQuery()
        {
            IActionResult result = await sut.GetUnread(take: 5, skip: 2);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUnreadNotificationsQuery>(q => q.Take == 5 && q.Skip == 2);
        }

        [Fact]
        public async Task GetUnreadCounter_ReturnsOk_AndSendsQuery()
        {
            IActionResult result = await sut.GetUnreadCounter();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUnreadCounterQuery>();
        }

        [Fact]
        public async Task MarkAsRead_ReturnsNoContent_AndSendsCommandWithId()
        {
            Guid id = Guid.NewGuid();

            IActionResult result = await sut.MarkAsRead(id);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MarkNotificationAsReadCommand>(c => c.NotificationId == id);
        }

        [Fact]
        public async Task MarkAllAsRead_ReturnsOk_AndSendsCommand()
        {
            IActionResult result = await sut.MarkAllAsRead();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<MarkAllNotificationsAsReadCommand>();
        }
    }
}
