using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using MediatR;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserUpdate
{
    public class UserUpdateCommandHandler : IRequestHandler<UserUpdateCommand, UserUpdateWeb>
    {
        private readonly IReadRepository<User> userRepo;
        private readonly ICurrentUser currentUser;

        public UserUpdateCommandHandler(IReadRepository<User> userRepo, ICurrentUser currentUser)
        {
            this.userRepo = userRepo;
            this.currentUser = currentUser;
        }

        public async Task<UserUpdateWeb> Handle(UserUpdateCommand request, CancellationToken cancellationToken)
        {
            User? user = await userRepo.GetById(currentUser.Id) ?? throw new NotFoundApiException(nameof(User), currentUser.Id.ToString());

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.CompanyName = request.CompanyName;
            user.TaxId = request.TaxId;
            user.Street = request.Street;
            user.City = request.City;
            user.PostalCode = request.PostalCode;
            user.Country = request.Country;

            await userRepo.Update(user);

            return new UserUpdateWeb(
                user.Id,
                user.FirstName,
                user.LastName,
                user.PhoneNumber,
                user.CompanyName,
                user.TaxId,
                user.Street,
                user.City,
                user.PostalCode,
                user.Country);
        }
    }
}
