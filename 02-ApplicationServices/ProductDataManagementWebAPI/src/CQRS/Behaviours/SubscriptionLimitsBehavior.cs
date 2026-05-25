using Business.Interfaces.Exceptions;
using Business.Interfaces.Services;
using Entities.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CQRS.Behaviours
{
    /// <summary>
    /// Sprawdza limity subskrypcji przed wykonaniem handlera.
    /// Dotyczy tylko komend implementujących IRequiresProjectSlot lub IRequiresUserSlot.
    /// </summary>
    public sealed class SubscriptionLimitsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ISubscriptionLimitsResolver limitsResolver;
        private readonly AppDbContext appDbContext;
        private readonly ILogger<SubscriptionLimitsBehavior<TRequest, TResponse>> logger;

        public SubscriptionLimitsBehavior(
            ISubscriptionLimitsResolver limitsResolver,
            AppDbContext appDbContext,
            ILogger<SubscriptionLimitsBehavior<TRequest, TResponse>> logger)
        {
            this.limitsResolver = limitsResolver;
            this.appDbContext = appDbContext;
            this.logger = logger;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (request is not IRequiresProjectSlot projectSlotRequest && request is not IRequiresUserSlot userSlotRequest)
            {
                return await next(cancellationToken);
            }

            Guid tenantId = request is IRequiresProjectSlot ps
                ? ps.TenantId
                : ((IRequiresUserSlot)request).TenantId;

            Entities.Models.Subscriptions.SubscriptionLimits limits;

            try
            {
                limits = await limitsResolver.ResolveAsync(tenantId, cancellationToken);
            }
            catch (NotFoundApiException)
            {
                logger.LogWarning(
                    "Nie znaleziono subskrypcji dla tenanta {TenantId}. Żądanie {RequestType} zostanie zablokowane.",
                    tenantId,
                    typeof(TRequest).Name);

                throw new ForbiddenApiException("Brak aktywnej subskrypcji dla tego konta. Skontaktuj się z administratorem.");
            }

            // ── Sprawdzenie limitu projektów ─────────────────────────────────────
            if (request is IRequiresProjectSlot)
            {
                int projectCount = await appDbContext.Projects
                    .CountAsync(p => p.TenantId == tenantId && p.IsActive, cancellationToken);

                if (!limits.CanAddProject(projectCount))
                {
                    logger.LogInformation(
                        "Limit projektów osiągnięty dla tenanta {TenantId}. Limit: {MaxProjects}, aktualna liczba: {ProjectCount}.",
                        tenantId,
                        limits.MaxProjects,
                        projectCount);

                    throw new ForbiddenApiException(
                        $"Osiągnięto limit projektów dla Twojego planu ({limits.MaxProjects}). Zmień plan aby dodać więcej projektów.");
                }
            }

            // ── Sprawdzenie limitu użytkowników ──────────────────────────────────
            if (request is IRequiresUserSlot)
            {
                int userCount = await appDbContext.TenantMembers
                    .CountAsync(m => m.TenantId == tenantId && m.IsActive, cancellationToken);

                if (!limits.CanAddUser(userCount))
                {
                    logger.LogInformation(
                        "Limit użytkowników osiągnięty dla tenanta {TenantId}. Limit: {MaxUsers}, aktualna liczba: {UserCount}.",
                        tenantId,
                        limits.MaxUsers,
                        userCount);

                    throw new ForbiddenApiException(
                        $"Osiągnięto limit użytkowników dla Twojego planu ({limits.MaxUsers}). Zmień plan aby dodać więcej użytkowników.");
                }
            }

            return await next(cancellationToken);
        }
    }
}
