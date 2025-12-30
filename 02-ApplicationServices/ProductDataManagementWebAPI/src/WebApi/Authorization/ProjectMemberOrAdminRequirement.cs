using Microsoft.AspNetCore.Authorization;

namespace WebApi.Authorization
{
    public class ProjectMemberOrAdminRequirement : IAuthorizationRequirement
    {
    }
}
