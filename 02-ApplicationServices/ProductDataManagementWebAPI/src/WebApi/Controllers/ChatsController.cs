using Business.Interfaces.WebModels.Chats;
using Business.Interfaces.WebModels.Chats.Requests;
using Chat.CQRS.Conversations.AddChatMember;
using Chat.CQRS.Conversations.CreateDirectChat;
using Chat.CQRS.Conversations.CreateGroupChat;
using Chat.CQRS.Conversations.DeleteChat;
using Chat.CQRS.Conversations.FindChatsByMembers;
using Chat.CQRS.Conversations.GetAvailableMembers;
using Chat.CQRS.Conversations.GetChatMembers;
using Chat.CQRS.Conversations.GetProjectMates;
using Chat.CQRS.Conversations.GetUserChats;
using Chat.CQRS.Conversations.LeaveChat;
using Chat.CQRS.Conversations.RemoveChatMember;
using Chat.CQRS.Conversations.RenameGroupChat;
using Chat.CQRS.Conversations.SearchChats;
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
    /// Chat operations. All endpoints require authentication only ([Authorize]).
    /// Access control is membership-based and enforced by CQRS handlers.
    /// Direct chats are cross-tenant; tenant chats are project-scoped.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api")]
    public class ChatsController : BaseApiController
    {
        public ChatsController(IMediator mediator) : base(mediator)
        {
        }

        // ── Direct chats ──────────────────────────────────────────────────────

        /// <summary>Returns the current user's direct chats (cross-tenant).</summary>
        [HttpGet("chats/direct")]
        public async Task<IActionResult> GetDirectChats(CancellationToken cancellationToken = default)
        {
            GetUserChatsQuery query = new GetUserChatsQuery(TenantId: null, DirectChatsOnly: true);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Creates a 1-1 direct chat with the target user. Idempotent.</summary>
        [HttpPost("chats/direct")]
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

        /// <summary>Finds direct chats containing the current user and every specified member.</summary>
        [HttpGet("chats/direct/by-members")]
        public async Task<IActionResult> FindChatsByMembers(
            [FromQuery] List<Guid> memberIds,
            CancellationToken cancellationToken = default)
        {
            FindChatsByMembersQuery query = new FindChatsByMembersQuery(memberIds);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Current user leaves a direct chat.</summary>
        [HttpPost("chats/direct/{chatId}/leave")]
        public async Task<IActionResult> LeaveDirectChat(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            LeaveChatCommand command = new LeaveChatCommand { TenantId = null, ChatId = chatId };
            await Send(command);
            return NoContent();
        }

        /// <summary>Returns cursor-paginated messages for a direct chat.</summary>
        [HttpGet("chats/direct/{chatId}/messages")]
        public async Task<IActionResult> GetDirectChatMessages(
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
        [HttpPost("chats/direct/{chatId}/messages")]
        public async Task<IActionResult> SendDirectMessage(
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
            return CreatedAtAction(nameof(GetDirectChatMessages), new { chatId }, new { id = messageId });
        }

        /// <summary>Edits a message in a direct chat. Author only, within the edit window.</summary>
        [HttpPatch("chats/direct/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> EditDirectMessage(
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
        [HttpDelete("chats/direct/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteDirectMessage(
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
        [HttpPut("chats/direct/{chatId}/read")]
        public async Task<IActionResult> MarkDirectChatAsRead(
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            MarkAsReadCommand command = new MarkAsReadCommand { TenantId = null, ChatId = chatId };
            await Send(command);
            return NoContent();
        }

        // ── Tenant chats ──────────────────────────────────────────────────────

        /// <summary>Returns chats for the current user within the given tenant.</summary>
        [HttpGet("tenants/{tenantId}/chats")]
        public async Task<IActionResult> GetTenantChats(
            [FromRoute] Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            GetUserChatsQuery query = new GetUserChatsQuery(TenantId: tenantId);
            List<ChatWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Searches chats within the tenant by phrase. Matches chat name, member full names, and message content.</summary>
        [HttpGet("tenants/{tenantId}/chats/search")]
        public async Task<IActionResult> SearchChats(
            [FromRoute] Guid tenantId,
            [FromQuery] string q,
            CancellationToken cancellationToken = default)
        {
            SearchChatsQuery query = new SearchChatsQuery(q, tenantId);
            List<ChatSearchResultWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Returns project contact groups limited to the current tenant.</summary>
        [HttpGet("tenants/{tenantId}/chats/contacts")]
        public async Task<IActionResult> GetContacts(
            [FromRoute] Guid tenantId,
            CancellationToken cancellationToken = default)
        {
            GetProjectMatesQuery query = new GetProjectMatesQuery(tenantId);
            List<ProjectContactsGroupWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Creates a group chat (3+ members) bound to a project within the tenant.</summary>
        [HttpPost("tenants/{tenantId}/chats")]
        public async Task<IActionResult> CreateGroupChat(
            [FromRoute] Guid tenantId,
            [FromBody] CreateChatRequest body,
            CancellationToken cancellationToken = default)
        {
            CreateGroupChatCommand command = new CreateGroupChatCommand
            {
                TenantId = tenantId,
                ProjectId = body.ProjectId,
                MemberUserIds = body.MemberUserIds ?? new List<Guid>(),
                Name = body.Name
            };
            CreateChatResultWeb result = await Send(command);
            return CreatedAtAction(nameof(GetTenantChats), new { tenantId }, result);
        }

        /// <summary>Renames a group chat. Admin only.</summary>
        [HttpPatch("tenants/{tenantId}/chats/{chatId}")]
        public async Task<IActionResult> RenameChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] RenameChatRequest body,
            CancellationToken cancellationToken = default)
        {
            RenameGroupChatCommand command = new RenameGroupChatCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                NewName = body.NewName
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Deletes a group chat (admin only) along with its messages and members.</summary>
        [HttpDelete("tenants/{tenantId}/chats/{chatId}")]
        public async Task<IActionResult> DeleteChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            DeleteChatCommand command = new DeleteChatCommand
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Returns all members of a chat. Requires membership.</summary>
        [HttpGet("tenants/{tenantId}/chats/{chatId}/members")]
        public async Task<IActionResult> GetChatMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            GetChatMembersQuery query = new GetChatMembersQuery
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            List<ChatMemberWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Returns project members not yet in this chat. Group chats only.</summary>
        [HttpGet("tenants/{tenantId}/chats/{chatId}/available-members")]
        public async Task<IActionResult> GetAvailableMembers(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            GetAvailableMembersQuery query = new GetAvailableMembersQuery
            {
                TenantId = tenantId,
                ChatId = chatId
            };
            List<AvailableMemberWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Adds a member to a group chat (admin only).</summary>
        [HttpPost("tenants/{tenantId}/chats/{chatId}/members")]
        public async Task<IActionResult> AddChatMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] AddChatMemberRequest body,
            CancellationToken cancellationToken = default)
        {
            AddChatMemberCommand command = new AddChatMemberCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                UserId = body.UserId,
                ProjectId = body.ProjectId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Removes a member from a group chat. Self-removal or admin removal of non-admin.</summary>
        [HttpDelete("tenants/{tenantId}/chats/{chatId}/members/{userId}")]
        public async Task<IActionResult> RemoveChatMember(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid userId,
            CancellationToken cancellationToken = default)
        {
            RemoveChatMemberCommand command = new RemoveChatMemberCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                UserId = userId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Current user leaves a group chat. Admin leaving dissolves the group.</summary>
        [HttpPost("tenants/{tenantId}/chats/{chatId}/leave")]
        public async Task<IActionResult> LeaveTenantChat(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            LeaveChatCommand command = new LeaveChatCommand { TenantId = tenantId, ChatId = chatId };
            await Send(command);
            return NoContent();
        }

        /// <summary>Returns cursor-paginated messages for a tenant chat.</summary>
        [HttpGet("tenants/{tenantId}/chats/{chatId}/messages")]
        public async Task<IActionResult> GetTenantChatMessages(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromQuery] Guid? before = null,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            GetChatMessagesQuery query = new GetChatMessagesQuery
            {
                TenantId = tenantId,
                ChatId = chatId,
                Before = before,
                PageSize = pageSize
            };
            List<MessageWeb> result = await Send(query);
            return Ok(result);
        }

        /// <summary>Sends a message to a tenant chat. Requires membership.</summary>
        [HttpPost("tenants/{tenantId}/chats/{chatId}/messages")]
        public async Task<IActionResult> SendTenantMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromBody] SendMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            SendMessageCommand command = new SendMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                Content = body.Content,
                ReplyToMessageId = body.ReplyToMessageId
            };
            Guid messageId = await Send(command);
            return CreatedAtAction(nameof(GetTenantChatMessages), new { tenantId, chatId }, new { id = messageId });
        }

        /// <summary>Edits a message in a tenant chat. Author only, within the configured time window.</summary>
        [HttpPatch("tenants/{tenantId}/chats/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> EditTenantMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            [FromBody] EditMessageRequest body,
            CancellationToken cancellationToken = default)
        {
            EditMessageCommand command = new EditMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                MessageId = messageId,
                NewContent = body.Content
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Soft-deletes a message in a tenant chat. Author or chat admin only.</summary>
        [HttpDelete("tenants/{tenantId}/chats/{chatId}/messages/{messageId}")]
        public async Task<IActionResult> DeleteTenantMessage(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            [FromRoute] Guid messageId,
            CancellationToken cancellationToken = default)
        {
            DeleteMessageCommand command = new DeleteMessageCommand
            {
                TenantId = tenantId,
                ChatId = chatId,
                MessageId = messageId
            };
            await Send(command);
            return NoContent();
        }

        /// <summary>Marks all messages in a tenant chat as read for the current user.</summary>
        [HttpPut("tenants/{tenantId}/chats/{chatId}/read")]
        public async Task<IActionResult> MarkTenantChatAsRead(
            [FromRoute] Guid tenantId,
            [FromRoute] Guid chatId,
            CancellationToken cancellationToken = default)
        {
            MarkAsReadCommand command = new MarkAsReadCommand { TenantId = tenantId, ChatId = chatId };
            await Send(command);
            return NoContent();
        }
    }
}
