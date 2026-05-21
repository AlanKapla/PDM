namespace Business.Interfaces.Services;

public interface IPermissionsVersionService
{
    Task BumpVersionAsync(Guid userId, CancellationToken cancellationToken = default);
    Task BumpVersionsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}
