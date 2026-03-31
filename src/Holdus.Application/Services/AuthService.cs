using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Holdus.Application.DTOs;
using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using Holdus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Holdus.Application.Services;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request);
    Task<TokenResponse?> RegisterAsync(RegisterRequest request);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request)
    {
        // Em produção: validar via Identity. Aqui simplificado.
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()  // Login precisa buscar sem filtro de tenant
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.Ativo);

        if (usuario == null) return null;

        // TODO: Validar password via Identity UserManager
        // Placeholder: aceita qualquer senha em dev

        return GenerateToken(usuario);
    }

    public async Task<TokenResponse?> RegisterAsync(RegisterRequest request)
    {
        // Verificar email duplicado
        var exists = await _db.Usuarios
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == request.Email);

        if (exists) return null;

        // Criar tenant (escritório)
        var tenant = new Tenant
        {
            Nome = request.NomeEscritorio,
            Slug = GenerateSlug(request.NomeEscritorio),
            Plano = PlanoTenant.Free,
            MaxUsuarios = 2,
            MaxProjetos = 5,
            StorageLimiteMb = 1024,
            Municipio = request.Municipio ?? "",
            Uf = request.Uf ?? "",
            Ativo = true,
        };

        _db.Tenants.Add(tenant);

        // Criar usuário admin do tenant
        var usuario = new Usuario
        {
            TenantId = tenant.Id,
            NomeCompleto = request.NomeCompleto,
            Email = request.Email,
            Role = RoleUsuario.Admin,
            OabNumero = request.OabNumero,
            OabUf = request.OabUf,
            IdentityUserId = Guid.NewGuid().ToString(), // Em produção: vem do Identity
            Ativo = true,
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        // Recarregar com tenant
        usuario.Tenant = tenant;

        return GenerateToken(usuario);
    }

    private TokenResponse GenerateToken(Usuario usuario)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var secret = jwtConfig["Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationMinutes = int.Parse(jwtConfig["ExpirationMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("tenant_id", usuario.TenantId.ToString()),
            new Claim("tenant_slug", usuario.Tenant?.Slug ?? ""),
            new Claim("role", usuario.Role.ToString()),
            new Claim("name", usuario.NomeCompleto),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateRefreshToken();

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: expiresAt,
            Tenant: new TenantInfo(
                usuario.TenantId,
                usuario.Tenant?.Nome ?? "",
                usuario.Tenant?.Slug ?? "",
                usuario.Tenant?.Plano.ToString() ?? "Free"
            ),
            Usuario: new UsuarioInfo(
                usuario.Id,
                usuario.NomeCompleto,
                usuario.Email,
                usuario.Role.ToString()
            )
        );
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string GenerateSlug(string nome)
    {
        return nome.ToLower()
            .Replace(" ", "-")
            .Replace("ã", "a").Replace("á", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
            .Replace("ú", "u").Replace("ü", "u")
            .Replace("ç", "c")
            .Replace(".", "").Replace(",", "").Replace("/", "")
            + "-" + Guid.NewGuid().ToString()[..4];
    }
}
