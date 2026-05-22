using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Messages.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message content must not exceed 4000 characters.");
    }
}
