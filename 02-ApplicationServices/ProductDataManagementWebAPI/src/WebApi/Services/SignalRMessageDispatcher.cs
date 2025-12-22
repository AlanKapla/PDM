using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRMessageDispatcher : IMessageDispatcher
    {
        private readonly IHubContext<MessageHub, IMessageClient> hubContext;
        private readonly ILogger<SignalRMessageDispatcher> logger;

        public SignalRMessageDispatcher(
            IHubContext<MessageHub, IMessageClient> hubContext,
            ILogger<SignalRMessageDispatcher> logger)
        {
            this.hubContext = hubContext;
            this.logger = logger;
        }

        public async Task DispatchAsync(MessageDto message, CancellationToken cancellationToken)
        {
            string chatId = message.ChatId.ToString();
            await hubContext.Clients.Group(chatId)
                .ReceiveMessage(message);

            logger.LogInformation(
                "Message {MessageId} dispatched to chat group {ChatId}",
                message.Id,
                chatId);
        }
    }
}
