using CQRS.Extensions;
using FluentValidation;

namespace CQRS.Notifications.GetAllNotifications
{
    public sealed class GetAllNotificationsQueryValidator : AbstractValidator<GetAllNotificationsQuery>
    {
        public GetAllNotificationsQueryValidator()
        {
            RuleFor(x => x.Take).PageSize();
            RuleFor(x => x.Skip).NonNegativeOffset();
        }
    }
}
