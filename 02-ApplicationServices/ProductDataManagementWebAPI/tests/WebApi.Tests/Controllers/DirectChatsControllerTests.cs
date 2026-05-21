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
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers;

namespace WebApi.Tests.Controllers
{
    public class DirectChatsControllerTests : ControllerTestBase
    {
        private readonly DirectChatsController sut;

        public DirectChatsControllerTests()
        {
            sut = new DirectChatsController(MediatorMock.Object);
        }

        [Fact]
        public async Task GetDirectChats_ReturnsOk_WithNullTenantDirectChatsOnly()
        {
            IActionResult result = await sut.GetDirectChats();

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetUserChatsQuery>(q => q.TenantId == null && q.DirectChatsOnly == true);
        }

        [Fact]
        public async Task CreateDirectChat_ReturnsCreated_WithTargetUserId()
        {
            Guid targetUserId = Guid.NewGuid();
            CreateDirectChatRequest body = new CreateDirectChatRequest { TargetUserId = targetUserId };
            SetupMediatorReturns<CreateDirectChatCommand, Business.Interfaces.WebModels.Chats.CreateChatResultWeb>(
                WebModelFactory.ChatResult(Guid.NewGuid()));

            IActionResult result = await sut.CreateDirectChat(body);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<CreateDirectChatCommand>(c => c.TargetUserId == targetUserId);
        }

        [Fact]
        public async Task FindChatsByMembers_PassesMemberIds_AndReturnsOk()
        {
            List<Guid> memberIds = new List<Guid> { Guid.NewGuid() };

            IActionResult result = await sut.FindChatsByMembers(memberIds);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<FindChatsByMembersQuery>(q => q.MemberUserIds.Count == 1);
        }

        [Fact]
        public async Task GetChatMessages_BuildsQuery_AndReturnsOk()
        {
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.GetChatMessages(chatId);

            result.Should().BeOfType<OkObjectResult>();
            VerifyMediatorCalledOnce<GetChatMessagesQuery>(q => q.ChatId == chatId && q.TenantId == null);
        }

        [Fact]
        public async Task SendMessage_BuildsCommand_AndReturnsCreated()
        {
            Guid chatId = Guid.NewGuid();
            SendMessageRequest body = new SendMessageRequest("hello");

            IActionResult result = await sut.SendMessage(chatId, body);

            result.Should().BeOfType<CreatedAtActionResult>();
            VerifyMediatorCalledOnce<SendMessageCommand>(c => c.ChatId == chatId && c.TenantId == null && c.Content == "hello");
        }

        [Fact]
        public async Task EditMessage_BuildsCommand_AndReturnsNoContent()
        {
            Guid chatId = Guid.NewGuid();
            Guid messageId = Guid.NewGuid();
            EditMessageRequest body = new EditMessageRequest("updated");

            IActionResult result = await sut.EditMessage(chatId, messageId, body);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<EditMessageCommand>(c => c.ChatId == chatId && c.MessageId == messageId && c.TenantId == null);
        }

        [Fact]
        public async Task DeleteMessage_BuildsCommand_AndReturnsNoContent()
        {
            Guid chatId = Guid.NewGuid();
            Guid messageId = Guid.NewGuid();

            IActionResult result = await sut.DeleteMessage(chatId, messageId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<DeleteMessageCommand>(c => c.ChatId == chatId && c.MessageId == messageId && c.TenantId == null);
        }

        [Fact]
        public async Task MarkAsRead_BuildsCommand_AndReturnsNoContent()
        {
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.MarkAsRead(chatId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<MarkAsReadCommand>(c => c.ChatId == chatId && c.TenantId == null);
        }

        [Fact]
        public async Task LeaveChat_BuildsCommand_AndReturnsNoContent()
        {
            Guid chatId = Guid.NewGuid();

            IActionResult result = await sut.LeaveChat(chatId);

            result.Should().BeOfType<NoContentResult>();
            VerifyMediatorCalledOnce<LeaveChatCommand>(c => c.ChatId == chatId && c.TenantId == null);
        }
    }
}
