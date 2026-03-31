using Holdus.Domain.Enums;
using Holdus.Domain.Interfaces;

namespace Holdus.Domain.Entities;

/// <summary>
/// Usuário do sistema (advogado, assistente, estagiário).
/// Vinculado ao ASP.NET Core Identity via IdentityUserId.
/// </summary>
public class Usuario : IAuditableEntity, ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    // Identity
    public string IdentityUserId { get; set; } = string.Empty;
    public RoleUsuario Role { get; set; } = RoleUsuario.Advogado;

    // Dados pessoais
    public string NomeCompleto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? OabNumero { get; set; }
    public string? OabUf { get; set; }
    public string? Telefone { get; set; }

    // Controle
    public bool Ativo { get; set; } = true;
    public DateTimeOffset? UltimoAcesso { get; set; }

    // Auditoria
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navegação
    public Tenant Tenant { get; set; } = null!;
}
