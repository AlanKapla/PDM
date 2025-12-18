using Business.Interfaces.Exceptions;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;

namespace CQRS.Users.UserActivate
{
    public class UserActivateCommandHandler : IRequestHandler<UserActivateCommand, UserActivateWeb>
    {
        private readonly IRepository<UserActivation> activationRepo;
        private readonly IReadRepository<User> userRepo;

        public UserActivateCommandHandler(IRepository<UserActivation> activationRepo, IReadRepository<User> userRepo)
        {
            this.activationRepo = activationRepo;
            this.userRepo = userRepo;
        }

        public async Task<UserActivateWeb> Handle(UserActivateCommand request, CancellationToken cancellationToken)
        {
            UserActivation? activation = await activationRepo.GetFirstBySearch(a => a.Token == request.Token);
            if (activation == null || activation.ExpiresAt < DateTime.UtcNow || activation.IsActivated)
            {
                throw new ValidationApiException("Invalid or expired activation token.");
            }

            User? user = await userRepo.GetById(activation.UserId);
            if (user == null)
            {
                throw new NotFoundApiException(nameof(User), activation.UserId.ToString());
            }

            user.IsActive = true;
            activation.ActivatedAt = DateTime.UtcNow;

            await userRepo.Update(user);
            await activationRepo.Update(activation);

            return new UserActivateWeb(user.Id, user.Email, true);
        }
    }
}
