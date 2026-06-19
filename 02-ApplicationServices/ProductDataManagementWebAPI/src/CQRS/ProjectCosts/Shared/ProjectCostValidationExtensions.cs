using System.Linq.Expressions;
using Business.Interfaces.Helpers;
using Entities.Models.Projects;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Repositories.Repository.Interfaces;

namespace CQRS.ProjectCosts.Shared
{
    /// <summary>
    /// Reguły walidacji wspólne dla wszystkich komend domeny ProjectCosts.
    /// </summary>
    internal static class ProjectCostValidationExtensions
    {
        /// <summary>
        /// Sprawdza, że wszystkie identyfikatory użytkowników są członkami projektu (TenantId, ProjectId).
        /// Pusta lista lub null są traktowane jako poprawne.
        /// </summary>
        public static IRuleBuilderOptions<T, List<Guid>> AllAreProjectMembers<T>(
            this IRuleBuilder<T, List<Guid>> rule,
            IRepository<ProjectMember> projectMemberRepository,
            Func<T, Guid> tenantIdSelector,
            Func<T, Guid> projectIdSelector)
            => rule
                .MustAsync(async (instance, userIds, _) =>
                {
                    if (userIds is null || userIds.Count == 0)
                    {
                        return true;
                    }

                    Guid tenantId = tenantIdSelector(instance);
                    Guid projectId = projectIdSelector(instance);

                    IEnumerable<ProjectMember> members = await projectMemberRepository.GetBySearch(
                        pm => pm.ProjectId == projectId
                              && pm.TenantId == tenantId
                              && pm.IsActive
                              && userIds.Contains(pm.UserId));

                    HashSet<Guid> memberUserIds = members.Select(m => m.UserId).ToHashSet();
                    return userIds.All(id => memberUserIds.Contains(id));
                })
                .WithMessage("All users must be members of the project");

        /// <summary>
        /// Reguły dla nazwy kosztu: wymagana, max 200 znaków.
        /// </summary>
        public static void ApplyCostNameRules<T>(
            this AbstractValidator<T> validator,
            Expression<Func<T, string>> selector)
        {
            validator.RuleFor(selector)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");
        }

        /// <summary>
        /// Reguły finansowe: Net lub Gross musi być podany; oba muszą być &gt;= 0 jeśli podane.
        /// </summary>
        public static void ApplyCostFinancialRules<T>(
            this AbstractValidator<T> validator,
            Expression<Func<T, decimal?>> netSelector,
            Expression<Func<T, decimal?>> grossSelector)
        {
            Func<T, decimal?> netGet = netSelector.Compile();
            Func<T, decimal?> grossGet = grossSelector.Compile();

            validator.RuleFor(x => x)
                .Must(x => netGet(x).HasValue || grossGet(x).HasValue)
                .WithMessage("Must provide Net or Gross")
                .OverridePropertyName("Amount");

            validator.RuleFor(netSelector)
                .GreaterThanOrEqualTo(0).WithMessage("Net must be greater than or equal to 0")
                .When(x => netGet(x).HasValue);

            validator.RuleFor(grossSelector)
                .GreaterThanOrEqualTo(0).WithMessage("Gross must be greater than or equal to 0")
                .When(x => grossGet(x).HasValue);
        }

        /// <summary>
        /// Reguły dla daty kosztu: opcjonalna, nie może być w przyszłości (z tolerancją +1 dnia).
        /// </summary>
        public static void ApplyCostDateRules<T>(
            this AbstractValidator<T> validator,
            Expression<Func<T, DateTime?>> selector)
        {
            Func<T, DateTime?> dateGet = selector.Compile();
            validator.RuleFor(selector)
                .LessThanOrEqualTo(DateTime.UtcNow.Date.AddDays(1))
                .WithMessage("Date cannot be in the future")
                .When(x => dateGet(x).HasValue);
        }

        /// <summary>
        /// Reguły dla dokumentu (IFormFile): typ (JPEG/JPG/PNG/PDF) i rozmiar (max 10MB).
        /// </summary>
        public static void ApplyDocumentRules<T>(
            this AbstractValidator<T> validator,
            Expression<Func<T, IFormFile?>> selector,
            string propertyName)
        {
            validator.RuleFor(selector)
                .Must(DocumentValidationHelper.IsValidDocumentType)
                .WithMessage($"{propertyName} must be JPEG, JPG, PNG or PDF");

            validator.RuleFor(selector)
                .Must(DocumentValidationHelper.IsValidDocumentSize)
                .WithMessage($"{propertyName} size cannot exceed 10MB");
        }
    }
}
