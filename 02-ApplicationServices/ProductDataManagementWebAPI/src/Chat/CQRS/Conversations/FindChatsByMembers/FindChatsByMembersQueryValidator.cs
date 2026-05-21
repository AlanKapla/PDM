using CQRS.Extensions;
using FluentValidation;

namespace Chat.CQRS.Conversations.FindChatsByMembers;

public sealed class FindChatsByMembersQueryValidator : AbstractValidator<FindChatsByMembersQuery>
{
    private const int MaxMemberIds = 50;

    public FindChatsByMembersQueryValidator()
    {
        RuleFor(x => x.MemberUserIds)
            .NotNull().WithMessage("MemberUserIds is required.")
            .Must(ids => ids.Count >= 1).WithMessage("At least one member must be specified.")
            .Must(ids => ids.Count <= MaxMemberIds)
                .WithMessage($"MemberUserIds cannot contain more than {MaxMemberIds} entries.")
            .Must(ids => ids.All(id => id != Guid.Empty))
                .WithMessage("MemberUserIds cannot contain empty GUIDs.")
            .UniqueIds();
    }
}
