using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace Business.Implementation.Services;

public class MicrosoftGraphService : IMicrosoftGraphService
{
    private readonly GraphServiceClient graphClient;
    private readonly ILogger<MicrosoftGraphService> logger;

    public MicrosoftGraphService(
        GraphServiceClient graphClient,
        ILogger<MicrosoftGraphService> logger)
    {
        this.graphClient = graphClient;
        this.logger = logger;
    }

    public async Task<UserGraphData?> GetUserDataAsync(string azureAdB2CObjectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await graphClient.Users[azureAdB2CObjectId]
                .GetAsync(requestConfig =>
                {
                    requestConfig.QueryParameters.Select = new[] { "givenName", "surname" };
                }, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("User with Object ID {ObjectId} not found in Graph API", azureAdB2CObjectId);
                return null;
            }

            return new UserGraphData(
                user.GivenName ?? string.Empty,
                user.Surname ?? string.Empty
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user data from Graph API for Object ID {ObjectId}", azureAdB2CObjectId);
            return null;
        }
    }
}
