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

            services.AddEndpointsApiExplorer();
            services.AddSwaggerDocumentation();
            services.AddHealthChecks();
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/keys"));

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
                        string? accessToken = context.Request.Cookies[CookieKeys.AccessToken];

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
            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();
            services.AddScoped<IEmailSender, SendGridEmailSender>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            return services;
        }

        public static IServiceCollection AddConfigurations(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
            services.Configure<EmailSettings>(config.GetSection(EmailSettings.SectionName));
            services.Configure<FrontendSettings>(config.GetSection(FrontendSettings.SectionName));
            services.Configure<CorsSettings>(config.GetSection(CorsSettings.SectionName));
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
                options.AddPolicy("TenantAdmin", policy => policy.Requirements.Add(new TenantAdminRequirement()));
            });
            services.AddScoped<IAuthorizationHandler, TenantAdminHandler>();
            return services;
        }
    }
}
