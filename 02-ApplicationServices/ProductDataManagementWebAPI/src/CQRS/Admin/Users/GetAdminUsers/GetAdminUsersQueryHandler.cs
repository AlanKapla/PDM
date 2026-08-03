using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Admin;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Admin.Users.GetAdminUsers
{
    public sealed class GetAdminUsersQueryHandler
        : IRequestHandler<GetAdminUsersQuery, IReadOnlyList<AdminUserWeb>>
    {
        private readonly IReadRepository<User> userReadRepo;
        private readonly ICurrentUser currentUser;

        public GetAdminUsersQueryHandler(
            IReadRepository<User> userReadRepo,
            ICurrentUser currentUser)
        {
            this.userReadRepo = userReadRepo;
            this.currentUser = currentUser;
        }

        public async Task<IReadOnlyList<AdminUserWeb>> Handle(
            GetAdminUsersQuery request,
            CancellationToken cancellationToken)
        {
            EnsureSuperAdmin();

            IEnumerable<User> users = await userReadRepo.GetBySearch(_ => true);

            return users
                .OrderByDescending(u => u.CreatedAt)
                .Select(MapToWeb)
                .ToList();
        }

        private void EnsureSuperAdmin()
        {
            if (!currentUser.IsSuperAdmin)
            {
                throw new ForbiddenApiException("Only SuperAdmin can list all users.");
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
