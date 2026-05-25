using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Users.GetAdminUsers;

public sealed record GetAdminUsersQuery : IRequestQuery<IEnumerable<AdminUserListItemWeb>>, ISuperAdminRequest;
