namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserUpdateWeb(
        Guid Id,
        string FirstName,
        string LastName,
        string? PhoneNumber,
        string? CompanyName,
        string? TaxId,
        string? Street,
        string? City,
        string? PostalCode,
        string? Country)
    {
    }
}
