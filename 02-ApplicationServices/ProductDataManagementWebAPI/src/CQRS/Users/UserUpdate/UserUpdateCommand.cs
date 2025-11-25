using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserUpdate
{
    public sealed record UserUpdateCommand(string FirstName, string LastName) : IRequestCommand<UserUpdateWeb>
    {
    }
}
