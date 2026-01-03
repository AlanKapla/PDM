using FluentValidation;

namespace CQRS.Roles.GetAvailableRoles
{
    public class GetAvailableRolesQueryValidator : AbstractValidator<GetAvailableRolesQuery>
    {
        public GetAvailableRolesQueryValidator()
        {
            RuleFor(x => x.Scope)
                .IsInEnum()
                .WithMessage("Scope must be a valid RoleScope value (Tenant or Project)");
        }
    }
}
