using FluentValidation;

namespace Chat.CQRS.Conversations.RemoveChatMember;

public sealed class RemoveChatMemberCommandValidator : AbstractValidator<RemoveChatMemberCommand>
{
    public RemoveChatMemberCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
