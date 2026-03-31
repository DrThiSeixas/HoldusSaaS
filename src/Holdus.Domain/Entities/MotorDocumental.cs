using Holdus.Domain.Enums;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// MOTOR DOCUMENTAL — Diagnóstico e Validação da Minuta 1.0
// Célula Cofre — Contrato Social Master
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Diagnóstico completo para montagem do contrato social.
/// Etapa A do motor documental — coleta as 15 variáveis-mãe.
/// </summary>
public class DiagnosticoCofre
{
    // ── Variáveis-mãe ────────────────────────────────────
    public TipoSociedadeLtda TipoSociedade => Socios.Count == 1
        ? TipoSociedadeLtda.Unipessoal
        : TipoSociedadeLtda.Pluripessoal;

    public bool CofrePuro { get; set; } = true;
    public bool AtividadeOperacionalPropria { get; set; } = false;
    public bool HaDescendentes { get; set; }
    public bool HaUsufrutoPlanejado { get; set; }

    // ── Dados da célula ──────────────────────────────────
    public string NomeEmpresarial { get; set; } = string.Empty;
    public EnderecoContrato Endereco { get; set; } = new();
    public string Comarca { get; set; } = string.Empty;
    public decimal CapitalSocial { get; set; }
    public decimal ValorPorQuota { get; set; } = 1.00m;

    // ── Quóruns configuráveis ────────────────────────────
    public int QuorumDeliberacaoEspecial { get; set; } = 75;
    public int QuorumCessaoTerceiros { get; set; } = 75;
    public int QuorumAlteracaoContratual { get; set; } = 75;

    // ── Apuração de haveres ──────────────────────────────
    public int HaveresParcelas { get; set; } = 12;
    public string HaveresIndice { get; set; } = "IPCA";
    public int HaveresPrazoDias { get; set; } = 90;

    // ── Sócios ───────────────────────────────────────────
    public List<SocioDiagnostico> Socios { get; set; } = [];

    // ── Administrador ────────────────────────────────────
    public int AdministradorIndex { get; set; } = 0; // Índice do sócio admin
    public bool AdminEhSocio { get; set; } = true;
    public string AdminPrazo { get; set; } = "indeterminado";

    // ── Derivados (calculados) ───────────────────────────
    public int TotalQuotas => Socios.Sum(s => s.Quotas);
    public bool TemImóvelUrbano => Socios.Any(s => s.Integralizacoes.Any(i => i.Tipo == TipoIntegralizacao.ImovelUrbano));
    public bool TemImovelRural => Socios.Any(s => s.Integralizacoes.Any(i => i.Tipo == TipoIntegralizacao.ImovelRural));
    public bool TemParticipacoes => Socios.Any(s => s.Integralizacoes.Any(i => i.Tipo == TipoIntegralizacao.Participacoes));
    public bool TemImovelQualquer => TemImóvelUrbano || TemImovelRural;
    public bool DeveTravaCofre => CofrePuro && !AtividadeOperacionalPropria;
    public bool DeveCoerenciaIntegralizacao => TemImovelQualquer;
}

/// <summary>
/// Dados de um sócio para o diagnóstico.
/// </summary>
public class SocioDiagnostico
{
    // ── Pessoa ────────────────────────────────────────────
    public int PessoaFisicaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Nacionalidade { get; set; } = "brasileira";
    public EstadoCivil EstadoCivil { get; set; }
    public RegimeBens? RegimeBens { get; set; }
    public bool UniaoEstavel => EstadoCivil == EstadoCivil.UniaoEstavel;
    public bool HaPactoConvivencia { get; set; }
    public string Profissao { get; set; } = string.Empty;

    // ── Documentos (descriptografados no momento do uso) ──
    public string Rg { get; set; } = string.Empty;
    public string RgOrgao { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;

    // ── Cônjuge ──────────────────────────────────────────
    public string? NomeConjuge { get; set; }
    public string? CpfConjuge { get; set; }
    public bool NecessitaAnuenciaConjugal { get; set; }

    // ── Quotas ───────────────────────────────────────────
    public int Quotas { get; set; }
    public decimal PercentualParticipacao { get; set; }

    // ── Integralização ───────────────────────────────────
    public List<IntegralizacaoDiagnostico> Integralizacoes { get; set; } = [];
}

/// <summary>
/// Dados de um bem/valor usado na integralização.
/// </summary>
public class IntegralizacaoDiagnostico
{
    public TipoIntegralizacao Tipo { get; set; }
    public decimal Valor { get; set; }
    public string? Descricao { get; set; }

    // ── Imóvel Urbano (BC3) ──────────────────────────────
    public string? Matricula { get; set; }
    public string? Cartorio { get; set; }
    public string? EnderecoImovel { get; set; }
    public string? Area { get; set; }
    public string? CadastroMunicipal { get; set; }

    // ── Imóvel Rural (BC4) ───────────────────────────────
    public string? Denominacao { get; set; }
    public string? MunicipioUf { get; set; }
    public string? Ccir { get; set; }
    public string? NirfCafir { get; set; }

    // ── Participação (BC5) ───────────────────────────────
    public string? NomeInvestida { get; set; }
    public string? CnpjInvestida { get; set; }
    public int? QtdQuotasAcoes { get; set; }
    public decimal? PercentualInvestida { get; set; }
    public string? TipoParticipacao { get; set; } // "quotas" ou "ações"
}

/// <summary>
/// Endereço da sede da sociedade.
/// </summary>
public class EnderecoContrato
{
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
    public string? Comarca { get; set; }

    public string Completo => string.Join(", ",
        new[] { Logradouro, Numero, Complemento, Bairro, Cidade, Uf, $"CEP {Cep}" }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}

// ═══════════════════════════════════════════════════════════════
// ENUMS ESPECÍFICOS DO MOTOR DOCUMENTAL
// ═══════════════════════════════════════════════════════════════

public enum TipoSociedadeLtda
{
    Pluripessoal,
    Unipessoal
}

public enum TipoIntegralizacao
{
    Dinheiro,
    ImovelUrbano,
    ImovelRural,
    Participacoes,
    BemMovel,
    Direitos
}

// ═══════════════════════════════════════════════════════════════
// RESULTADO DA VALIDAÇÃO (Etapa B)
// ═══════════════════════════════════════════════════════════════

public class ResultadoValidacao
{
    public bool PodeGerar => !Erros.Any(e => e.Severidade == SeveridadeValidacao.Bloqueio);
    public List<ItemValidacao> Erros { get; set; } = [];
    public List<ItemValidacao> Alertas => Erros.Where(e => e.Severidade == SeveridadeValidacao.Alerta).ToList();
    public List<ItemValidacao> Bloqueios => Erros.Where(e => e.Severidade == SeveridadeValidacao.Bloqueio).ToList();
    public List<string> ChecklistRegistral { get; set; } = [];
}

public class ItemValidacao
{
    public string Codigo { get; set; } = string.Empty;  // V1, V2, ...
    public string Mensagem { get; set; } = string.Empty;
    public SeveridadeValidacao Severidade { get; set; }
    public string? FundamentoLegal { get; set; }
    public string? Sugestao { get; set; }
}

public enum SeveridadeValidacao
{
    Alerta,
    Bloqueio
}
