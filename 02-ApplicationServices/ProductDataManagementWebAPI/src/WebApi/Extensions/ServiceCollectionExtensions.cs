using Azure.Identity;
using Business.Implementation.Model;
using Business.Implementation.Services;
using Business.Interfaces.Configuration;
using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Behaviours;
using Entities.Context;
using Entities.Models;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositiories.Repository.Interfaces;
using Repositiories.Repository.Repositories;
using Repositories.Repository.Interfaces;
using WebApi.Authorization;
using WebApi.Constants;

namespace WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            IdentityModelEventSource.ShowPII = true;

            services
                .AddApiBasics()
                .AddDatabase(config)
                .AddCqrs()
                .AddAzureAdB2C(config)
                .AddMicrosoftGraph(config)
                .AddAuthorizationPolicies()
                .AddAppRepositories()
                .AddAppServices()
                .AddConfigurations(config)
                .AddFrontendCors(config);

            return services;
        }

        public static IServiceCollection AddApiBasics(this IServiceCollection services)
        {
            services
                .AddHttpContextAccessor()
                .AddControllers();

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52428800;
                options.ValueLengthLimit = 52428800;
                options.MultipartHeadersLengthLimit = 52428800;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerDocumentation();
            services.AddHealthChecks();

            var keysPath = Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? @"D:\home", "keys");

            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath));

            services.AddSignalR();
            services.AddMemoryCache();

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Product Data Management API", 
                    Version = "v1",
                    Description = "API protected by Azure AD B2C authentication"
                });

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter Azure AD B2C JWT Bearer token",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                c.AddSecurityDefinition("Bearer", bearerScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { bearerScheme, Array.Empty<string>() }
                });

                var cookieScheme = new OpenApiSecurityScheme
                {
                    Name = CookieKeys.AccessToken,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Description = "JWT stored in HttpOnly cookie (legacy auth)",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "CookieAuth"
                    }
                };

                c.AddSecurityDefinition("CookieAuth", cookieScheme);
            });

            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection"),
                    sql =>
                    {
                        sql.CommandTimeout(60);
                        sql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: new[]
                            {
                                4060, 40197, 40501, 40613,
                                10928, 10929,
                                49918, 49919, 49920
                            });
                    }));
            return services;
        }

        public static IServiceCollection AddCqrs(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
            return services;
        }

        public static IServiceCollection AddAzureAdB2C(this IServiceCollection services, IConfiguration config)
        {
            var azureAdB2CSettings = config.GetSection(AzureAdB2CSettings.SectionName).Get<AzureAdB2CSettings>();

            if (azureAdB2CSettings == null)
            {
                throw new InvalidOperationException("AzureAdB2C settings are not configured");
            }

            var authority = $"{azureAdB2CSettings.Instance.TrimEnd('/')}/{azureAdB2CSettings.TenantId}/v2.0";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.MetadataAddress = $"{authority}/.well-known/openid-configuration";
                    
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://{azureAdB2CSettings.TenantId}.ciamlogin.com/{azureAdB2CSettings.TenantId}/v2.0",
                        ValidateAudience = true,
                        ValidAudiences =
                        [
                            azureAdB2CSettings.ClientId,
                            $"api://{azureAdB2CSettings.ClientId}"
                        ],
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(5),
                        NameClaimType = "name",
                        RoleClaimType = "roles"
                    };

                    options.RefreshOnIssuerKeyNotFound = true;
                    options.AutomaticRefreshInterval = TimeSpan.FromHours(1);

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddAppRepositories(this IServiceCollection services)
        {
            services.AddScoped<IReadRepository<User>, ReadRepository<User>>();
            services.AddScoped<IRepository<User>, ReadRepository<User>>();
            services.AddScoped<IReadRepository<Tenant>, ReadRepository<Tenant>>();
            services.AddScoped<IRepository<Tenant>, Repository<Tenant>>(); 
            services.AddScoped<IReadRepository<Project>, ReadRepository<Project>>();
            services.AddScoped<IRepository<Project>, Repository<Project>>();
            services.AddScoped<IReadRepository<ProjectGroup>, ReadRepository<ProjectGroup>>();
            services.AddScoped<IRepository<TenantMember>, Repository<TenantMember>>();
            services.AddScoped<IRepository<ProjectMember>, Repository<ProjectMember>>();
            services.AddScoped<IRepository<ProjectGroupMember>, Repository<ProjectGroupMember>>();
            services.AddScoped<IReadRepository<UserSession>, ReadRepository<UserSession>>();
            services.AddScoped<IReadRepository<TenantPreferencesProfile>, ReadRepository<TenantPreferencesProfile>>();
            services.AddScoped<IRepository<TenantPreferencesProfile>, Repository<TenantPreferencesProfile>>();
            services.AddScoped<IRepository<TenantInvitation>, Repository<TenantInvitation>>();
            services.AddScoped<IReadRepository<Notification>, ReadRepository<Notification>>();
            services.AddScoped<IRepository<Notification>, Repository<Notification>>();
            services.AddScoped<IReadRepository<ProjectFilePackage>, ReadRepository<ProjectFilePackage>>();
            services.AddScoped<IRepository<ProjectFilePackage>, Repository<ProjectFilePackage>>();
            services.AddScoped<IReadRepository<ProjectFile>, ReadRepository<ProjectFile>>();
            services.AddScoped<IRepository<ProjectFile>, Repository<ProjectFile>>();
            services.AddScoped<IReadRepository<ProjectFileVersion>, ReadRepository<ProjectFileVersion>>();
            services.AddScoped<IRepository<ProjectFileVersion>, Repository<ProjectFileVersion>>();
            services.AddScoped<IReadRepository<ProjectFileVersionComment>, ReadRepository<ProjectFileVersionComment>>();
            services.AddScoped<IRepository<ProjectFileVersionComment>, Repository<ProjectFileVersionComment>>();
            services.AddScoped<IReadRepository<SharedProjectFile>, ReadRepository<SharedProjectFile>>();
            services.AddScoped<IRepository<SharedProjectFile>, Repository<SharedProjectFile>>();
            services.AddScoped<IReadRepository<Chat>, ReadRepository<Chat>>();
            services.AddScoped<IRepository<Chat>, Repository<Chat>>();
            services.AddScoped<IRepository<ChatMember>, Repository<ChatMember>>();
            services.AddScoped<IReadRepository<MessageHistory>, ReadRepository<MessageHistory>>();
            services.AddScoped<IRepository<MessageHistory>, Repository<MessageHistory>>();
            services.AddScoped<IReadRepository<WorkSchedule>, ReadRepository<WorkSchedule>>();
            services.AddScoped<IRepository<WorkSchedule>, Repository<WorkSchedule>>();
            services.AddScoped<IRepository<WorkScheduleStage>, Repository<WorkScheduleStage>>();
            services.AddScoped<IRepository<WorkScheduleStageWork>, Repository<WorkScheduleStageWork>>();
            services.AddScoped<IRepository<WorkScheduleStageWorkAssignment>, Repository<WorkScheduleStageWorkAssignment>>();
            services.AddScoped<IReadRepository<ProjectCost>, ReadRepository<ProjectCost>>();
            services.AddScoped<IRepository<ProjectCost>, Repository<ProjectCost>>();
            services.AddScoped<IReadRepository<SharedProjectCost>, ReadRepository<SharedProjectCost>>();
            services.AddScoped<IRepository<SharedProjectCost>, Repository<SharedProjectCost>>();
            services.AddScoped<IReadRepository<CostEstimateTemplate>, ReadRepository<CostEstimateTemplate>>();
            services.AddScoped<IRepository<CostEstimateTemplate>, Repository<CostEstimateTemplate>>();
            services.AddScoped<IReadRepository<CostEstimate>, ReadRepository<CostEstimate>>();
            services.AddScoped<IRepository<CostEstimate>, Repository<CostEstimate>>();
            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();
            services.AddScoped<IEmailSender, QueuedEmailSender>();
            services.AddSingleton<IEmailTransport, SendGridEmailSender>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IQueueStorageService, QueueStorageService>();
            services.AddHostedService<EmailWorker>();

            services.AddSingleton<INotificationDispatcher, WebApi.Services.SignalRNotificationDispatcher>();
            services.AddHostedService<NotificationWorker>();
            services.AddScoped<INotificationSender, QueuedNotificationSender>();

            services.AddSingleton<INotificationMarkAsReadDispatcher, WebApi.Services.SignalRNotificationMarkAsReadDispatcher>();
            services.AddHostedService<NotificationMarkAsReadWorker>();
            services.AddScoped<INotificationMarkAsReadSender, QueuedNotificationMarkAsReadSender>();

            services.AddSingleton<IMessageDispatcher, WebApi.Services.SignalRMessageDispatcher>();
            services.AddHostedService<MessageWorker>();

            services.AddScoped<IMicrosoftGraphService, MicrosoftGraphService>();

            return services;
        }

        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, IConfiguration config)
        {
            var azureAdB2CSettings = config.GetSection(AzureAdB2CSettings.SectionName).Get<AzureAdB2CSettings>();

            if (azureAdB2CSettings == null)
            {
                throw new InvalidOperationException("AzureAdB2C settings are not configured");
            }

            if (string.IsNullOrEmpty(azureAdB2CSettings.ClientSecret))
            {
                throw new InvalidOperationException("AzureAdB2C:ClientSecret is required for Microsoft Graph API");
            }

            services.AddSingleton(sp =>
            {
                var clientSecretCredential = new ClientSecretCredential(
                    azureAdB2CSettings.TenantId,
                    azureAdB2CSettings.ClientId,
                    azureAdB2CSettings.ClientSecret);

                return new GraphServiceClient(clientSecretCredential);
            });

            return services;
        }

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
            services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
            services.Configure<FrontendSettings>(config.GetSection(FrontendSettings.SectionName));
            services.Configure<CorsSettings>(config.GetSection(CorsSettings.SectionName));
            services.Configure<BlobStorageSettings>(config.GetSection(BlobStorageSettings.SectionName));
            services.Configure<AzureAdB2CSettings>(config.GetSection(AzureAdB2CSettings.SectionName));
            return services;
        }

        public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration config)
        {
            var corsSettings = config.GetSection(CorsSettings.SectionName).Get<CorsSettings>();

            var origins = corsSettings?.AllowedOrigins?
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim().TrimEnd('/'))
                .Distinct()
                .ToArray();

            if (origins == null || origins.Length == 0)
            {
                var frontendSection = config.GetSection(FrontendSettings.SectionName);
                string? url = frontendSection.GetValue<string>(nameof(FrontendSettings.BaseUrl));
                if (!string.IsNullOrWhiteSpace(url))
                {
                    origins = [url.Trim().TrimEnd('/')];
                }
                else
                {
                    throw new ArgumentNullException("No CORS origins configured (CorsSettings.AllowedOrigins or FrontendSettings.BaseUrl).");
                }
            }

            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", builder =>
                {
                    builder
                        .WithOrigins(origins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }

        public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.TenantAdmin, policy => policy.Requirements.Add(new TenantAdminRequirement()));
                options.AddPolicy(Policies.TenantMember, policy => policy.Requirements.Add(new TenantMemberRequirement()));
                options.AddPolicy(Policies.TenantAdminOrOwner, policy => policy.Requirements.Add(new TenantAdminOrOwnerRequirement()));
                options.AddPolicy(Policies.ProjectAdmin, policy => policy.Requirements.Add(new ProjectAdminRequirement()));
                options.AddPolicy(Policies.ProjectMember, policy => policy.Requirements.Add(new ProjectMemberRequirement()));
            });
            services.AddScoped<IAuthorizationHandler, TenantAdminHandler>();
            services.AddScoped<IAuthorizationHandler, TenantMemberHandler>();
            services.AddScoped<IAuthorizationHandler, TenantAdminOrOwnerHandler>();
            services.AddScoped<IAuthorizationHandler, ProjectAdminHandler>();
            services.AddScoped<IAuthorizationHandler, ProjectMemberHandler>();
            return services;
        }
    }
}
