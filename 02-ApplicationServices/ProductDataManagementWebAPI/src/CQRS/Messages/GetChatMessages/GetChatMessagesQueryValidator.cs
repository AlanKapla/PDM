using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Messages.GetChatMessages
{
    public class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
    {
        private readonly IReadRepository<Chat> chatRepo;
        private readonly IRepository<ChatMember> chatMemberRepo;
        private readonly ICurrentUser currentUser;

        public GetChatMessagesQueryValidator(
            IReadRepository<Chat> chatRepo,
            IRepository<ChatMember> chatMemberRepo,
            ICurrentUser currentUser)
        {
            this.chatRepo = chatRepo;
            this.chatMemberRepo = chatMemberRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.ChatId)
                .NotEmpty().WithMessage("ChatId is required");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");

            RuleFor(x => x.ChatId)
                .MustAsync(ChatMustExist)
                .WithMessage("Chat not found");

            RuleFor(x => x.ChatId)
                .MustAsync(UserMustBeChatMember)
                .WithMessage("User is not a member of this chat");
        }

        private async Task<bool> ChatMustExist(Guid chatId, CancellationToken cancellationToken)
        {
            var chat = await chatRepo.GetFirstBySearch(
                c => c.Id == chatId,
                cancellationToken);

            return chat != null;
        }

        private async Task<bool> UserMustBeChatMember(Guid chatId, CancellationToken cancellationToken)
        {
            var member = await chatMemberRepo.GetFirstBySearch(
                cm => cm.ChatId == chatId && cm.UserId == currentUser.Id);

            return member != null;
        }
    }
}
