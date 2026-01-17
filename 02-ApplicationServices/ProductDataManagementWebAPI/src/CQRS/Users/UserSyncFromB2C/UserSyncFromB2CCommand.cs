using CQRS;

namespace CQRS.Users.UserSyncFromB2C
{
    public record UserSyncFromB2CCommand : IRequestCommand<Guid>
    {
    }
}
