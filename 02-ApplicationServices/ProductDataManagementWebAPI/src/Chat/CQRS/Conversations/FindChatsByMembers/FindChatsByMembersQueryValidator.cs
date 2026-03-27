using FluentValidation;

namespace Chat.CQRS.Conversations.FindChatsByMembers;

public sealed class FindChatsByMembersQueryValidator : AbstractValidator<FindChatsByMembersQuery>
{
    public FindChatsByMembersQueryValidator()
    {
        RuleFor(x => x.MemberUserIds)
            .NotNull().WithMessage("MemberUserIds is required.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one member must be specified.");
    }
}
