using Business.Interfaces.Constants;
using CQRS.Chats.CreateChat;
using CQRS.Chats.GetProjectChats;
using CQRS.Messages.GetChatMessages;
using CQRS.Messages.MarkMessagesAsRead;
using CQRS.Messages.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/tenants/{tenantId}/projects/{projectId}/[controller]")]
    [ApiController]
    public class ChatController(IMediator mediator) : BaseApiController(mediator)
    {
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetProjectChats([FromRoute] Guid projectId)
        {
            var query = new GetProjectChatsQuery(projectId);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> CreateChat(
            [FromRoute] Guid projectId,
            [FromBody] CreateChatCommand command)
        {
            var result = await Send(command);
            return CreatedAtAction(nameof(GetProjectChats), new { projectId }, result);
        }

        [HttpGet("{chatId}/messages")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> GetChatMessages(
            [FromRoute] Guid chatId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = new GetChatMessagesQuery(chatId, pageNumber, pageSize);
            var result = await Send(query);
            return Ok(result);
        }

        [HttpPost("{chatId}/messages")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid chatId,
            [FromBody] SendMessageCommand command)
        {
            var result = await Send(command);
            return CreatedAtAction(nameof(GetChatMessages), new { chatId }, result);
        }

        [HttpPut("{chatId}/read")]
        [Authorize(Policy = PermissionCodes.ProjectView)]
        public async Task<IActionResult> MarkMessagesAsRead([FromRoute] Guid chatId)
        {
            var command = new MarkMessagesAsReadCommand(chatId);
            var result = await Send(command);
            return Ok(new { markedAsRead = result });
        }
    }
}
