using FluentValidation;

namespace Chat.CQRS.Conversations.GetAvailableMembers;

public sealed class GetAvailableMembersQueryValidator : AbstractValidator<GetAvailableMembersQuery>
{
    public GetAvailableMembersQueryValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty().WithMessage("ChatId is required.");
    }
}
