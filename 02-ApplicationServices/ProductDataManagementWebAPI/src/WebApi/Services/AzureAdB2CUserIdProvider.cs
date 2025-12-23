using Business.Interfaces.Constants;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace WebApi.Services;

public class AzureAdB2CUserIdProvider : IUserIdProvider
{
    private readonly ILogger<AzureAdB2CUserIdProvider> logger;

    public AzureAdB2CUserIdProvider(ILogger<AzureAdB2CUserIdProvider> logger)
    {
        this.logger = logger;
    }

    public string? GetUserId(HubConnectionContext connection)
    {
        // Azure External ID (CIAM) uses short form "oid"
        var userId = connection.User?.FindFirst("oid")?.Value
                     ?? connection.User?.FindFirst(ClaimNames.Oid)?.Value; // Fallback to old format
        
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("❌ SignalR: No 'oid' claim found in token for connection {ConnectionId}", connection.ConnectionId);
            
            // Debug: log all claims
            var claims = connection.User?.Claims?.Select(c => $"{c.Type}={c.Value}");
            if (claims?.Any() == true)
            {
                logger.LogWarning("Available claims: {Claims}", string.Join(", ", claims));
            }
        }
        else
        {
            logger.LogInformation("✅ SignalR: User {UserId} connected with ConnectionId {ConnectionId}", userId, connection.ConnectionId);
        }
        
        return userId;
    }
}
