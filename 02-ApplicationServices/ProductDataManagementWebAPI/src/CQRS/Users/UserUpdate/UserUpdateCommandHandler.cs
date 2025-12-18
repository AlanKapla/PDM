using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.WebModels.Users;
using Entities.Models;
using MediatR;
using Repositiories.Repository.Interfaces;

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

            await userRepo.Update(user);

            return new UserUpdateWeb(user.Id, user.FirstName, user.LastName);
        }
    }
}
