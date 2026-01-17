using CQRS.Notifications.GetUnreadNotifications;
using CQRS.Notifications.GetAllNotifications;
using CQRS.Notifications.MarkNotificationAsRead;
using CQRS.Notifications.MarkAllNotificationsAsRead;
using CQRS.Notifications.GetUnreadCounter;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notification")]
    public class NotificationController : BaseApiController
    {
        public NotificationController(IMediator mediator) : base(mediator) { }

        // GET /api/notification - wszystkie powiadomienia (historia)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int take = 50, [FromQuery] int skip = 0)
        {
            var result = await Send(new GetAllNotificationsQuery(take, skip));
            return Ok(result);
        }

        // GET /api/notification/unread - tylko nieprzeczytane
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread([FromQuery] int take = 50, [FromQuery] int skip = 0)
        {
            var result = await Send(new GetUnreadNotificationsQuery(take, skip));
            return Ok(result);
        }

        [HttpGet("unread-counter")]
        public async Task<IActionResult> GetUnreadCounter()
        {
            var result = await Send(new GetUnreadCounterQuery());
            return Ok(result);
        }

        [HttpPut("{notificationId}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            await Send(new MarkNotificationAsReadCommand(notificationId));
            return NoContent();
        }

        [HttpPut("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var markedCount = await Send(new MarkAllNotificationsAsReadCommand());
            return Ok(new { markedCount });
        }
    }
}
