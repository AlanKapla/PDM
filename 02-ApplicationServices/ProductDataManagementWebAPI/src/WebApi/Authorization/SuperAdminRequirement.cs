using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization;

public sealed record SuperAdminRequirement : IAuthorizationRequirement;
