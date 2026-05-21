using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Notifications.GetUnreadNotifications
{
    public sealed class GetUnreadNotificationsQueryValidator : AbstractValidator<GetUnreadNotificationsQuery>
    {
        public GetUnreadNotificationsQueryValidator()
        {
            RuleFor(x => x.Take).PageSize();
            RuleFor(x => x.Skip).NonNegativeOffset();
        }
    }
}