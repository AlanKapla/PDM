using Azure.Identity;
using Business.AIAgent.Extensions;
using Business.Implementation.Model;
using Business.Implementation.Services;
using Business.Implementation.Services.Excel;
using Business.Implementation.Validators;
using Business.Interfaces.Configuration;
using Business.Interfaces.Configurations;
using Business.Interfaces.Constants;
using Business.Interfaces.Model;
using Business.Interfaces.Services;
using CQRS.Behaviours;
using Entities.Context;
using Entities.Models;
using Entities.Models.CostEstimates;
using Entities.Models.CostEstimateTemplates;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositories.Repository.Interfaces;
using Repositories.Repository.Repositories;
using WebApi.Authorization;
using WebApi.Services;

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
                .AddFrontendCors(config)
                .AddAIAgent(config)      // AI Agent Framework with Semantic Kernel
                .AddAIPlugins();         // AI Agent Plugins (DI registration)

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
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
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
            services.AddScoped<IReadRepository<PermissionsVersionProfile>, ReadRepository<PermissionsVersionProfile>>();
            services.AddScoped<IRepository<PermissionsVersionProfile>, Repository<PermissionsVersionProfile>>();
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
            services.AddScoped<IRepository<WorkScheduleStageWorkComment>, Repository<WorkScheduleStageWorkComment>>();
            services.AddScoped<IReadRepository<ProjectCost>, ReadRepository<ProjectCost>>();
            services.AddScoped<IRepository<ProjectCost>, Repository<ProjectCost>>();
            services.AddScoped<IReadRepository<SharedProjectCost>, ReadRepository<SharedProjectCost>>();
            services.AddScoped<IRepository<SharedProjectCost>, Repository<SharedProjectCost>>();
            services.AddScoped<IReadRepository<CostEstimateTemplate>, ReadRepository<CostEstimateTemplate>>();
            services.AddScoped<IRepository<CostEstimateTemplate>, Repository<CostEstimateTemplate>>();
            services.AddScoped<IRepository<CostEstimateTemplateCurrency>, Repository<CostEstimateTemplateCurrency>>();
            services.AddScoped<IRepository<CostEstimateTemplateUnit>, Repository<CostEstimateTemplateUnit>>();
            services.AddScoped<IRepository<CostEstimateTemplateGroupFieldDefinition>, Repository<CostEstimateTemplateGroupFieldDefinition>>();
            services.AddScoped<IRepository<CostEstimateTemplateItemSystemFieldDefinition>, Repository<CostEstimateTemplateItemSystemFieldDefinition>>();
            services.AddScoped<IRepository<CostEstimateTemplateItemCalculatedFieldDefinition>, Repository<CostEstimateTemplateItemCalculatedFieldDefinition>>();
            services.AddScoped<IRepository<CostEstimateTemplateItemGenericFieldDefinition>, Repository<CostEstimateTemplateItemGenericFieldDefinition>>();
            services.AddScoped<IReadRepository<CostEstimate>, ReadRepository<CostEstimate>>();
            services.AddScoped<IRepository<CostEstimate>, Repository<CostEstimate>>();
            services.AddScoped<IRepository<CostEstimateGroup>, Repository<CostEstimateGroup>>();
            services.AddScoped<IRepository<CostEstimateGroupFieldValue>, Repository<CostEstimateGroupFieldValue>>();
            services.AddScoped<IRepository<CostEstimateItem>, Repository<CostEstimateItem>>();
            services.AddScoped<IRepository<CostEstimateItemFieldValue>, Repository<CostEstimateItemFieldValue>>();
            services.AddScoped<IRepository<CostEstimateFile>, Repository<CostEstimateFile>>();
            services.AddScoped<IReadRepository<Role>, ReadRepository<Role>>();
            services.AddScoped<IRepository<Role>, Repository<Role>>();
            services.AddScoped<IReadRepository<Permission>, ReadRepository<Permission>>();
            services.AddScoped<IRepository<Permission>, Repository<Permission>>();
            services.AddScoped<IRepository<RolePermission>, Repository<RolePermission>>();
            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUser, CurrentUser>();
            
            // New permission-based services
            services.AddSingleton<IUserContextCache, InMemoryUserContextCache>();
            services.AddScoped<AccessService>();
            services.AddScoped<PermissionsVersionService>();
            
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();
            services.AddScoped<IEmailSender, QueuedEmailSender>();
            services.AddSingleton<IEmailTransport, SendGridEmailSender>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            services.AddSingleton<IQueueStorageService, QueueStorageService>();
            services.AddHostedService<EmailWorker>();

            services.AddSingleton<INotificationDispatcher, SignalRNotificationDispatcher>();
            services.AddHostedService<NotificationWorker>();
            services.AddScoped<INotificationSender, QueuedNotificationSender>();

            services.AddSingleton<INotificationMarkAsReadDispatcher, SignalRNotificationMarkAsReadDispatcher>();
            services.AddHostedService<NotificationMarkAsReadWorker>();
            services.AddScoped<INotificationMarkAsReadSender, QueuedNotificationMarkAsReadSender>();

            // ✅ Background services
            services.AddHostedService<FileShareConsolidationService>();

            services.AddSingleton<IMessageDispatcher, SignalRMessageDispatcher>();
            services.AddHostedService<MessageWorker>();

            services.AddScoped<IMicrosoftGraphService, MicrosoftGraphService>();
            
            // File access service - checking access with Package + Allow/Deny model
            services.AddScoped<IFileAccessService, FileAccessService>();
            
            
            // Cost estimate calculation service
            services.AddScoped<ICostEstimateCalculationService, CostEstimateCalculationService>();
            
            // Template structure service - used in multiple handlers
            services.AddScoped<ITemplateStructureService, TemplateStructureService>();
            
            // ✅ NEW: Business services for CostEstimate and Template lifecycle
            services.AddScoped<ICostEstimateTemplateService, CostEstimateTemplateService>();
            services.AddScoped<ICostEstimateService, CostEstimateService>();
            
            // ✅ Excel import storage service
            services.AddScoped<ICostEstimateExcelStorageService, CostEstimateExcelStorageService>();
            
            // Excel parser service - for cost estimate import
            services.AddScoped<IExcelParserService, ExcelParserService>();
            
            // Cost estimate validators
            services.AddScoped<CostEstimateGroupValidator>();
            services.AddScoped<CostEstimateItemValidator>();

            services.AddHostedService<StartupSeederService>();
            services.AddHostedService<RolePermissionSeederService>();

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
            services.Configure<SeedSettings>(config.GetSection(SeedSettings.SectionName));
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
    }
}
