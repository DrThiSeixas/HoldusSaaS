using Holdus.Domain.Enums;
using Holdus.Domain.Interfaces;

namespace Holdus.Domain.Entities;

/// <summary>
/// Tenant = Escritório de advocacia.
/// Raiz do isolamento multi-tenant.
/// Não implementa ITenantEntity (ele É o tenant).
/// </summary>
public class Tenant : IAuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Identificação
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;  // URL-friendly, unique
    public string? CnpjEscritorio { get; set; }

    // Plano e limites
    public PlanoTenant Plano { get; set; } = PlanoTenant.Free;
    public int MaxUsuarios { get; set; } = 2;
    public int MaxProjetos { get; set; } = 5;
    public long StorageLimiteMb { get; set; } = 1024; // 1GB

    // Configuração visual
    public string? LogoUrl { get; set; }
    public string? CorPrimaria { get; set; }  // Hex #RRGGBB

    // Configurações fiscais (Simulador Tributário)
    public RegimeTributario RegimeTributario { get; set; } = RegimeTributario.SimplesNacional;
    public decimal IssAliquota { get; set; } = 5.0m;
    public string Municipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;

    // Configurações gerais (JSON flexível para 31+ parâmetros)
    public string? ConfiguracoesJson { get; set; }

    // Controle
    public bool Ativo { get; set; } = true;

    // Auditoria
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navegação
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
