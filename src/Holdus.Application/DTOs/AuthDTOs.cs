namespace Holdus.Application.DTOs;

// ═══════════════════════════════════════════════════════════════
// AUTH DTOs
// ═══════════════════════════════════════════════════════════════

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string NomeCompleto,
    string Email,
    string Password,
    string NomeEscritorio,
    string? OabNumero = null,
    string? OabUf = null,
    string? Municipio = null,
    string? Uf = null
);

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    TenantInfo Tenant,
    UsuarioInfo Usuario
);

public record TenantInfo(Guid Id, string Nome, string Slug, string Plano);
public record UsuarioInfo(Guid Id, string Nome, string Email, string Role);

// ═══════════════════════════════════════════════════════════════
// COMMON DTOs
// ═══════════════════════════════════════════════════════════════

public record ApiResponse<T>(bool Success, T? Data, string? Message = null, string[]? Errors = null);
public record PagedResponse<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
