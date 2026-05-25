using Business.Interfaces.WebModels.Admin;

namespace CQRS.Admin.Users.GetAdminUserDetails;

public sealed record GetAdminUserDetailsQuery(Guid UserId)
    : IRequestQuery<AdminUserDetailsWeb>, ISuperAdminRequest;
