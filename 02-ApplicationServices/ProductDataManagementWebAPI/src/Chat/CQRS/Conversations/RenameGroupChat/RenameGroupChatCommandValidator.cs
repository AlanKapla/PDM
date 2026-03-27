using FluentValidation;

namespace Chat.CQRS.Conversations.RenameGroupChat;

public sealed class RenameGroupChatCommandValidator : AbstractValidator<RenameGroupChatCommand>
{
    public RenameGroupChatCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.NewName)
            .NotEmpty().WithMessage("New name is required.")
            .MaximumLength(200).WithMessage("Chat name must not exceed 200 characters.");
    }
}
