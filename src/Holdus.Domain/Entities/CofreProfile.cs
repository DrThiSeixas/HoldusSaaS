#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// COFRE PROFILE — Modelo de Domínio da Célula Cofre
// Versão definitiva — Thiago Seixas
// ═══════════════════════════════════════════════════════════════

public enum CellStatus
{
    Draft = 0,
    EmValidacao = 1,
    PendenciaDocumental = 2,
    Bloqueada = 3,
    AptaParaGeracao = 4,
    DocumentosGerados = 5,
    Finalizada = 6
}

public enum CompanyType
{
    Ltda = 0,
    LtdaUnipessoal = 1
}

public enum ContributionType
{
    Nenhuma = 0,
    Dinheiro = 1,
    ImovelUrbano = 2,
    ImovelRural = 3,
    ParticipacoesSocietarias = 4,
    Mista = 5
}

public enum MaritalStatus
{
    NaoInformado = 0,
    Solteiro = 1,
    Casado = 2,
    Divorciado = 3,
    Viuvo = 4,
    UniaoEstavel = 5
}

public enum MaritalRegime
{
    NaoInformado = 0,
    ComunhaoParcial = 1,
    ComunhaoUniversal = 2,
    SeparacaoConvencional = 3,
    SeparacaoObrigatoria = 4,
    ParticipacaoFinalNosAquestos = 5
}

public enum AdministratorType
{
    Socio = 0,
    NaoSocio = 1
}

public enum ParticipationAssetType
{
    Quotas = 0,
    Acoes = 1
}

public enum ReflexActType
{
    AlteracaoContratual = 0,
    AverbacaoLivroSocietario = 1,
    Outro = 2
}

public enum ValidationSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public sealed class Address
{
    public string Logradouro { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string? Complemento { get; set; }
    public string Bairro { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string Cep { get; set; } = string.Empty;
}

public sealed class PartnerProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string? Rg { get; set; }
    public string Nacionalidade { get; set; } = "Brasileira";
    public string Profissao { get; set; } = string.Empty;

    public MaritalStatus EstadoCivil { get; set; } = MaritalStatus.NaoInformado;
    public bool UniaoEstavel { get; set; }
    public MaritalRegime RegimeBens { get; set; } = MaritalRegime.NaoInformado;
    public bool HaPactoOuContratoConvivencia { get; set; }

    public bool IsSpouseOrPartnerOfAnotherPartnerInThisCompany { get; set; }
    public bool RequiresSpousalConsent { get; set; }
    public bool SpousalConsentOk { get; set; }

    public decimal ParticipacaoPercentual { get; set; }
    public int QuantidadeQuotas { get; set; }
    public decimal ValorQuotas { get; set; }

    public bool IntegralizaNesteAto { get; set; }
    public ContributionType TipoAporte { get; set; } = ContributionType.Dinheiro;
}

public sealed class AdministratorProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public AdministratorType Tipo { get; set; } = AdministratorType.Socio;
    public string PrazoMandato { get; set; } = "indeterminado";
    public bool PoderesIsolados { get; set; } = true;
    public List<string> Restricoes { get; set; } = new();
    public bool DeclaracaoDesimpedimento { get; set; } = true;
}

public sealed class CapitalContributionInfo
{
    public ContributionType Modo { get; set; } = ContributionType.Nenhuma;
    public decimal ValorTotalAporte { get; set; }
    public decimal ValorDestinadoCapital { get; set; }
    public bool HaParcelaForaCapital { get; set; }
    public string? DescricaoReservaOuExcedente { get; set; }
}

public sealed class PropertyContribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ContributionType Tipo { get; set; } = ContributionType.ImovelUrbano;
    public string Matricula { get; set; } = string.Empty;
    public string Cartorio { get; set; } = string.Empty;
    public string Municipio { get; set; } = string.Empty;
    public string Uf { get; set; } = string.Empty;
    public string EnderecoOuDenominacao { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string? CadastroMunicipal { get; set; }
    public string? Ccir { get; set; }
    public string? NirfCafir { get; set; }
    public string TituloAquisitivo { get; set; } = string.Empty;
    public decimal ValorAtribuidoNoAto { get; set; }
    public decimal ValorIntegralizadoEmCapital { get; set; }
    public string ProprietarioConferente { get; set; } = string.Empty;
    public bool DocumentacaoOk { get; set; }
}

public sealed class EquityContribution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SociedadeInvestidaNome { get; set; } = string.Empty;
    public string SociedadeInvestidaCnpj { get; set; } = string.Empty;
    public string? SociedadeInvestidaNire { get; set; }
    public ParticipationAssetType TipoParticipacao { get; set; } = ParticipationAssetType.Quotas;
    public int Quantidade { get; set; }
    public decimal Percentual { get; set; }
    public decimal ValorAtribuido { get; set; }
    public bool CapitalDaInvestidaTotalmenteIntegralizado { get; set; }
    public bool AporteTotal { get; set; }
    public bool RestricaoContratualTransferencia { get; set; }
    public bool DocumentacaoOk { get; set; }
}

public sealed class ReflexActInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SociedadeAfetada { get; set; } = string.Empty;
    public ReflexActType TipoAto { get; set; } = ReflexActType.AlteracaoContratual;
    public bool UsoTotalDaParticipacao { get; set; }
    public bool MesmaUfDaReceptora { get; set; }
    public bool TramitacaoConjuntaExigida { get; set; }
    public bool Gerado { get; set; }
}

public sealed class GeneratedDocument
{
    public string CodigoDocumento { get; set; } = string.Empty;
    public string NomeDocumento { get; set; } = string.Empty;
    public bool Gerado { get; set; }
    public DateTime? DataGeracao { get; set; }
    public string Versao { get; set; } = "1.0";
}

public sealed class CofreProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjetoId { get; set; }
    public Guid ClienteId { get; set; }

    public CellStatus StatusCelula { get; set; } = CellStatus.Draft;

    public string NomeEmpresarial { get; set; } = string.Empty;
    public CompanyType TipoSociedade { get; set; } = CompanyType.Ltda;
    public string NaturezaCelula { get; set; } = "COFRE";
    public bool CofrePuro { get; set; } = true;

    public string? Cnpj { get; set; }
    public string? Nire { get; set; }
    public DateTime? DataConstituicao { get; set; }
    public string UfJunta { get; set; } = string.Empty;

    public Address EnderecoSede { get; set; } = new();
    public string PrazoDuracao { get; set; } = "indeterminado";

    public bool ObjetoSocialPadrao { get; set; } = true;
    public string ObjetoSocialTexto { get; set; } = string.Empty;
    public string CnaePrincipal { get; set; } = "6462-0/00";
    public List<string> CnaesSecundarios { get; set; } = new();

    public bool AtividadeOperacionalPropria { get; set; }
    public bool LocacaoImoveisProprios { get; set; }
    public bool CompraVendaImoveis { get; set; }
    public bool PrestacaoServicos { get; set; }
    public bool AdministraTerceiros { get; set; }

    public List<PartnerProfile> Socios { get; set; } = new();
    public List<AdministratorProfile> Administradores { get; set; } = new();

    public decimal CapitalSocialAtual { get; set; }
    public decimal CapitalSocialPosAto { get; set; }
    public ContributionType TipoIntegralizacao { get; set; } = ContributionType.Nenhuma;
    public CapitalContributionInfo Integralizacao { get; set; } = new();

    public List<PropertyContribution> ImoveisAportados { get; set; } = new();
    public List<EquityContribution> ParticipacoesAportadas { get; set; } = new();

    public bool HaAtoReflexo { get; set; }
    public List<ReflexActInfo> AtosReflexos { get; set; } = new();

    public bool HaRiscoTema796 { get; set; }
    public bool HaRiscoAtividadePreponderante { get; set; }
    public bool HaBloqueioRegimeBens { get; set; }
    public bool HaAnuenciaConjugalPendente { get; set; }

    public bool ChecklistRegistralOk { get; set; }
    public bool ChecklistTributarioOk { get; set; }

    public string? ObservacoesInternas { get; set; }
    public List<GeneratedDocument> DocumentosGerados { get; set; } = new();

    // ── Derivados ────────────────────────────────────────
    public bool IsAlteracao =>
        !string.IsNullOrWhiteSpace(Cnpj) ||
        !string.IsNullOrWhiteSpace(Nire);

    public bool TemIntegralizacaoImovel =>
        TipoIntegralizacao == ContributionType.ImovelUrbano ||
        TipoIntegralizacao == ContributionType.ImovelRural ||
        TipoIntegralizacao == ContributionType.Mista;

    public bool TemIntegralizacaoParticipacoes =>
        TipoIntegralizacao == ContributionType.ParticipacoesSocietarias ||
        TipoIntegralizacao == ContributionType.Mista;
}
