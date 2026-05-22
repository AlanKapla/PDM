using Business.Interfaces.WebModels.Chats;
using Business.Interfaces.WebModels.Chats.Requests;
using Chat.CQRS.Conversations.CreateDirectChat;
using Chat.CQRS.Conversations.FindChatsByMembers;
using Chat.CQRS.Conversations.GetUserChats;
using Chat.CQRS.Conversations.LeaveChat;
using Chat.CQRS.Messages.DeleteMessage;
using Chat.CQRS.Messages.EditMessage;
using Chat.CQRS.Messages.GetChatMessages;
using Chat.CQRS.Messages.MarkAsRead;
using Chat.CQRS.Messages.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    /// <summary>
    /// Cross-tenant direct (1-1) chats. Authorization is membership-based — there
    /// is no tenant policy here. Group/project chats are served by
    /// <see cref="TenantChatsController"/>.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/chats/direct")]
    public class DirectChatsController : BaseApiController
    {
        public DirectChatsController(IMediator mediator) : base(mediator)
        {
        }

        /// <summary>Returns the current user's direct chats (cross-tenant).</summary>
        [HttpGet]
        public async Task<IActionResult> GetDirectChats(CancellationToken cancellationToken = default)
        {
            GetUserChatsQuery query = new GetUserChatsQuery(TenantId: null, DirectChatsOnly: true);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Creates a 1-1 direct chat with the target user. Idempotent.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateDirectChat(
            [FromBody] CreateDirectChatRequest body,
            CancellationToken cancellationToken = default)
        {
            CreateDirectChatCommand command = new CreateDirectChatCommand
            {
                TargetUserId = body.TargetUserId
            };
            CreateChatResultWeb result = await Send(command);
            return CreatedAtAction(nameof(GetDirectChats), new { id = result.Id }, result);
        }

        /// <summary>Finds chats containing the current user and every specified member.</summary>
        [HttpGet("by-members")]
        public async Task<IActionResult> FindChatsByMembers(
            [FromQuery] List<Guid> memberIds,
            CancellationToken cancellationToken = default)
        {
            FindChatsByMembersQuery query = new FindChatsByMembersQuery(memberIds);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Returns cursor-paginated messages for a direct chat.</summary>
        [HttpGet("{chatId}/messages")]
        public async Task<IActionResult> GetChatMessages(
            [FromRoute] Guid chatId,
            [FromQuery] Guid? before = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            GetChatMessagesQuery query = new GetChatMessagesQuery
            {
                TenantId = null,
                ChatId = chatId,
                Before = before,
                PageSize = pageSize
            };
            List<MessageWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Sends a message to a direct chat.</summary>
        [HttpPost("{chatId}/messages")]
        public async Task<IActionResult> SendMessage(
            [FromRoute] Guid chatId,
            [FromBody] SendMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            SendMessageCommand command = new SendMessageCommand
            {
                TenantId = null,
                ChatId = chatId,
                Content = body.Content,
                ReplyToMessageId = body.ReplyToMessageId
            };
            Guid messageId = await Send(command);
            return CreatedAtAction(nameof(GetChatMessages), new { chatId }, new { id = messageId });
        }

        /// <summary>Edits a message in a direct chat. Author only, within the edit window.</summary>
        [HttpPatch("{chatId}/messages/{messageId}")]
        public async Task<IActionResult> EditMessage(
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            EditMessageCommand command = new EditMessageCommand
            {
                TenantId = null,
                ChatId = chatId,
                MessageId = messageId,
                NewContent = body.Content
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Soft-deletes a message in a direct chat. Author only.</summary>
        [HttpDelete("{chatId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            DeleteMessageCommand command = new DeleteMessageCommand
            {
                TenantId = null,
                ChatId = chatId,
                MessageId = messageId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Marks a direct chat as read for the current user.</summary>
        [HttpPut("{chatId}/read")]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            MarkAsReadCommand command = new MarkAsReadCommand
            {
                TenantId = null,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Current user leaves a direct chat (effectively deletes it for them).</summary>
        [HttpPost("{chatId}/leave")]
        public async Task<IActionResult> LeaveChat(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            LeaveChatCommand command = new LeaveChatCommand
            {
                TenantId = null,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }
    }
}
