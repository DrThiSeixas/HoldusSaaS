#nullable enable
using System.Collections.Generic;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// ACORDO DE QUOTISTAS — Profile para Matriz de Decisão
// Usado pelo MatrizDecisaoAcordoEngine nas 16 perguntas
// ═══════════════════════════════════════════════════════════════

// ── Enums ────────────────────────────────────────────────────

public enum ModoVotoUsufruto
{
    SemUsufruto = 0,
    UsufrutuarioIntegral = 1,
    NuProprietarioIntegral = 2,
    Misto = 3,
}

public enum ModoEconomicoUsufruto
{
    NaoDefinido = 0,
    UsufrutuarioIntegral = 1,
    NuProprietarioIntegral = 2,
    Repartido = 3,
}

public enum QuorumEleicaoAdmin
{
    MaioriaSimples = 0,
    MaioriaQualificada = 1,
    Unanimidade = 2,
}

public enum ModoArquivamento
{
    NaoArquivar = 0,
    ArquivarExtrato = 1,
    ArquivarIntegral = 2,
    ArquivarClausulaCiencia = 3,
}

public enum ModoControversia
{
    Foro = 0,
    Arbitragem = 1,
    MediacaoMaisForo = 2,
    Escalonada = 3,
}

// ── Sub-profiles ─────────────────────────────────────────────

public sealed class AcordoIdentificacao
{
    public List<string> Partes { get; set; } = new();
}

public sealed class DireitosPoliticosAcordo
{
    public bool EstaDefinido { get; set; }
    public ModoVotoUsufruto ModoVoto { get; set; } = ModoVotoUsufruto.SemUsufruto;
    public string? VotoMateriasOrdinarias { get; set; }
    public string? VotoMateriasExtraordinarias { get; set; }
}

public sealed class DireitosEconomicosAcordo
{
    public ModoEconomicoUsufruto ModoEconomico { get; set; } = ModoEconomicoUsufruto.NaoDefinido;
    public string? FormulaReparticao { get; set; }
    public bool ExcluiQuotistaDeResultados { get; set; }
}

public sealed class AdministracaoAcordo
{
    public QuorumEleicaoAdmin QuorumEleicao { get; set; } = QuorumEleicaoAdmin.MaioriaSimples;
    public List<string> MateriasUnanimidade { get; set; } = new();
    public List<string> MateriasMaioriaQualificada { get; set; } = new();
    public List<string> MateriasVedadasADonatarios { get; set; } = new();
}

public sealed class CirculacaoAcordo
{
    public bool VedacaoCessaoTerceiros { get; set; } = true;
    public bool EntradaConjugePermitida { get; set; }
}

public sealed class MetodoValuation
{
    public string? FormulaContratual { get; set; }
}

public sealed class SaidaAcordo
{
    public bool PermiteSaidaVoluntaria { get; set; }
    public int PrazoNotificacaoDias { get; set; } = 60;
    public MetodoValuation Valuation { get; set; } = new();
    public int? ParcelasPagamento { get; set; }
}

public sealed class SancoesAcordo
{
    public decimal? ValorMulta { get; set; }
}

public sealed class ArquivamentoAcordo
{
    public ModoArquivamento Modo { get; set; } = ModoArquivamento.NaoArquivar;
}

public sealed class ControversiasAcordo
{
    public ModoControversia Modo { get; set; } = ModoControversia.Foro;
    public string? CamaraArbitral { get; set; }
    public int PrazoMediacaoDias { get; set; } = 30;
    public string? Comarca { get; set; }
    public string? UfForo { get; set; }
}

// ── Profile principal ────────────────────────────────────────

public sealed class AcordoQuotistasProfile
{
    public AcordoIdentificacao Identificacao { get; set; } = new();
    public bool HasUsufruct { get; set; }
    public DireitosPoliticosAcordo DireitosPoliticos { get; set; } = new();
    public DireitosEconomicosAcordo DireitosEconomicos { get; set; } = new();
    public AdministracaoAcordo Administracao { get; set; } = new();
    public CirculacaoAcordo Circulacao { get; set; } = new();
    public SaidaAcordo Saida { get; set; } = new();
    public SancoesAcordo Sancoes { get; set; } = new();
    public ArquivamentoAcordo Arquivamento { get; set; } = new();
    public ControversiasAcordo Controversias { get; set; } = new();

    /// <summary>
    /// Módulos obrigatórios resolvidos pela engine.
    /// </summary>
    public List<string> MandatoryModules { get; set; } = new();

    /// <summary>
    /// Resolve módulos obrigatórios com base nos dados preenchidos.
    /// </summary>
    public void ResolveModulosObrigatorios()
    {
        MandatoryModules.Clear();
        MandatoryModules.Add("MOD-IDENT");      // Identificação sempre obrigatório
        MandatoryModules.Add("MOD-CIRCULACAO");  // Circulação sempre obrigatório

        if (HasUsufruct)
        {
            MandatoryModules.Add("MOD-POLITICOS");  // Direitos políticos do usufrutuário
            MandatoryModules.Add("MOD-ECONOMICOS"); // Direitos econômicos
        }

        MandatoryModules.Add("MOD-ADMIN");       // Administração
        MandatoryModules.Add("MOD-SAIDA");       // Saída e valuation
        MandatoryModules.Add("MOD-CONTROVERSIAS"); // Resolução de conflitos
    }
}
