namespace Business.Interfaces.Services;

public interface IMicrosoftGraphService
{
    Task<UserGraphData?> GetUserDataAsync(string azureAdB2CObjectId, CancellationToken cancellationToken = default);
}

public record UserGraphData(string FirstName, string LastName);
