using Business.Interfaces.DTO;
using Business.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;

namespace WebApi.Services
{
    public class SignalRMessageDispatcher : IMessageDispatcher
    {
        private readonly IHubContext<MessageHub, IMessageClient> hubContext;

        public SignalRMessageDispatcher(IHubContext<MessageHub, IMessageClient> hubContext)
        {
            this.hubContext = hubContext;
        }

        public async Task DispatchAsync(MessageDto message, CancellationToken cancellationToken)
        {
            string chatId = message.ChatId.ToString();
            await hubContext.Clients.Group(chatId)
                .ReceiveMessage(message);
        }
    }
}
