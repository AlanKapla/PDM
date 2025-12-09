using Business.Interfaces.DTO;
using Business.Interfaces.Exceptions;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Entities.Models;
using MediatR;
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
    private readonly IRepository<TenantMember> tenantMemberRepo;
    private readonly IRepository<TenantPreferencesProfile> tenantPrefsRepo;
    private readonly INotificationSender notificationSender;
    private readonly ICurrentUser currentUser;

    public ToggleTenantStatusCommandHandler(
        IReadRepository<Tenant> tenantRepo,
        IRepository<TenantMember> tenantMemberRepo,
        IRepository<TenantPreferencesProfile> tenantPrefsRepo,
        INotificationSender notificationSender,
        ICurrentUser currentUser)
    {
        this.tenantRepo = tenantRepo;
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

        // Jeśli tenant został dezaktywowany, wyczyść ActiveTenantId z profili użytkowników
        if (!request.IsActive)
        {
            IEnumerable<TenantPreferencesProfile> profilesWithActiveTenant = await tenantPrefsRepo.GetBySearch(
                p => p.ActiveTenantId == request.TenantId);

            foreach (TenantPreferencesProfile profile in profilesWithActiveTenant)
            {
                profile.ActiveTenantId = null;
                await tenantPrefsRepo.Update(profile);
            }
        }

        // Pobierz wszystkich aktywnych członków tenanta do wysłania notyfikacji
        IEnumerable<TenantMember> tenantMembers = await tenantMemberRepo.GetBySearch(
            tm => tm.TenantId == request.TenantId && tm.IsActive);

        // Określ typ i treść notyfikacji w zależności od akcji
        string actionText = request.IsActive ? "aktywowana" : "zdezaktywowana";
        NotificationType notificationType = request.IsActive ? NotificationType.Info : NotificationType.Warning;

        // Wyślij notyfikację do wszystkich członków tenanta oprócz użytkownika wykonującego akcję
        foreach (TenantMember member in tenantMembers.Where(tm => tm.UserId != currentUser.Id))
        {
            NotificationDto notification = new()
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                ProjectId = null,
                UserId = member.UserId,
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
