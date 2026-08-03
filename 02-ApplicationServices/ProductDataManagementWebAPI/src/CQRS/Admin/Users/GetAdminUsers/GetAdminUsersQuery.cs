using Business.Interfaces.WebModels.Admin;
using CQRS;

namespace CQRS.Admin.Users.GetAdminUsers
{
    public sealed record GetAdminUsersQuery : IRequestQuery<IReadOnlyList<AdminUserWeb>>;
}
