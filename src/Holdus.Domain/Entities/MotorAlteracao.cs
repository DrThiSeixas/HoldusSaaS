using Holdus.Domain.Enums;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// MOTOR DOCUMENTAL — Minuta 2.0
// Alteração Contratual: Integralização de Imóvel no Cofre
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Diagnóstico para alteração contratual de aumento de capital
/// mediante integralização de imóvel na célula Cofre.
/// </summary>
public class DiagnosticoAlteracaoCofre
{
    // ── Dados da sociedade (já constituída) ───────────────
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Nire { get; set; } = string.Empty;
    public string JuntaComercialUf { get; set; } = string.Empty;
    public EnderecoContrato Endereco { get; set; } = new();
    public int ClausulaCapitalNumero { get; set; } = 3;

    // ── Capital ──────────────────────────────────────────
    public decimal CapitalAtual { get; set; }
    public decimal ValorAumento { get; set; }
    public decimal CapitalNovo => CapitalAtual + ValorAumento;
    public decimal ValorPorQuota { get; set; } = 1.00m;

    // ── Quadro societário pós-alteração ──────────────────
    public List<SocioQuadroAlteracao> QuadroSocietario { get; set; } = [];

    // ── Sócio conferente ─────────────────────────────────
    public int ConferenteIndex { get; set; }
    public SocioQuadroAlteracao Conferente => QuadroSocietario[ConferenteIndex];

    // ── Imóvel ───────────────────────────────────────────
    public ImovelIntegralizacao Imovel { get; set; } = new();

    // ── Cofre ────────────────────────────────────────────
    public bool CofrePuro { get; set; } = true;
    public bool AtividadeOperacionalPropria { get; set; } = false;
    public string? ObjetoSocialAtual { get; set; }

    // ── Confirmações expressas do advogado ────────────────
    public bool ConfirmaSemParcelaForaCapital { get; set; }
    public bool ConfirmaSemAtividadeOperacional { get; set; }

    // ── Derivados ────────────────────────────────────────
    public bool DeveReforcoNarrativo => CofrePuro && ConfirmaSemAtividadeOperacional;
    public int TotalQuotasNovo => QuadroSocietario.Sum(s => s.QuotasPosAumento);

    public string JuntaComercialNome => JuntaComercialUf switch
    {
        "SP" => "Junta Comercial do Estado de São Paulo",
        "MG" => "Junta Comercial do Estado de Minas Gerais",
        "RJ" => "Junta Comercial do Estado do Rio de Janeiro",
        "PR" => "Junta Comercial do Estado do Paraná",
        "SC" => "Junta Comercial do Estado de Santa Catarina",
        "RS" => "Junta Comercial do Estado do Rio Grande do Sul",
        "BA" => "Junta Comercial do Estado da Bahia",
        "GO" => "Junta Comercial do Estado de Goiás",
        "DF" => "Junta Comercial do Distrito Federal",
        _ => $"Junta Comercial do Estado de {JuntaComercialUf}"
    };
}

/// <summary>
/// Sócio no quadro societário da alteração contratual.
/// Contém quotas antes e depois do aumento.
/// </summary>
public class SocioQuadroAlteracao
{
    public int PessoaFisicaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Nacionalidade { get; set; } = "brasileira";
    public EstadoCivil EstadoCivil { get; set; }
    public RegimeBens? RegimeBens { get; set; }
    public bool UniaoEstavel => EstadoCivil == EstadoCivil.UniaoEstavel;
    public string Profissao { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string RgOrgao { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;

    // Quotas
    public int QuotasAntes { get; set; }
    public int QuotasNovas { get; set; }
    public int QuotasPosAumento => QuotasAntes + QuotasNovas;
    public decimal PercentualPosAumento { get; set; }
}

/// <summary>
/// Dados do imóvel sendo integralizado.
/// Cobre urbano e rural com campos condicionais.
/// </summary>
public class ImovelIntegralizacao
{
    public TipoImovelIntegralizacao Tipo { get; set; }
    public decimal ValorAtribuido { get; set; }
    public string Descricao { get; set; } = string.Empty;

    // ── Campos comuns ─────────────────────────────────────
    public string Matricula { get; set; } = string.Empty;
    public string Cartorio { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string TituloAquisitivo { get; set; } = string.Empty;

    // ── Urbano ────────────────────────────────────────────
    public string? CadastroMunicipal { get; set; }

    // ── Rural ─────────────────────────────────────────────
    public string? Denominacao { get; set; }
    public string? MunicipioUf { get; set; }
    public string? Ccir { get; set; }
    public string? NirfCafir { get; set; }
}

public enum TipoImovelIntegralizacao
{
    Urbano,
    Rural
}
