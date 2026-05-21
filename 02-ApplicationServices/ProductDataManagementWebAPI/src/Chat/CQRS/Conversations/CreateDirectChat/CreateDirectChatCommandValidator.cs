using Business.Interfaces.Model;
using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.CreateDirectChat;

public sealed class CreateDirectChatCommandValidator : AbstractValidator<CreateDirectChatCommand>
{
    public CreateDirectChatCommandValidator(ICurrentUser currentUser)
    {
        RuleFor(x => x.TargetUserId)
            .RequiredId()
            .NotCurrentUser(currentUser);
    }
}
