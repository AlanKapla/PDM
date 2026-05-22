using System.Text.Json;
using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.SendMessage
{
    public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, Guid>
    {
        private readonly IRepository<MessageHistory> messageRepo;
        private readonly ICurrentUser currentUser;
        private readonly IQueueStorageService queueStorage;

        public SendMessageCommandHandler(
            IRepository<MessageHistory> messageRepo,
            ICurrentUser currentUser,
            IQueueStorageService queueStorage)
        {
            this.messageRepo = messageRepo;
            this.currentUser = currentUser;
            this.queueStorage = queueStorage;
        }

        public async Task<Guid> Handle(SendMessageCommand request, CancellationToken cancellationToken)
        {
            MessageHistory message = MessageHistory.Create(
                chatId: request.ChatId,
                authorId: currentUser.Id,
                content: request.Content,
                replyToId: null);

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
