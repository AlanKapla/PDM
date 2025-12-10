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
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repositiories.Repository.Interfaces;
using Repositiories.Repository.Repositories;
using Repositories.Repository.Interfaces;
using Services.Interfaces;
using System.Text;
using System.Linq;
using WebApi.Authorization;
using WebApi.Constants;

namespace WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services
                .AddApiBasics()
                .AddDatabase(config)
                .AddCqrs()
                .AddJwt(config)
                .AddAuthorizationPolicies() // added
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

            // Konfiguracja FormOptions dla multipart/form-data uploads (np. pliki)
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52428800; // 50 MB
                options.ValueLengthLimit = 52428800;
                options.MultipartHeadersLengthLimit = 52428800;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerDocumentation();
            services.AddHealthChecks();
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/keys"));
            
            services.AddSignalR();

            services.AddMemoryCache();

            return services;
        }

        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                var cookieScheme = new OpenApiSecurityScheme
                {
                    Name = CookieKeys.AccessToken,
                    Type = SecuritySchemeType.ApiKey,
                    In = ParameterLocation.Cookie,
                    Description = "JWT stored in HttpOnly cookie named `access_token`",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "CookieAuth"
                    }
                };

                c.AddSecurityDefinition("CookieAuth", cookieScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    { cookieScheme, Array.Empty<string>() }
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
                                4060,
                                40197,
                                40501,
                                40613,
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

        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
        {
            services.AddSingleton<IJwtService, JwtService>();
            services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

            var jwtSection = config.GetSection(JwtSettings.SectionName);
            var secret = jwtSection.GetValue<string>(nameof(JwtSettings.Secret)) ?? throw new ArgumentNullException(nameof(JwtSettings.Secret));
            var issuer = jwtSection.GetValue<string>(nameof(JwtSettings.Issuer));
            var audience = jwtSection.GetValue<string>(nameof(JwtSettings.Audience));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // Sprawdź token z cookie
                        string? accessToken = context.Request.Cookies[CookieKeys.AccessToken];

                        // Dla SignalR: sprawdź query string (WebSocket nie może przesyłać cookies w nagłówkach)
                        var path = context.HttpContext.Request.Path;
                        if (string.IsNullOrEmpty(accessToken) && 
                            path.StartsWithSegments("/api/hubs"))
                        {
                            accessToken = context.Request.Query["access_token"];
                        }

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
            services.AddScoped<IReadRepository<Tenant>, ReadRepository<Tenant>>();
            services.AddScoped<IRepository<Tenant>, Repository<Tenant>>(); 
            services.AddScoped<IReadRepository<Project>, ReadRepository<Project>>();
            services.AddScoped<IReadRepository<ProjectGroup>, ReadRepository<ProjectGroup>>();
            services.AddScoped<IRepository<TenantMember>, Repository<TenantMember>>();
            services.AddScoped<IRepository<ProjectMember>, Repository<ProjectMember>>();
            services.AddScoped<IRepository<ProjectGroupMember>, Repository<ProjectGroupMember>>();
            services.AddScoped<IReadRepository<UserSession>, ReadRepository<UserSession>>();
            services.AddScoped<IRepository<UserPasswordReset>, Repository<UserPasswordReset>>();
            services.AddScoped<IRepository<UserActivation>, Repository<UserActivation>>();
            services.AddScoped<IReadRepository<TenantPreferencesProfile>, ReadRepository<TenantPreferencesProfile>>();
            services.AddScoped<IRepository<TenantPreferencesProfile>, Repository<TenantPreferencesProfile>>();
            services.AddScoped<IRepository<TenantInvitation>, Repository<TenantInvitation>>();
            services.AddScoped<IReadRepository<Notification>, ReadRepository<Notification>>();
            services.AddScoped<IRepository<Notification>, Repository<Notification>>();
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
            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();
            // Application-facing sender enqueues to queue
            services.AddScoped<IEmailSender, QueuedEmailSender>();
            // Low-level transport actually sends via provider (singleton-safe)
            services.AddSingleton<IEmailTransport, SendGridEmailSender>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IBlobStorageService, BlobStorageService>();
            // Queue storage used by hosted services (singleton-safe)
            services.AddSingleton<IQueueStorageService, QueueStorageService>();
            services.AddHostedService<EmailWorker>();

            // Notification dispatcher via SignalR (singleton-safe)
            services.AddSingleton<INotificationDispatcher, WebApi.Services.SignalRNotificationDispatcher>();
            // Notification background worker
            services.AddHostedService<NotificationWorker>();
            services.AddScoped<INotificationSender, QueuedNotificationSender>();

            // Notification mark as read dispatcher via SignalR (singleton-safe)
            services.AddSingleton<INotificationMarkAsReadDispatcher, WebApi.Services.SignalRNotificationMarkAsReadDispatcher>();
            // Notification mark as read background worker
            services.AddHostedService<NotificationMarkAsReadWorker>();
            services.AddScoped<INotificationMarkAsReadSender, QueuedNotificationMarkAsReadSender>();

            services.AddSingleton<IMessageDispatcher, WebApi.Services.SignalRMessageDispatcher>();
            services.AddHostedService<MessageWorker>();

            return services;
        }

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
            services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
            services.Configure<FrontendSettings>(config.GetSection(FrontendSettings.SectionName));
            services.Configure<CorsSettings>(config.GetSection(CorsSettings.SectionName));
            services.Configure<BlobStorageSettings>(config.GetSection(BlobStorageSettings.SectionName));
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
                // Backward compatibility: fallback to single FrontendSettings.BaseUrl
                var frontendSection = config.GetSection(FrontendSettings.SectionName);
                string? url = frontendSection.GetValue<string>(nameof(FrontendSettings.BaseUrl));
                if (!string.IsNullOrWhiteSpace(url))
                {
                    origins = new[] { url.Trim().TrimEnd('/') };
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
