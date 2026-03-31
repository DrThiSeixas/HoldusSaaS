using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Holdus.API.Middleware;
using Holdus.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Holdus.API.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Registra PostgreSQL + EF Core com multi-tenant provider.
    /// Suporta Railway (DATABASE_URL) e local (ConnectionStrings:DefaultConnection).
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            // Railway injeta DATABASE_URL; local usa appsettings.json
            var connStr = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(connStr))
            {
                // Railway format: postgresql://user:pass@host:port/db
                // Npgsql aceita esse formato diretamente
                if (connStr.StartsWith("postgresql://") || connStr.StartsWith("postgres://"))
                {
                    var uri = new Uri(connStr);
                    var userInfo = uri.UserInfo.Split(':');
                    connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
                }
            }
            else
            {
                connStr = config.GetConnectionString("DefaultConnection");
            }

            options.UseNpgsql(connStr, npgsql =>
            {
                npgsql.MigrationsAssembly("Holdus.Infrastructure");
                npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                npgsql.CommandTimeout(30);
            });
        });

        // Tenant provider (scoped — um por request)
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, HttpTenantProvider>();

        return services;
    }

    /// <summary>
    /// Registra autenticação JWT com claims de tenant.
    /// Suporta Railway (variáveis de ambiente) e local (appsettings.json).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwtSection = config.GetSection("Jwt");
        // Railway: JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE via env vars
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? jwtSection["Secret"]!;
        var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? jwtSection["Issuer"]!;
        var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? jwtSection["Audience"]!;
        var key = Encoding.UTF8.GetBytes(secret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
            options.AddPolicy("AdvogadoOrAdmin", p => p.RequireClaim("role", "Admin", "Advogado"));
        });

        return services;
    }

    /// <summary>
    /// Registra Redis para caching distribuído.
    /// </summary>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration config)
    {
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = config.GetConnectionString("Redis");
            options.InstanceName = "Holdus:";
        });

        return services;
    }

    /// <summary>
    /// CORS para frontend React (Next.js).
    /// Suporta dev local + Railway + domínio customizado.
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                var origins = new List<string>
                {
                    "http://localhost:3000",       // Next.js dev
                    "http://localhost:5173",       // Vite dev
                    "https://app.holdus.com.br"   // Produção
                };

                // Railway: frontend URL injetada via variável de ambiente
                var railwayFrontend = Environment.GetEnvironmentVariable("FRONTEND_URL");
                if (!string.IsNullOrEmpty(railwayFrontend))
                    origins.Add(railwayFrontend);

                builder
                    .WithOrigins(origins.ToArray())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Rate limiting por tenant (protege contra abuso).
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("RateLimiting");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("PerTenant", limiter =>
            {
                limiter.PermitLimit = section.GetValue<int>("PermitLimit", 100);
                limiter.Window = TimeSpan.FromSeconds(section.GetValue<int>("WindowSeconds", 60));
                limiter.QueueLimit = 5;
            });
        });

        return services;
    }

    /// <summary>
    /// Registra serviços do Holdus (workflow, execução, etc).
    /// </summary>
    public static IServiceCollection AddHoldusServices(this IServiceCollection services)
    {
        services.AddScoped<Holdus.Infrastructure.Data.Services.WorkflowInitializationService>();
        services.AddScoped<Holdus.Infrastructure.Data.Services.WorkflowExecutionService>();
        return services;
    }
}