using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default);
    }
}
