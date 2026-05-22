using Business.Interfaces.WebModels.Users;

namespace CQRS.Users.UserUpdate
{
    public sealed record UserUpdateCommand(
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? CompanyName,
        string? TaxId,
        string? Street,
        string? City,
        string? PostalCode,
        string? Country
    ) : IRequestCommand<UserUpdateWeb>
    {
    }
}
