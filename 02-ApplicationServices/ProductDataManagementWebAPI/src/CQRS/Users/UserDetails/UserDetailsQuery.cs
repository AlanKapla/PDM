using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserDetails
{
    public sealed record UserDetailsQuery : IRequestQuery<UserDetailsWeb>;
}