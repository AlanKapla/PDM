using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Users.SendWelcomeEmailToUser
{
    public sealed class SendWelcomeEmailToUserCommandHandler
        : IRequestHandler<SendWelcomeEmailToUserCommand, AdminUserWeb>
    {
        private readonly IReadRepository<User> userReadRepo;
        private readonly IRepository<User> userRepo;
        private readonly ICurrentUser currentUser;
        private readonly IWelcomeEmailService welcomeEmailService;

        public SendWelcomeEmailToUserCommandHandler(
            IReadRepository<User> userReadRepo,
            IRepository<User> userRepo,
            ICurrentUser currentUser,
            IWelcomeEmailService welcomeEmailService)
        {
            this.userReadRepo = userReadRepo;
            this.userRepo = userRepo;
            this.currentUser = currentUser;
            this.welcomeEmailService = welcomeEmailService;
        }

        public async Task<AdminUserWeb> Handle(
            SendWelcomeEmailToUserCommand request,
            CancellationToken cancellationToken)
        {
            EnsureSuperAdmin();

            User user = await GetAndValidateUserAsync(request.UserId, cancellationToken);
            EnsureUserHasEmail(user);

            await welcomeEmailService.SendWelcomeEmailAsync(user, cancellationToken);
            user.WelcomeEmailSentAt = DateTime.UtcNow;
            await userRepo.Update(user);

            return MapToWeb(user);
        }

        private void EnsureSuperAdmin()
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can send welcome emails.");
            }
        }

        private async Task<User> GetAndValidateUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            User? user = await userReadRepo.GetFirstBySearch(
                u => u.Id == userId,
                cancellationToken);

            if (user is null)
            {
                throw new NotFoundApiException(nameof(User), userId.ToString());
            }

            return user;
        }

        private static void EnsureUserHasEmail(User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new ConflictApiException(
                    nameof(User),
                    user.Id.ToString(),
                    "User does not have an email address.");
            }
        }

        private static AdminUserWeb MapToWeb(User user)
        {
            return new AdminUserWeb(
                Id: user.Id,
                Email: user.Email,
                FirstName: user.FirstName,
                LastName: user.LastName,
                IsActive: user.IsActive,
                SystemRole: user.SystemRole.ToString(),
                CreatedAt: user.CreatedAt,
                WelcomeEmailSentAt: user.WelcomeEmailSentAt,
                PhoneNumber: user.PhoneNumber,
                CompanyName: user.CompanyName,
                TaxId: user.TaxId,
                Street: user.Street,
                City: user.City,
                PostalCode: user.PostalCode,
                Country: user.Country);
        }
    }
}
