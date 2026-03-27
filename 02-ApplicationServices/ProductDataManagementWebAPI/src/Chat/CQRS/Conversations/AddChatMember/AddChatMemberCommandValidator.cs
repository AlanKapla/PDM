using FluentValidation;

namespace Chat.CQRS.Conversations.AddChatMember;

public sealed class AddChatMemberCommandValidator : AbstractValidator<AddChatMemberCommand>
{
    public AddChatMemberCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");
    }
}
