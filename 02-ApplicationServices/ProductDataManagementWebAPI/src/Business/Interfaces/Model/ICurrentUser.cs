using Business.Implementation.Model;
using Entities.Enums;

namespace Business.Interfaces.Model
{
    public interface ICurrentUser
    {
        Guid Id { get; }
        string FirstName { get; }
        string LastName { get; }
        string Email { get; }
        Guid? ActiveTenantId { get; }
        TenantRole? ActiveTenantRole { get; }
        SystemRole SystemRole { get; }

        List<TenantMembership>? Tenants { get; }
        List<ProjectMembership>? Projects { get; }
        List<GroupMembership>? Groups { get; }

        bool IsAuthenticated { get; }
    }
}
