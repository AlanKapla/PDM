using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Messages.EditMessage;

public sealed class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.MessageId).RequiredId();

        RuleFor(x => x.NewContent)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message content must not exceed 4000 characters.");
    }
}
