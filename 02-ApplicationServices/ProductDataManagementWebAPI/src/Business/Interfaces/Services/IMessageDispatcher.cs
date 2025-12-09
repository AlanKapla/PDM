using Business.Interfaces.DTO;

namespace Business.Interfaces.Services
{
    public interface IMessageDispatcher
    {
        Task DispatchAsync(MessageDto message, CancellationToken cancellationToken);
    }
}
