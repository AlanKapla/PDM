using FluentValidation;

namespace Chat.CQRS.Conversations.SearchChats;

public sealed class SearchChatsQueryValidator : AbstractValidator<SearchChatsQuery>
{
    public SearchChatsQueryValidator()
    {
        RuleFor(x => x.Phrase)
            .NotEmpty().WithMessage("Phrase is required.")
            .MinimumLength(2).WithMessage("Search phrase must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Search phrase must not exceed 200 characters.");
    }
}
