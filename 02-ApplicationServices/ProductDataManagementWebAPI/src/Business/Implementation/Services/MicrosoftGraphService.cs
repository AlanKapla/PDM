using Business.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models.ODataErrors;

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
            var users = graphClient.Users;

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
        catch (ODataError ex) when (ex.ResponseStatusCode == 404)
        {
            logger.LogWarning("User with Object ID {ObjectId} not found in Azure AD B2C: {Message}", azureAdB2CObjectId, ex.Message);
            return null;
        }
        catch (ODataError ex)
        {
            logger.LogError(ex, "Graph API error fetching user data for Object ID {ObjectId}", azureAdB2CObjectId);
            throw new InvalidOperationException("Błąd podczas pobierania danych użytkownika z Azure AD.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error fetching user data from Graph API for Object ID {ObjectId}", azureAdB2CObjectId);
            throw;
        }
    }
}
