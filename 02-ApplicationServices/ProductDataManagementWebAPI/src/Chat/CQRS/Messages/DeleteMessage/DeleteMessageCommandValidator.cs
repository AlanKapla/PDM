using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Messages.DeleteMessage;

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.ChatId).RequiredId();

        RuleFor(x => x.MessageId).RequiredId();
    }
}
