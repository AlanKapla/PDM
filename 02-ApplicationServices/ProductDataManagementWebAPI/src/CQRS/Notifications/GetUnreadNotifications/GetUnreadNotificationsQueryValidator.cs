using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using FluentValidation;

namespace CQRS.Notifications.GetUnreadNotifications
{
    public class GetUnreadNotificationsQueryValidator : AbstractValidator<GetUnreadNotificationsQuery>
    {
        public GetUnreadNotificationsQueryValidator(ICurrentUser currentUser)
        {
            RuleFor(x => x)
                .Must(_ => currentUser.IsAuthenticated && currentUser.Id != Guid.Empty)
                .WithMessage("User must be authenticated")
                .WithErrorCode("401");
        }
    }
}