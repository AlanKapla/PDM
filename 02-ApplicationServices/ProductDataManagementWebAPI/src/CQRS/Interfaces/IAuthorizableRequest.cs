using Business.Interfaces.Model;

namespace CQRS.Interfaces;

public interface IAuthorizableRequest
{
    string PermissionCode { get; }
    ResourceRef GetResource();
}
