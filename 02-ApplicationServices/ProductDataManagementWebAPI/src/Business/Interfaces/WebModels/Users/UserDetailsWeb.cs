namespace Business.Interfaces.WebModels.Users
{
    public class UserDetailsWeb
    {
        public string Email { get; set; } = string.Empty;
        public Guid? LastTenantId { get; set; }
    }
}
