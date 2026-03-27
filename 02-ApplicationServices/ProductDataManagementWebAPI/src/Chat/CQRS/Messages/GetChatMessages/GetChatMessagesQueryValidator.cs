using FluentValidation;

namespace Chat.CQRS.Messages.GetChatMessages;

public sealed class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100.");
    }
}
