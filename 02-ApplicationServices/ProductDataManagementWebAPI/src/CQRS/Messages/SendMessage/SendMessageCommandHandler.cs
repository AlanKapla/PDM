using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.SendMessage
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
    {
        private readonly IRepository<MessageHistory> messageRepo;
        private readonly IReadRepository<Chat> chatRepo;
        private readonly ICurrentUser currentUser;
        private readonly IQueueStorageService queueStorage;

        public SendMessageCommandHandler(
            IRepository<MessageHistory> messageRepo,
            IReadRepository<Chat> chatRepo,
            ICurrentUser currentUser,
            IQueueStorageService queueStorage)
        {
            this.messageRepo = messageRepo;
            this.chatRepo = chatRepo;
            this.currentUser = currentUser;
            this.queueStorage = queueStorage;
        }

        public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            var chat = await chatRepo.GetFirstBySearch(c => c.Id == request.ChatId, cancellationToken);

            MessageHistory message = new MessageHistory
            {
                ChatId = request.ChatId,
                TenantId = chat!.TenantId,
                UserId = currentUser.Id,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            await messageRepo.Insert(message);

            MessageDto messageDto = new MessageDto
            {
                Id = message.Id,
                ChatId = message.ChatId,
                UserId = message.UserId,
                Content = message.Content,
                CreatedAt = message.CreatedAt
            };

            string messageJson = JsonSerializer.Serialize(messageDto);
            await queueStorage.EnqueueAsync(QueueNames.MessageSend, messageJson, cancellationToken: cancellationToken);

            return message.Id;
        }
    }
}
