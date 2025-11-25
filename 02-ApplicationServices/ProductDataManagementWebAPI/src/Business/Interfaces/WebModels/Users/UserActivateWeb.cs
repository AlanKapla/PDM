namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserActivateWeb(Guid Id, string Email, bool Activated)
    {
    }
}
