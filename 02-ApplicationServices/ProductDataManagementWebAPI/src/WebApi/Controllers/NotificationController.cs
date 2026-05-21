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
            GetAllNotificationsQuery query = new GetAllNotificationsQuery
            {
                Take = take,
                Skip = skip
            };
            IEnumerable<Business.Interfaces.WebModels.Notifications.NotificationWeb> result = await Send(query);
            return Ok(result);
        }

        // GET /api/notification/unread - tylko nieprzeczytane
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread([FromQuery] int take = 50, [FromQuery] int skip = 0)
        {
            GetUnreadNotificationsQuery query = new GetUnreadNotificationsQuery
            {
                Take = take,
                Skip = skip
            };
            IEnumerable<Business.Interfaces.WebModels.Notifications.NotificationWeb> result = await Send(query);
            return Ok(result);
        }

        [HttpGet("unread-counter")]
        public async Task<IActionResult> GetUnreadCounter()
        {
            int counter = await Send(new GetUnreadCounterQuery());
            return Ok(counter);
        }

        [HttpPut("{notificationId}/mark-as-read")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            MarkNotificationAsReadCommand command = new MarkNotificationAsReadCommand
            {
                NotificationId = notificationId
            };
            await Send(command);
            return NoContent();
        }

        [HttpPut("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int markedCount = await Send(new MarkAllNotificationsAsReadCommand());
            return Ok(new { markedCount });
        }
    }
}
