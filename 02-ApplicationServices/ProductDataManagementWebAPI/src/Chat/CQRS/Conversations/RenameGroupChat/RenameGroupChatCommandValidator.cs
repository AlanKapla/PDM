using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.RenameGroupChat;

public sealed class RenameGroupChatCommandValidator : AbstractValidator<RenameGroupChatCommand>
{
    public RenameGroupChatCommandValidator()
    {
        RuleFor(x => x.TenantId).RequiredId();
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(200).WithMessage("Chat name must not exceed 200 characters.");
    }
}
