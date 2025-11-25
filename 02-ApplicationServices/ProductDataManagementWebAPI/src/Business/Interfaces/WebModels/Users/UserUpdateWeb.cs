namespace Business.Interfaces.WebModels.Users
{
    public sealed record UserUpdateWeb(Guid Id, string FirstName, string LastName)
    {
    }
}
