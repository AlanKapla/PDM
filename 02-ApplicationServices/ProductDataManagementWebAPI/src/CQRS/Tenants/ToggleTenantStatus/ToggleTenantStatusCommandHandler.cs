using Business.Interfaces.Constants;
using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Extensions;
using Entities.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Repositiories.Repository.Interfaces;
using Repositories.Repository.Interfaces;
using NotificationType = Business.Interfaces.DTO.NotificationType;

namespace CQRS.Tenants.ToggleTenantStatus;

/// <summary>
/// Handler zmieniający status aktywności tenanta
/// </summary>
public class ToggleTenantStatusCommandHandler : IRequestHandler<ToggleTenantStatusCommand, Unit>
{
    private readonly IReadRepository<Tenant> tenantRepo;
    private readonly IReadRepository<User> userRepo;
    private readonly IRepository<TenantMember> tenantMemberRepo;
    private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;

    public ToggleTenantStatusCommandHandler(
        IReadRepository<Tenant> tenantRepo,
        IReadRepository<User> userRepo,
        IRepository<TenantMember> tenantMemberRepo,
        IRepository<TenantPreferencesProfile> tenantPrefsRepo,
        INotificationSender notificationSender,
        ICurrentUser currentUser)
    {
        this.tenantRepo = tenantRepo;
        this.userRepo = userRepo;
        this.tenantMemberRepo = tenantMemberRepo;
        this.tenantPrefsRepo = tenantPrefsRepo;
        this.notificationSender = notificationSender;
        this.currentUser = currentUser;
    }

    public async Task<Unit> Handle(ToggleTenantStatusCommand request, CancellationToken cancellationToken)
    {
        // Pobierz tenanta - może być aktywny lub nieaktywny
        Tenant? tenant = await tenantRepo.GetById(request.TenantId)
            ?? throw new NotFoundApiException(nameof(Tenant), request.TenantId.ToString());

        // Zmień status tenanta
        tenant.IsActive = request.IsActive;
        await tenantRepo.Update(tenant);

        // Jeśli tenant został dezaktywowany, wyczyść ActiveTenantId TYLKO dla non-adminów
        if (!request.IsActive)
        {
            // Pobierz wszystkich członków tenanta z ich rolami
            IEnumerable<TenantMember> allMembers = await tenantMemberRepo.GetBySearch(
                tm => tm.TenantId == request.TenantId && tm.IsActive,
                q => q.Include(tm => tm.MemberRole)
            );

            // Pobierz profile użytkowników którzy mają ten tenant jako aktywny
            IEnumerable<TenantPreferencesProfile> profilesWithActiveTenant = await tenantPrefsRepo.GetBySearch(
                p => p.ActiveTenantId == request.TenantId);

            foreach (TenantPreferencesProfile profile in profilesWithActiveTenant)
            {
                // Sprawdź czy użytkownik jest adminem tego tenanta
                TenantMember? membership = allMembers.FirstOrDefault(m => m.UserId == profile.UserId);
                bool isAdmin = membership?.MemberRole?.Code == RoleCodes.TenantAdmin;

                // Wyczyść ActiveTenantId TYLKO dla non-adminów
                if (!isAdmin)
                {
                    profile.ActiveTenantId = null;
                    await tenantPrefsRepo.Update(profile);
                }
                // Admini zachowują swój ActiveTenantId
            }
        }

        // Pobierz wszystkich aktywnych członków tenanta do wysłania notyfikacji
        IEnumerable<TenantMember> tenantMembers = await tenantMemberRepo.GetBySearch(
            tm => tm.TenantId == request.TenantId && tm.IsActive);

        string actionText = request.IsActive ? "aktywowana" : "zdezaktywowana";
        NotificationType notificationType = request.IsActive ? NotificationType.Info : NotificationType.Warning;

        var memberUserIds = tenantMembers
            .Where(tm => tm.UserId != currentUser.Id)
            .Select(tm => tm.UserId)
            .ToList();

        var users = await userRepo.GetBySearch(u => memberUserIds.Contains(u.Id));
        var userDict = users.ToDictionary(u => u.Id);

        foreach (TenantMember member in tenantMembers.Where(tm => tm.UserId != currentUser.Id))
        {
            userDict.TryGetValue(member.UserId, out User? targetUser);

            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = member.UserId,
                AzureAdB2CObjectId = targetUser?.AzureAdB2CObjectId,
                Type = notificationType,
                Title = $"Organizacja {actionText}",
                Message = $"Organizacja \"{tenant.Name}\" została {actionText}",
                CreatedAt = DateTimeOffset.UtcNow,
                Readed = false,
                Metadata = new Dictionary<string, object?>
                {
                    { "tenantId", request.TenantId },
                    { "tenantName", tenant.Name },
                    { "modifiedByUserId", currentUser.Id },
                    { "isActive", request.IsActive }
                }
            };

            await notificationSender.EnqueueAsync(notification, cancellationToken);
        }

        return Unit.Value;
    }
}
