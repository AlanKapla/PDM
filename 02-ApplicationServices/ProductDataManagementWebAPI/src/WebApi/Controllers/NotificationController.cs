using CQRS.Notifications.GetUnreadNotifications;
using CQRS.Notifications.GetAllNotifications;
using CQRS.Notifications.MarkNotificationAsRead;
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
        public async Task<IActionResult> GetAll([FromQuery] int limit = 50)
        {
            var result = await Send(new GetAllNotificationsQuery(limit));
            return Ok(result);
        }

        // GET /api/notification/unread - tylko nieprzeczytane
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
