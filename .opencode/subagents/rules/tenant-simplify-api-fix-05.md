# tenant-simplify-api-fix-05 — CQRS Handlers: dostosowanie do IsAdmin

## Cel
Zaktualizuj wszystkie handlery CQRS dotyczące tenanta, aby używały `IsAdmin` zamiast `RoleId`/`RoleCode`.
Dodaj walidację duplikatów w `InviteTenantMember`.

## Skill
Przeczytaj `.github/skills/api/skill-api-cqrs.md` przed implementacją.

---

## 1. `CreateTenant` — `src/CQRS/Tenants/CreateTenant/CreateTenantCommandHandler.cs`

Usuń:
- `IReadRepository<Role> roleRepo` — z konstruktora i pola
- pobieranie `adminRole` przez `roleRepo.GetFirstBySearch`
- throw `NotFoundApiException` dla brakującej roli

Zamień tworzenie `TenantMember`:
```csharp
// STARE:
TenantMember ownerMember = new TenantMember
{
    TenantId = tenant.Id,
    UserId = currentUser.Id,
    RoleId = adminRole.Id
};

// NOWE:
TenantMember ownerMember = new TenantMember
{
    TenantId = tenant.Id,
    UserId = currentUser.Id,
    IsAdmin = true
};
```

Zaktualizuj mapowanie `TenantDetailsWeb` — usuń `RoleCode = RoleCodes.TenantAdmin`, dodaj `IsAdmin = true`.

Usuń using `Entities.Models.Roles` jeśli nie jest używany.

---

## 2. `UpdateTenantMemberRole` → `UpdateTenantMemberAdmin`

### `src/CQRS/Tenants/UpdateTenantMemberRole/UpdateTenantMemberRoleCommand.cs`

Zamień całą zawartość:
```csharp
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using MediatR;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed record UpdateTenantMemberRoleCommand : IRequestCommand<Unit>, IAuthorizableRequest
    {
        public Guid TenantId { get; init; }
        public Guid UserId { get; init; }
        public required bool IsAdmin { get; init; }

        public string PermissionCode => PermissionCodes.TenantMembersManage;

        public ResourceRef GetResource() => new(TenantId: TenantId);
    }
}
```

### `src/CQRS/Tenants/UpdateTenantMemberRole/UpdateTenantMemberRoleCommandHandler.cs`

Zastąp całą implementację:
```csharp
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Helpers;
using Entities.Models;
using Entities.Models.Notifications;
using Entities.Models.Tenants;
using Entities.Models.Users;
using MediatR;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.UpdateTenantMemberRole
{
    public sealed class UpdateTenantMemberRoleCommandHandler : IRequestHandler<UpdateTenantMemberRoleCommand, Unit>
    {
        private readonly IReadRepository<Tenant> tenantRepo;
        private readonly IReadRepository<User> userRepo;
        private readonly IRepository<TenantMember> tenantMemberRepo;
        private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
        private readonly IReadRepository<Notification> notificationRepo;
        private readonly IPermissionsVersionService permissionsVersionService;
        private readonly INotificationSender notificationSender;
        private readonly ICurrentUser currentUser;

        public UpdateTenantMemberRoleCommandHandler(
            IReadRepository<Tenant> tenantRepo,
            IReadRepository<User> userRepo,
            IRepository<TenantMember> tenantMemberRepo,
            IRepository<TenantPreferencesProfile> tenantPrefsRepo,
            IReadRepository<Notification> notificationRepo,
            IPermissionsVersionService permissionsVersionService,
            INotificationSender notificationSender,
            ICurrentUser currentUser)
        {
            this.tenantRepo = tenantRepo;
            this.userRepo = userRepo;
            this.tenantMemberRepo = tenantMemberRepo;
            this.tenantPrefsRepo = tenantPrefsRepo;
            this.notificationRepo = notificationRepo;
            this.permissionsVersionService = permissionsVersionService;
            this.notificationSender = notificationSender;
            this.currentUser = currentUser;
        }

        public async Task<Unit> Handle(UpdateTenantMemberRoleCommand request, CancellationToken cancellationToken)
        {
            Tenant tenant = await tenantRepo.GetFirstBySearch(t => t.Id == request.TenantId)
                ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

            TenantMember tenantMember = await tenantMemberRepo.GetFirstBySearch(
                m => m.TenantId == request.TenantId
                    && m.UserId == request.UserId
                    && m.IsActive,
                cancellationToken)
                ?? throw new NotFoundApiException(nameof(TenantMember), $"Tenant: {request.TenantId}, User: {request.UserId}");

            bool isDemoting = tenantMember.IsAdmin && !request.IsAdmin;

            if (isDemoting)
            {
                int adminCount = await tenantMemberRepo.CountAsync(
                    m => m.TenantId == request.TenantId
                         && m.IsActive
                         && m.IsAdmin,
                    cancellationToken);

                if (adminCount <= 1)
                {
                    throw new ConflictApiException(
                        nameof(TenantMember),
                        request.UserId.ToString(),
                        "Nie można odebrać uprawnień administratora ostatniemu administratorowi tenanta.");
                }
            }

            tenantMember.IsAdmin = request.IsAdmin;

            await tenantMemberRepo.Update(tenantMember);
            await tenantMemberRepo.SaveChangesAsync(cancellationToken);

            await permissionsVersionService.BumpVersionAsync(request.UserId, cancellationToken);

            User? targetUser = await userRepo.GetFirstBySearch(u => u.Id == request.UserId, cancellationToken);

            string message = request.IsAdmin
                ? $"Otrzymałeś uprawnienia administratora w organizacji: {tenant.Name}"
                : $"Zmieniono Twoje uprawnienia w organizacji: {tenant.Name}";

            NotificationDto notification = NotificationBuilder.Build(
                userId: request.UserId,
                azureAdB2CObjectId: targetUser?.AzureAdB2CObjectId,
                tenantId: request.TenantId,
                type: NotificationType.Info,
                title: "Zmiana uprawnień",
                message: message
            );

            await notificationSender.SendAsync(notification, notificationRepo, cancellationToken);

            return Unit.Value;
        }
    }
}
```

**Uwaga:** Sprawdź jak wygląda `NotificationBuilder.Build` w projekcie — jeśli nie istnieje taki helper, utwórz `NotificationDto` bezpośrednio analogicznie jak w `RemoveTenantMemberCommandHandler`.

---

## 3. `RemoveTenantMember` — `src/CQRS/Tenants/RemoveTenantMember/RemoveTenantMemberCommandHandler.cs`

Dodaj walidację ostatniego admina przed usunięciem:

```csharp
// Po pobraniu tenantMember, przed ustawieniem IsActive = false:
if (tenantMember.IsAdmin)
{
    int adminCount = await tenantMemberRepo.CountAsync(
        m => m.TenantId == request.TenantId
             && m.IsActive
             && m.IsAdmin,
        cancellationToken);

    if (adminCount <= 1)
    {
        throw new ConflictApiException(
            nameof(TenantMember),
            request.UserId.ToString(),
            "Nie można usunąć ostatniego administratora tenanta.");
    }
}
```

Usuń `q => q.Include(m => m.MemberRole)` z zapytania — `MemberRole` nie istnieje.

---

## 4. `GetTenantDetails` — `src/CQRS/Tenants/GetTenantDetails/GetTenantDetailsQueryHandler.cs`

Usuń:
- `IReadRepository<Role> roleRepo` z konstruktora i pola
- całe ładowanie ról (`memberRoleIds`, `roles`, `roleDict`)
- `using Entities.Models.Roles`

Zamień mapowanie `TenantMemberWeb`:
```csharp
// STARE:
string roleCode = RoleCodes.TenantMember;
if (m.RoleId.HasValue && roleDict.TryGetValue(m.RoleId.Value, out Role? role))
{
    roleCode = role.Code;
}

return new TenantMemberWeb(
    UserId: m.UserId,
    Email: user?.Email ?? string.Empty,
    FirstName: user?.FirstName ?? string.Empty,
    LastName: user?.LastName ?? string.Empty,
    RoleCode: roleCode,
    IsActive: m.IsActive,
    JoinedAt: m.CreatedAt
);

// NOWE:
return new TenantMemberWeb(
    UserId: m.UserId,
    Email: user?.Email ?? string.Empty,
    FirstName: user?.FirstName ?? string.Empty,
    LastName: user?.LastName ?? string.Empty,
    IsAdmin: m.IsAdmin,
    IsActive: m.IsActive,
    JoinedAt: m.CreatedAt
);
```

Zamień budowanie `TenantDetailsWeb` na końcu:
```csharp
// STARE: RoleCode = currentUserMembership?.MemberRole?.Code ?? RoleCodes.TenantMember,
// NOWE:
IsAdmin = currentUserMembership?.IsAdmin ?? false,
```

Usuń `using Entities.Models.Roles` i `using Business.Interfaces.Constants` (jeśli `RoleCodes` był stamtąd).

---

## 5. `GetAdminTenants` — `src/CQRS/Tenants/GetAdminTenants/GetAdminTenantsQueryHandler.cs`

Zamień predykat filtrowania:
```csharp
// STARE:
IEnumerable<TenantMember> adminMemberships = await tenantMemberRepo.GetBySearch(
    m => m.UserId == currentUser.Id
        && m.IsActive
        && m.MemberRole!.Code == RoleCodes.TenantAdmin,
    include => include.Include(m => m.Tenant).Include(m => m.MemberRole)
);

// NOWE:
IEnumerable<TenantMember> adminMemberships = await tenantMemberRepo.GetBySearch(
    m => m.UserId == currentUser.Id
        && m.IsActive
        && m.IsAdmin,
    include => include.Include(m => m.Tenant)
);
```

Zamień mapowanie `TenantBasicWeb`:
```csharp
// STARE: RoleCode = RoleCodes.TenantAdmin
// NOWE: IsAdmin = true
```

Usuń `using Business.Interfaces.Constants` (jeśli używane tylko dla `RoleCodes`), `using Entities.Enums`, `using Microsoft.EntityFrameworkCore` jeśli nie potrzebne.

---

## 6. `GetUserTenants` — `src/CQRS/Tenants/GetUserTenants/GetUserTenantsQueryHandler.cs`

W metodzie `Handle`:

Zamień `.Include(m => m.MemberRole)` → usuń include (MemberRole nie istnieje).

Zamień mapowanie `RoleCode` na `IsAdmin`:
```csharp
// STARE:
string roleCode = membershipDict.TryGetValue(t.Id, out TenantMember? membership)
    ? (membership.MemberRole?.Code ?? RoleCodes.TenantMember)
    : RoleCodes.SystemSuperAdmin;

return new UserTenantWeb { ..., RoleCode = roleCode, ... };

// NOWE:
bool isAdmin = membershipDict.TryGetValue(t.Id, out TenantMember? membership)
    ? membership.IsAdmin
    : false; // SuperAdmin bez TenantMember nie jest automatycznie IsAdmin

return new UserTenantWeb { ..., IsAdmin = isAdmin, ... };
```

Analogicznie dla zwykłego flow (non-SuperAdmin):
```csharp
// STARE:
IEnumerable<TenantMember> memberships = await tenantMemberRepo.GetBySearch(
    m => m.UserId == currentUser.Id && m.IsActive,
    q => q.Include(m => m.MemberRole).Include(m => m.Tenant)
);
// NOWE: usuń Include(m => m.MemberRole)

// STARE: RoleCode = m.MemberRole?.Code ?? RoleCodes.TenantMember,
// NOWE:   IsAdmin = m.IsAdmin,
```

---

## 7. `InviteTenantMember` — `src/CQRS/Tenants/InviteTenantMember/InviteTenantMemberCommandHandler.cs`

Dodaj walidację duplikatów po normalizacji emaila:

```csharp
// Po: string normalizedEmail = request.Email.Trim().ToLowerInvariant();
// Dodaj:

// Sprawdź czy istnieje już aktywne zaproszenie dla tego emaila
bool duplicateInvitation = await invitationRepo.AnyAsync(
    i => i.TenantId == request.TenantId
         && i.Email == normalizedEmail
         && i.IsActive
         && i.Status == InvitationStatus.Pending
         && i.ExpiresAt > DateTime.UtcNow,
    cancellationToken);

if (duplicateInvitation)
{
    throw new ConflictApiException(
        nameof(TenantInvitation),
        normalizedEmail,
        "Aktywne zaproszenie dla tego adresu email już istnieje.");
}

// Sprawdź czy użytkownik nie jest już aktywnym członkiem tenanta
bool alreadyMember = await tenantMemberRepo.AnyAsync(
    m => m.TenantId == request.TenantId
         && m.User.Email == normalizedEmail
         && m.IsActive,
    cancellationToken);

if (alreadyMember)
{
    throw new ConflictApiException(
        nameof(TenantMember),
        normalizedEmail,
        "Użytkownik jest już aktywnym członkiem tej organizacji.");
}
```

**Uwaga:** Potrzebujesz `IReadRepository<TenantInvitation>` lub `IRepository<TenantInvitation>` — sprawdź co już jest w konstruktorze. Jeśli handler korzysta z `IRepository<TenantInvitation>` to ma już metodę `AnyAsync`. Sprawdź sygnaturę `AnyAsync` w interfejsie repozytorium.

Jeśli `AnyAsync` nie istnieje w interfejsie repozytorium, użyj:
```csharp
TenantInvitation? existing = await invitationRepo.GetFirstBySearch(
    i => i.TenantId == request.TenantId
         && i.Email == normalizedEmail
         && i.IsActive
         && i.Status == InvitationStatus.Pending
         && i.ExpiresAt > DateTime.UtcNow,
    cancellationToken);

if (existing is not null)
{
    throw new ConflictApiException(...);
}
```

---

## 8. `AcceptTenantInvitation` — znajdź handler i zaktualizuj tworzenie TenantMember

Znajdź `src/CQRS/Tenants/AcceptTenantInvitation/AcceptTenantInvitationCommandHandler.cs`.
Znajdź miejsce tworzenia nowego `TenantMember` i zastąp:
```csharp
// STARE (coś w stylu):
TenantMember newMember = new TenantMember
{
    TenantId = invitation.TenantId,
    UserId = currentUser.Id,
    RoleId = memberRole.Id // lub podobne
};

// NOWE:
TenantMember newMember = new TenantMember
{
    TenantId = invitation.TenantId,
    UserId = currentUser.Id,
    IsAdmin = false
};
```

Usuń pobieranie `memberRole` przez `roleRepo.GetFirstBySearch` jeśli było potrzebne tylko do `TenantMember.RoleId`.

---

## 9. `UserDetailsQueryHandler` — `src/CQRS/Users/UserDetails/UserDetailsQueryHandler.cs`

Zastąp budowanie `activeTenantPermissions`:

**Stary kod:**
```csharp
var activeTenantPermissions = new HashSet<string>();

if (currentUser.IsAuthenticated && currentUser.ActiveTenantId.HasValue)
{
    var tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
    if (tenantSnapshot != null)
    {
        activeTenantPermissions = tenantSnapshot.TenantPermissionCodes;
    }
}

return new UserDetailsWeb(
    currentUser.Id, 
    currentUser.FirstName, 
    currentUser.LastName, 
    currentUser.Email, 
    currentUser.ActiveTenantId,
    activeTenantPermissions,
    ...
);
```

**Nowy kod:**
```csharp
bool isActiveTenantAdmin = false;

if (currentUser.IsAuthenticated && currentUser.ActiveTenantId.HasValue)
{
    TenantCtxSnapshot? tenantSnapshot = await currentUser.GetActiveTenantSnapshotAsync(cancellationToken);
    isActiveTenantAdmin = tenantSnapshot?.IsAdmin ?? false;
}

return new UserDetailsWeb(
    currentUser.Id, 
    currentUser.FirstName, 
    currentUser.LastName, 
    currentUser.Email, 
    currentUser.ActiveTenantId,
    isActiveTenantAdmin,
    user?.PhoneNumber,
    user?.CompanyName,
    user?.TaxId,
    user?.Street,
    user?.City,
    user?.PostalCode,
    user?.Country);
```

Dodaj `using Business.Interfaces.Model;` jeśli brakuje (dla `TenantCtxSnapshot`).
Usuń `using Business.Interfaces.Constants;` jeśli nie jest używany przez inne fragmenty handlera.

---

## Build check
```
dotnet build src/CQRS/CQRS.csproj
```
