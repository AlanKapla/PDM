using CQRS.Notifications.GetUnreadNotifications;
using CQRS.Notifications.MarkNotificationAsRead;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class NotificationController : BaseApiController
    {
        public NotificationController(IMediator mediator) : base(mediator) { }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var result = await Send(new GetUnreadNotificationsQuery());
            return Ok(result);
        }

        [HttpPut("{notificationId}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            await Send(new MarkNotificationAsReadCommand(notificationId));
            return NoContent();
        }
    }
}