using Business.Interfaces.Constants;
using Business.Interfaces.Model;

namespace CQRS;

public interface IAuthorizableRequest
{
    string PermissionCode { get; }
    ResourceRef GetResource();
    
    /// <summary>
    /// Gets the resource scope for this request (optional, used for filtering resources)
    /// </summary>
    ResourceScope? GetResourceScope() => null;
}
