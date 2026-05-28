using Chat.Hubs;
using Chat.Services;
using Microsoft.AspNetCore.SignalR;
using Entities.Models.Chats;
using Entities.Models.Costs;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using ChatModel = Entities.Models.Chats.Chat;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Repositories;

namespace Chat.Registration;

/// <summary>
/// Extension methods to register the Chat module into the ASP.NET Core pipeline.
/// Call AddChat() in Program.cs and MapChatHub() after UseRouting().
/// </summary>
public static class ChatServiceExtensions
{
    /// <summary>
    /// Registers Chat module services: options, repositories, and hub.
    /// </summary>
    public static IServiceCollection AddChat(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ChatOptions>(configuration.GetSection(ChatOptions.SectionName));

        services.AddScoped<IReadRepository<ChatModel>, ReadRepository<ChatModel>>();
        services.AddScoped<IRepository<ChatModel>, Repository<ChatModel>>();
        services.AddScoped<IReadRepository<ChatMember>, ReadRepository<ChatMember>>();
        services.AddScoped<IRepository<ChatMember>, Repository<ChatMember>>();
        services.AddScoped<IReadRepository<MessageHistory>, ReadRepository<MessageHistory>>();
        services.AddScoped<IRepository<MessageHistory>, Repository<MessageHistory>>();
        services.AddScoped<IChatDirectService, ChatDirectService>();

        return services;
    }

    /// <summary>
    /// Maps the ChatHub endpoint. Call after app.UseAuthentication() and app.UseAuthorization().
    /// </summary>
    public static HubEndpointConventionBuilder MapChatHub(this IEndpointRouteBuilder app)
    {
        return app.MapHub<ChatHub>("/api/hubs/chat");
    }
}
