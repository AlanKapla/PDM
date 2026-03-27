using FluentValidation;

namespace Chat.CQRS.Messages.EditMessage;

public sealed class EditMessageCommandValidator : AbstractValidator<EditMessageCommand>
{
    public EditMessageCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.MessageId)
            .NotEmpty().WithMessage("MessageId is required.");

        RuleFor(x => x.NewContent)
            .NotEmpty().WithMessage("Message content cannot be empty.")
            .MaximumLength(4000).WithMessage("Message content must not exceed 4000 characters.");
    }
}
