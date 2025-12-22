using Business.Interfaces.Constants;
using Microsoft.AspNetCore.SignalR;

namespace WebApi.Services;

public class AzureAdB2CUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirst(ClaimNames.Oid)?.Value;
    }
}
