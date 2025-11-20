using Business.Implementation.Model;
using Business.Implementation.Services;
using Business.Interfaces.Configuration;
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
using WebApi.Constants;
using WebApi.Filters.Project;

namespace WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiBasics(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddEndpointsApiExplorer();
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

            services.AddHealthChecks();

            services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/keys"));

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
                            maxRetryCount: 5,                 // ile ponowień
                            maxRetryDelay: TimeSpan.FromSeconds(30), // max odstęp
                            errorNumbersToAdd: new[]           // (opcjonalnie) dodatkowe kody błędów SQL
                            {
                                // przykładowe transienty (Azure SQL/SQL Server)
                                4060,   // Cannot open database
                                40197,  // The service has encountered an error
                                40501,  // Throttling
                                40613,  // Database not currently available
                                10928, 10929, // Resource limits
                                49918, 49919, 49920 // Service busy
                            });
                    }));
            return services;
        }

        public static IServiceCollection AddCqrs(this IServiceCollection services)
        {
            // FluentValidation from all loaded assemblies
            services.AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies());

            // Validation pipeline behavior
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Transaction pipeline behavior
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

            // MediatR handlers from all loaded assemblies
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

            return services;
        }

        public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration config)
        {
            // Bind options for convenient DI if needed elsewhere
            services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));

            // Services related to auth
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
                options.RequireHttpsMetadata = false; // consider true in prod behind HTTPS
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

                        if (string.IsNullOrEmpty(accessToken))
                        {
                            string authHeader = context.Request.Headers["Authorization"].ToString();
                            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                            {
                                accessToken = authHeader.Substring("Bearer ".Length).Trim();
                            }
                        }

                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();

            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.ProjectAccess, policy =>
                    policy.Requirements.Add(new ProjectAccessRequirement()));

            return services;
        }

        public static IServiceCollection AddAppRepositories(this IServiceCollection services)
        {
            services.AddScoped<IReadRepository<User>, ReadRepository<User>>();
            services.AddScoped<IReadRepository<Tenant>, ReadRepository<Tenant>>();
            services.AddScoped<IReadRepository<Project>, ReadRepository<Project>>();
            services.AddScoped<IReadRepository<ProjectGroup>, ReadRepository<ProjectGroup>>();
            services.AddScoped<IRepository<TenantMember>, Repository<TenantMember>>();
            services.AddScoped<IRepository<ProjectMember>, Repository<ProjectMember>>();
            services.AddScoped<IRepository<ProjectGroupMember>, Repository<ProjectGroupMember>>();
            services.AddScoped<IReadRepository<UserSession>, ReadRepository<UserSession>>();

            return services;
        }

        public static IServiceCollection AddAppServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IHttpCookieService, HttpCookieService>();

            return services;
        }
    }
}
