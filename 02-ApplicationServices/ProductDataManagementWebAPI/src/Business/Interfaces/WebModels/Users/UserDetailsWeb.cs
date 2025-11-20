namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserDetailsWeb(string FirstName, string LastName, string Email, Guid? LastTenantId);
}
