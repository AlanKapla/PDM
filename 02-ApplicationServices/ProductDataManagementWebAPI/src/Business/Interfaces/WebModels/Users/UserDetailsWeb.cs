using Entities.Enums;

namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserDetailsWeb(
        Guid Id, 
        string FirstName, 
        string LastName, 
        string Email, 
        Guid? ActiveTenantId,
        Dictionary<Guid, ProjectRole> ProjectRoles);
}
