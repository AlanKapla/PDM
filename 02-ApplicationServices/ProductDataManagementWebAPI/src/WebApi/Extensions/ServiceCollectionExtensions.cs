using Azure.Identity;
using Business.AIAgent.Registration;
using Business.Implementation.Model;
using Business.Implementation.Services;
using Business.Interfaces.Configuration;
using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using Chat.Registration;
using CQRS.Behaviours;
using CQRS.PostCommit;
using CQRS.WorkSchedules.Shared;
using Entities.Context;
using Entities.Models;
using Entities.Models.Base;
using Entities.Models.CostEstimates;
using Entities.Models.Costs;
using Entities.Models.CostTrackers;
using Entities.Models.Files;
using Entities.Models.Notifications;
using Entities.Models.Projects;
using Entities.Models.Tenants;
using Entities.Models.Users;
using Entities.Models.WorkSchedules;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Repositories;
using WebApi.Authorization;
using WebApi.Services;
using Business.Implementation.Services.AI;

namespace WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services
                .AddApiBasics()
                .AddDatabase(config)
                .AddRedisCache(config)
                .AddCqrs()
                .AddAzureAdB2C(config)
                .AddMicrosoftGraph(config)
                .AddAuthorizationPolicies()
                .AddAppRepositories()
                .AddAppServices()
                .AddConfigurations(config)
                .AddFrontendCors(config)
                .AddChat(config)
                .AddAIAgent(config);

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

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(System.Globalization.CultureInfo.InvariantCulture);
                options.SupportedCultures = [System.Globalization.CultureInfo.InvariantCulture];
                options.SupportedUICultures = [System.Globalization.CultureInfo.InvariantCulture];
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerDocumentation();
            services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("database");

            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = false; // Wyłączone w produkcji dla bezpieczeństwa
                options.KeepAliveInterval = TimeSpan.FromSeconds(10); // Ping co 10s
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30); // Timeout po 30s bez odpowiedzi
                options.HandshakeTimeout = TimeSpan.FromSeconds(15);
                options.MaximumReceiveMessageSize = 102400; // 100 KB
            });

            services.AddSingleton<IUserIdProvider, AzureAdB2CUserIdProvider>();

            services.AddMemoryCache();

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "Brickly API", 
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

                c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);
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
                            errorNumbersToAdd: [4060, 40197, 40501, 40613, 10928, 10929, 49918, 49919, 49920]);
                    }));
            return services;
        }

        public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration config)
        {
            var redisSettings = config.GetSection(RedisSettings.SectionName).Get<RedisSettings>();

            if (redisSettings != null && redisSettings.IsEnabled && !string.IsNullOrWhiteSpace(redisSettings.ConnectionString))
            {
                services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                {
                    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisSettings.ConnectionString);
                    configuration.AbortOnConnectFail = false;
                    configuration.ConnectTimeout = 15000;
                    configuration.SyncTimeout = 15000;
                    configuration.Ssl = true;
                    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
                });

                services.AddHealthChecks()
                    .AddCheck("redis", () =>
                    {
                        // IConnectionMultiplexer is resolved inline — health check runs outside DI scope
                        var connectionString = redisSettings.ConnectionString;
                        try
                        {
                            var mux = StackExchange.Redis.ConnectionMultiplexer.Connect(connectionString);
                            return mux.IsConnected
                                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
                                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Redis connection not established.");
                        }
                        catch (Exception ex)
                        {
                            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(ex.Message);
                        }
                    });
            }

            // Jeśli Redis wyłączony — IConnectionMultiplexer nie jest rejestrowany.
            // CacheService przyjmuje go jako opcjonalny (IConnectionMultiplexer? redis = null)
            // i automatycznie przechodzi w tryb bypass bez cache.

            return services;
        }

        public static IServiceCollection AddCqrs(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AssignedAuthorizationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            services.AddScoped<IPostCommitDispatcher, PostCommitDispatcher>();
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
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                logger.LogInformation("Token received from query string for path: {Path}", context.Request.Path);
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                            logger.LogError(context.Exception, "Authentication failed for path: {Path}", context.Request.Path);
                            return Task.CompletedTask;
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddAppRepositories(this IServiceCollection services)
        {
            // IRepository<User> intentionally resolves to ReadRepository — users are managed via Azure AD B2C
            services.AddScoped<IReadRepository<User>, ReadRepository<User>>();
            services.AddScoped<IRepository<User>, ReadRepository<User>>();

            services
                .AddRepository<Tenant>()
                .AddRepository<TenantPreferencesProfile>()
                .AddRepository<PermissionsVersionProfile>()
                .AddWriteRepository<TenantMember>()
                .AddRepository<TenantInvitation>()
                .AddWriteRepository<TenantInvitationModulePermission>()
                .AddReadOnlyRepository<UserSession>()
                .AddRepository<Contractor>();

            services
                .AddRepository<Project>()
                .AddWriteRepository<ProjectMember>()
                .AddWriteRepository<ProjectMemberModulePermission>()
                .AddRepository<ProjectParams>()
                .AddRepository<ProjectCurrency>()
                .AddRepository<ProjectUnit>();

            services
                .AddRepository<Notification>();

            services
                .AddRepository<ProjectFilePackage>()
                .AddRepository<ProjectFile>()
                .AddRepository<ProjectFileVersion>()
                .AddRepository<ProjectFileVersionComment>()
                .AddRepository<SharedProjectFile>();

            services
                .AddRepository<WorkSchedule>()
                .AddRepository<WorkScheduleStage>()
                .AddRepository<WorkScheduleStageWork>()
                .AddRepository<WorkScheduleStageWorkPeriod>()
                .AddWriteRepository<WorkScheduleStageWorkAssignment>()
                .AddWriteRepository<WorkScheduleStageWorkComment>()
                .AddWriteRepository<WorkScheduleStageWorkDependency>();

            services
                .AddRepository<ProjectCost>();

            services
                .AddRepository<CostEstimate>()
                .AddRepository<SharedCostEstimate>()
                .AddRepository<CostEstimateGroup>()
                .AddRepository<CostEstimateItem>()
                .AddRepository<CostEstimateFieldSchema>()
                .AddRepository<CostEstimateAdditionalFieldValue>()
                .AddRepository<CostEstimateItemFile>();

            services
                .AddRepository<TrackedCost>()
                .AddRepository<BaseCostAttachment>();


            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddSingleton<IUserContextCache, InMemoryUserContextCache>();
            services.AddScoped<IAccessService, AccessService>();
            services.AddScoped<IPermissionsVersionService, PermissionsVersionService>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();
            services.AddScoped<IEmailSender, QueuedEmailSender>();
            services.AddSingleton<IEmailTransport, SmtpEmailSender>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IQueueStorageService, QueueStorageService>();
            services.AddHostedService<EmailWorker>();

            services.AddSingleton<INotificationDispatcher, SignalRNotificationDispatcher>();
            services.AddScoped<INotificationSender, QueuedNotificationSender>();
            services.AddHostedService<NotificationWorker>();

            services.AddSingleton<INotificationMarkAsReadDispatcher, SignalRNotificationMarkAsReadDispatcher>();
            services.AddScoped<INotificationMarkAsReadSender, QueuedNotificationMarkAsReadSender>();
            services.AddHostedService<NotificationMarkAsReadWorker>();

            services.AddHostedService<FileShareConsolidationService>();

            services.AddSingleton<IMessageDispatcher, SignalRMessageDispatcher>();
            services.AddHostedService<MessageWorker>();

            services.AddScoped<IMicrosoftGraphService, MicrosoftGraphService>();
            services.AddScoped<ICostEstimateCalculationService, CostEstimateCalculationService>();
            services.AddScoped<ICostEstimateRecalculationService, CostEstimateRecalculationService>();
            services.AddScoped<ICostEstimateCacheService, CostEstimateCacheService>();
            services.AddScoped<ICostEstimateAccessService, CostEstimateAccessService>();
            services.AddScoped<IProjectCostAccessService, ProjectCostAccessService>();
            services.AddScoped<ICostEstimateShareService, CostEstimateShareService>();
            services.AddScoped<IContractorService, ContractorService>();
            services.AddSingleton<ICostTrackerFinancialService, CostTrackerFinancialService>();
            services.AddScoped<ICostTrackerAttachmentService, CostTrackerAttachmentService>();
            services.AddSingleton<ICostTrackerTimelineService, CostTrackerTimelineService>();
            services.AddScoped<IWorkScheduleSyncService, WorkScheduleSyncService>();
            services.AddScoped<IWorkScheduleNotificationService, WorkScheduleNotificationService>();
            services.AddScoped<IWorkScheduleCacheService, WorkScheduleCacheService>();
            services.AddScoped<IWorkScheduleAccessService, WorkScheduleAccessService>();
            services.AddScoped<WorkScheduleBuilder>();
            services.AddScoped<IWorkScheduleAIGeneratorService, WorkScheduleAIGeneratorService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProjectMemberService, ProjectMemberService>();
            services.AddScoped<IProjectMembershipProvisioner, ProjectMembershipProvisioner>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddScoped<IProjectFilesService, ProjectFilesService>();
            services.AddScoped<IFileAccessGuard, FileAccessGuard>();
            services.AddSingleton<IFileShareDiffService, Business.Implementation.Services.Files.FileShareDiffService>();
            services.AddScoped<IFileShareNotificationService, Business.Implementation.Services.Files.FileShareNotificationService>();
            services.AddSingleton<IFileVersionWebMapper, Business.Implementation.Services.Files.FileVersionWebMapper>();
            services.AddScoped<IDashboardDataLoader, DashboardDataLoader>();
            services.AddScoped<IScheduleSummaryBuilder, ScheduleSummaryBuilder>();
            services.AddScoped<IProjectTimelineAggregator, ProjectTimelineAggregator>();
            services.AddScoped<IProjectDashboardAssembler, ProjectDashboardAssembler>();

            services.AddHostedService<StartupSeederService>();

            services.AddScoped<IDocumentParserService, DocumentParserService>();
            services.AddScoped<ICostEstimateAIGeneratorService, CostEstimateAIGeneratorService>();

            return services;
        }

        public static IServiceCollection AddMicrosoftGraph(this IServiceCollection services, IConfiguration config)
        {
            var azureAdB2CSettings = config.GetSection(AzureAdB2CSettings.SectionName).Get<AzureAdB2CSettings>()
                ?? throw new InvalidOperationException("AzureAdB2C settings are not configured");

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
            services.Configure<SmtpSettings>(config.GetSection(SmtpSettings.SectionName));
            services.Configure<FrontendSettings>(config.GetSection(FrontendSettings.SectionName));
            services.Configure<CorsSettings>(config.GetSection(CorsSettings.SectionName));
            services.Configure<BlobStorageSettings>(config.GetSection(BlobStorageSettings.SectionName));
            services.Configure<AzureAdB2CSettings>(config.GetSection(AzureAdB2CSettings.SectionName));
            services.Configure<SeedSettings>(config.GetSection(SeedSettings.SectionName));
            services.Configure<RedisSettings>(config.GetSection(RedisSettings.SectionName));
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
                // Auto-register all permission-based policies with their scopes
                foreach (var permissionCode in PermissionCodes.All)
                {
                    var scope = PermissionScopes.Get(permissionCode);
                    options.AddPolicy(permissionCode, policy =>
                        policy.Requirements.Add(new PermissionRequirement(permissionCode, scope)));
                }
            });

            // Permission-based handler
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }

        public static IServiceCollection AddRepository<T>(this IServiceCollection services) where T : BaseEntity
            => services
                .AddScoped<IReadRepository<T>, ReadRepository<T>>()
                .AddScoped<IRepository<T>, Repository<T>>();

        public static IServiceCollection AddReadOnlyRepository<T>(this IServiceCollection services) where T : BaseEntity
            => services.AddScoped<IReadRepository<T>, ReadRepository<T>>();

        public static IServiceCollection AddWriteRepository<T>(this IServiceCollection services) where T : class
            => services.AddScoped<IRepository<T>, Repository<T>>();
    }
}
