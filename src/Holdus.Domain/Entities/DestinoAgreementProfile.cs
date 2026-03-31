#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// ACORDO DE QUOTISTAS DA DESTINO — Modelo de Domínio
// Versão definitiva — Thiago Seixas
//
// "O acordo deixa de ser texto solto e passa a ser
//  objeto jurídico parametrizado."
// ═══════════════════════════════════════════════════════════════

// ── ENUMS ────────────────────────────────────────────────────

public enum AgreementStatus
{
    Draft = 0,
    EmValidacao = 1,
    Bloqueado = 2,
    AptoParaGeracao = 3,
    Gerado = 4
}

public enum AgreementPartyRole
{
    Quotista = 0,
    NuProprietario = 1,
    Usufrutuario = 2,
    Aderente = 3,
    AdministradorAderente = 4
}

public enum GiftSuccessionMode
{
    NaoAplicavel = 0,
    AdiantamentoLegitima = 1,
    ParteDisponivelComDispensaColacao = 2
}

public enum PoliticalRightsMode
{
    NaoDefinido = 0,
    Quotista = 1,
    Usufrutuario = 2,
    NuProprietario = 3,
    RegimeMisto = 4,
    AnuenciaConjuntaExtraordinarias = 5
}

public enum EconomicRightsMode
{
    NaoDefinido = 0,
    Quotista = 1,
    Usufrutuario = 2,
    NuProprietario = 3,
    RateioParametrizado = 4
}

public enum TransferRestrictionMode
{
    NaoDefinido = 0,
    VedacaoTotal = 1,
    PreferenciaInterna = 2,
    ApenasIntraFamilia = 3,
    AprovacaoQualificada = 4
}

public enum ReservedMatterQuorumMode
{
    NaoDefinido = 0,
    MaioriaSimples = 1,
    MaioriaQualificada = 2,
    Unanimidade = 3
}

public enum FamilyEventType
{
    FalecimentoQuotista = 0,
    FalecimentoUsufrutuario = 1,
    FalecimentoAdministrador = 2,
    Incapacidade = 3,
    Divorcio = 4,
    DissolucaoUniaoEstavel = 5,
    Penhora = 6,
    Insolvencia = 7,
    RenunciaUsufruto = 8
}

public enum RestrictiveClauseType
{
    Incomunicabilidade = 0,
    Impenhorabilidade = 1,
    Inalienabilidade = 2,
    Reversao = 3
}

public enum ValuationMode
{
    NaoDefinido = 0,
    BalancoDeterminacao = 1,
    ValorPatrimonial = 2,
    FormulaContratual = 3
}

public enum ArchivingMode
{
    NaoDefinido = 0,
    NaoArquivar = 1,
    ArquivarExtrato = 2,
    ArquivarCiencia = 3,
    ArquivarIntegral = 4
}

public enum DisputeResolutionMode
{
    NaoDefinido = 0,
    Foro = 1,
    MediacaoEForo = 2,
    Arbitragem = 3,
    MediacaoEArbitragem = 4
}

// ── PARTES ───────────────────────────────────────────────────

public sealed class DestinoAgreementParty
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public AgreementPartyRole Papel { get; set; } = AgreementPartyRole.Quotista;
    public bool Signatario { get; set; } = true;
    public DateTime? DataAdesao { get; set; }
    public bool QuotistaAtual { get; set; }
    public bool IngressaPorDoacao { get; set; }
    public bool DescendenteDoDoador { get; set; }
}

// ── MATÉRIAS RESERVADAS ──────────────────────────────────────

public sealed class DestinoReservedMatter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Descricao { get; set; } = string.Empty;
    public ReservedMatterQuorumMode Quorum { get; set; } = ReservedMatterQuorumMode.NaoDefinido;
    public bool Obrigatoria { get; set; }
}

// ── EVENTOS FAMILIARES ───────────────────────────────────────

public sealed class DestinoFamilyEventRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public FamilyEventType Evento { get; set; }
    public string ConsequenciaJuridica { get; set; } = string.Empty;
    public string? Prazo { get; set; }
    public string? ResponsavelDeliberacao { get; set; }
}

// ── CLÁUSULAS RESTRITIVAS ────────────────────────────────────

public sealed class DestinoRestrictiveClause
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public RestrictiveClauseType Tipo { get; set; }
    public string Alcance { get; set; } = string.Empty;
    public string? Prazo { get; set; }
    public string? BeneficiarioReversao { get; set; }
}

// ── REGRAS DE SAÍDA ──────────────────────────────────────────

public sealed class DestinoExitRule
{
    public ValuationMode ValuationMode { get; set; } = ValuationMode.NaoDefinido;
    public bool DireitoPreferenciaRemanescentes { get; set; } = true;
    public bool RecompraPelaSociedade { get; set; }
    public int? ParcelasPagamento { get; set; }
    public bool DescontoIliquidez { get; set; }
    public decimal? PercentualDescontoIliquidez { get; set; }
}

// ── REGRAS DE SANÇÃO ─────────────────────────────────────────

public sealed class DestinoPenaltyRule
{
    public decimal? MultaValor { get; set; }
    public bool PerdasEDanos { get; set; }
    public bool TutelaEspecifica { get; set; }
    public bool ObrigacaoFazerNaoFazer { get; set; }
    public bool NotificacaoPrevia { get; set; }
}

// ── CONFIDENCIALIDADE ────────────────────────────────────────

public sealed class DestinoConfidentialityRule
{
    public bool Ativa { get; set; } = true;
    public string Escopo { get; set; } = "Informações patrimoniais, societárias, sucessórias e familiares ligadas à estrutura.";
    public bool ExcecoesLegais { get; set; } = true;
    public bool DeverAssinarAtosCorrelatos { get; set; } = true;
    public bool DeverComparecimentoJunta { get; set; } = true;
}

// ── ARQUIVAMENTO ─────────────────────────────────────────────

public sealed class DestinoArchivingRule
{
    public ArchivingMode Mode { get; set; } = ArchivingMode.NaoDefinido;
    public bool RegulaUsufrutoEmInstrumentoParassocial { get; set; }
    public bool ExigeEficaciaPeranteTerceiros { get; set; } = true;
}

// ── CONTROVÉRSIAS ────────────────────────────────────────────

public sealed class DestinoDisputeRule
{
    public DisputeResolutionMode Mode { get; set; } = DisputeResolutionMode.NaoDefinido;
    public string? ForoComarca { get; set; }
    public string? CamaraArbitral { get; set; }
    public bool MediacaoPrevia { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// ENTIDADE PRINCIPAL — DestinoAgreementProfile
// ═══════════════════════════════════════════════════════════════

public sealed class DestinoAgreementProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DestinoId { get; set; }
    public string NomeSociedade { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? Nire { get; set; }

    public AgreementStatus Status { get; set; } = AgreementStatus.Draft;

    // ── Doação ───────────────────────────────────────────
    public bool HaDoacao { get; set; }
    public GiftSuccessionMode GiftSuccessionMode { get; set; } = GiftSuccessionMode.NaoAplicavel;

    // ── Usufruto ─────────────────────────────────────────
    public bool HaUsufruto { get; set; }
    public PoliticalRightsMode PoliticalRightsMode { get; set; } = PoliticalRightsMode.NaoDefinido;
    public EconomicRightsMode EconomicRightsMode { get; set; } = EconomicRightsMode.NaoDefinido;

    // ── Circulação ───────────────────────────────────────
    public TransferRestrictionMode TransferRestrictionMode { get; set; } = TransferRestrictionMode.NaoDefinido;

    // ── Módulos compostos ────────────────────────────────
    public List<DestinoAgreementParty> Parties { get; set; } = new();
    public List<DestinoReservedMatter> ReservedMatters { get; set; } = new();
    public List<DestinoFamilyEventRule> FamilyEventRules { get; set; } = new();
    public List<DestinoRestrictiveClause> RestrictiveClauses { get; set; } = new();

    // ── Regras ───────────────────────────────────────────
    public DestinoExitRule ExitRule { get; set; } = new();
    public DestinoPenaltyRule PenaltyRule { get; set; } = new();
    public DestinoConfidentialityRule ConfidentialityRule { get; set; } = new();
    public DestinoArchivingRule ArchivingRule { get; set; } = new();
    public DestinoDisputeRule DisputeRule { get; set; } = new();

    // ── Controle ─────────────────────────────────────────
    public List<string> MandatoryModules { get; set; } = new();
    public List<string> OptionalModules { get; set; } = new();
    public List<string> BlockedBy { get; set; } = new();

    // ── Derivados ────────────────────────────────────────
    public bool HasGiftToDescendant =>
        Parties.Any(x => x.IngressaPorDoacao && x.DescendenteDoDoador);

    public bool HasSignatories =>
        Parties.Any(x => x.Signatario);
}
