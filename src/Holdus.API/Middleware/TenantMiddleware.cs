using System.Security.Claims;
using Holdus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Holdus.API.Middleware;

/// <summary>
/// Implementação do ITenantProvider.
/// Lê TenantId e UserId dos claims JWT no HttpContext.
/// Registrado como Scoped no DI.
/// </summary>
public class HttpTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _http;

    public HttpTenantProvider(IHttpContextAccessor http)
    {
        _http = http;
    }

    public Guid TenantId
    {
        get
        {
            var claim = _http.HttpContext?.User?.FindFirst("tenant_id");
            return claim != null && Guid.TryParse(claim.Value, out var id)
                ? id
                : Guid.Empty;
        }
    }

    public Guid? UserId
    {
        get
        {
            var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)
                     ?? _http.HttpContext?.User?.FindFirst("sub");
            return claim != null && Guid.TryParse(claim.Value, out var id)
                ? id
                : null;
        }
    }
}

/// <summary>
/// Middleware que seta o tenant_id na sessão do PostgreSQL para RLS.
/// Executado após autenticação JWT, antes dos controllers.
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var tenantClaim = context.User?.FindFirst("tenant_id");

        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            // Seta variável de sessão do PostgreSQL para RLS
            // Guid já validado via TryParse — sem risco de SQL injection
            await db.Database.ExecuteSqlRawAsync(
                $"SET app.current_tenant_id = '{tenantId}'"
            );
        }

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }
}
