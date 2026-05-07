using Business.Interfaces.Model;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Roles;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using FluentValidation;
using Repositories.Repository.Interfaces;

namespace CQRS.Notifications.MarkNotificationAsRead
{
    public class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
    {
        private readonly IRepository<Notification> notificationRepo;
        private readonly ICurrentUser currentUser;

        public MarkNotificationAsReadCommandValidator(
            IRepository<Notification> notificationRepo,
            ICurrentUser currentUser)
        {
            this.notificationRepo = notificationRepo;
            this.currentUser = currentUser;

            RuleFor(x => x.NotificationId)
                .NotEmpty()
                .WithMessage("NotificationId is required");

            // Walidacja: notyfikacja musi istnieć i należeć do zalogowanego użytkownika
            RuleFor(x => x.NotificationId)
                .MustAsync(NotificationMustExistAndBelongToUser)
                .WithMessage("Notification not found or does not belong to the current user");
        }

        private async Task<bool> NotificationMustExistAndBelongToUser(Guid notificationId, CancellationToken cancellationToken)
        {
            var notification = await notificationRepo.GetFirstBySearch(
                n => n.Id == notificationId && n.UserId == currentUser.Id);

            return notification != null;
        }
    }
}
