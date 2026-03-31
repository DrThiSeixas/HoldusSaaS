using Holdus.Domain.Enums;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// MOTOR DOCUMENTAL — Minuta 3.0
// Alteração Contratual: Integralização com Participações
// Societárias (quotas/ações) na Célula Cofre
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Diagnóstico para integralização com participações societárias.
/// Gera até 3 atos: investida + Cofre + averbação S.A.
/// </summary>
public class DiagnosticoParticipacoesCofre
{
    // ── Sociedade receptora (Cofre) ──────────────────────
    public DadosSociedadeAlteracao Cofre { get; set; } = new();
    public decimal CapitalAtualCofre { get; set; }
    public decimal ValorAumento { get; set; }
    public decimal CapitalNovoCofre => CapitalAtualCofre + ValorAumento;
    public decimal ValorPorQuotaCofre { get; set; } = 1.00m;
    public int ClausulaCapitalNumeroCofre { get; set; } = 3;
    public bool CofrePuro { get; set; } = true;
    public bool AtividadeOperacionalPropria { get; set; } = false;
    public List<SocioQuadroAlteracao> QuadroCofre { get; set; } = [];
    public int TotalQuotasNovoCofre => QuadroCofre.Sum(s => s.QuotasPosAumento);

    // ── Sociedade investida (compartilhadora) ────────────
    public DadosSociedadeAlteracao Investida { get; set; } = new();
    public decimal CapitalInvestida { get; set; }
    public decimal ValorPorQuotaInvestida { get; set; } = 1.00m;
    public int ClausulaCapitalNumeroInvestida { get; set; } = 3;
    public List<SocioQuadroInvestida> QuadroInvestidaPosAlteracao { get; set; } = [];

    // ── Sócio conferente ─────────────────────────────────
    public int ConferenteIndex { get; set; }
    public SocioQuadroAlteracao Conferente => QuadroCofre[ConferenteIndex];

    // ── Participação aportada ────────────────────────────
    public TipoAtivoParticipacao TipoAtivo { get; set; }
    public int QuantidadeAportada { get; set; }
    public decimal PercentualInvestida { get; set; }
    public decimal ValorAtribuido { get; set; }
    public UsoParticipacao UsoTotalOuParcial { get; set; }

    // ── Campos extras para ações (S.A.) ──────────────────
    public DadosAcoes? Acoes { get; set; }

    // ── Campos extras para uso parcial ───────────────────
    public int? QuotasConferenteAntesInvestida { get; set; }
    public int? QuotasConferenteDepoisInvestida { get; set; }

    // ── Confirmações expressas ───────────────────────────
    public bool ConfirmaCapitalIntegralizado { get; set; }
    public bool ConfirmaSemAtividadeOperacional { get; set; }
    public bool ConfirmaSemRestricoesTransferencia { get; set; }

    // ── Derivados ────────────────────────────────────────
    public bool MesmaUf => string.Equals(Cofre.Uf, Investida.Uf, StringComparison.OrdinalIgnoreCase);
    public bool EhQuotasLtda => TipoAtivo == TipoAtivoParticipacao.Quotas;
    public bool EhAcoesSA => TipoAtivo == TipoAtivoParticipacao.Acoes;
    public bool DeveGerarAtoInvestida => EhQuotasLtda;
    public bool DeveGerarChecklistSA => EhAcoesSA;
}

/// <summary>
/// Dados de identificação de uma sociedade (usados em ambos os atos).
/// </summary>
public class DadosSociedadeAlteracao
{
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Nire { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public EnderecoContrato Endereco { get; set; } = new();

    public string JuntaComercialNome => Uf switch
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
        _ => $"Junta Comercial do Estado de {Uf}"
    };
}

/// <summary>
/// Sócio no quadro da investida pós-alteração.
/// </summary>
public class SocioQuadroInvestida
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }  // Se for PJ (Cofre ingressando)
    public string? Cpf { get; set; }   // Se for PF
    public bool EhPessoaJuridica { get; set; }
    public int Quotas { get; set; }
    public decimal Percentual { get; set; }
}

/// <summary>
/// Dados específicos para ações de S.A.
/// </summary>
public class DadosAcoes
{
    public string Especie { get; set; } = "ordinárias";  // ordinárias / preferenciais
    public string? Classe { get; set; }
    public string Forma { get; set; } = "nominativas";   // nominativas / escriturais
    public decimal? ValorNominal { get; set; }
}

public enum TipoAtivoParticipacao
{
    Quotas,
    Acoes
}

public enum UsoParticipacao
{
    Total,
    Parcial
}

// ═══════════════════════════════════════════════════════════════
// RESULTADO — Gera múltiplos documentos
// ═══════════════════════════════════════════════════════════════

public class ResultadoMontagemParticipacoes
{
    public bool Sucesso { get; set; }
    public ResultadoValidacao Validacao { get; set; } = new();

    // Ato 1: alteração da investida (se Ltda)
    public string? HtmlAtoInvestida { get; set; }

    // Ato 2: alteração do Cofre
    public string? HtmlAtoCofre { get; set; }

    // Ato 3: checklist averbação S.A.
    public List<string>? ChecklistSA { get; set; }

    public List<string> BlocosCondicionaisAtivados { get; set; } = [];
}
