using Entities.Models.Users;

namespace Business.Interfaces.Services
{
    public interface IWelcomeEmailService
    {
        Task SendWelcomeEmailAsync(User user, CancellationToken cancellationToken = default);
    }
}
