using Business.Interfaces.Model;
using Entities.Enums;
using Entities.Models;

namespace Services.Interfaces;

public interface IJwtService
{
    TokenDto GenerateToken(User user, Guid? activeTenantId = null, TenantRole? activeTenantRole = null);
}

