using FluentValidation;

namespace Chat.CQRS.Conversations.DeleteChat;

public sealed class DeleteChatCommandValidator : AbstractValidator<DeleteChatCommand>
{
    public DeleteChatCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");
    }
}
