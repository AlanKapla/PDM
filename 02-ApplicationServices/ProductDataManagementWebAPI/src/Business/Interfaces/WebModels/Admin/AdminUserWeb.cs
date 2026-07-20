namespace Business.Interfaces.WebModels.Admin;

public sealed record AdminUserWeb(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    string SystemRole,
    DateTime CreatedAt,
    DateTime? WelcomeEmailSentAt,
    string? PhoneNumber,
    string? CompanyName,
    string? TaxId,
    string? Street,
    string? City,
    string? PostalCode,
    string? Country);
