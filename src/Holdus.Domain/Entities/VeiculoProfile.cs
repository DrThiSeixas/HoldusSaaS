#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// CÉLULA VEÍCULO — Modelo de Domínio
//
// "A Veículo segura o comando."
//
// Sociedade limitada de regência supletiva pela Lei das S.A.,
// com classes de quotas diferenciadas, destinada a concentrar
// o controle político e intermediar a reorganização da
// titularidade econômica entre Cofre e Destino.
// ═══════════════════════════════════════════════════════════════

// ── ENUMS ────────────────────────────────────────────────────

public enum VeiculoStatus
{
    Draft = 0,
    EmValidacao = 1,
    Bloqueado = 2,
    AptoParaGeracao = 3,
    DocumentosGerados = 4,
    Finalizado = 5
}

public enum ClasseQuota
{
    Ordinaria = 0,
    Preferencial = 1
}

public enum CallEventoGatilho
{
    NaoDefinido = 0,
    Falecimento = 1,
    Incapacidade = 2,
    Saida = 3,
    RenunciaControle = 4,
    Personalizado = 5
}

/// <summary>
/// 3 opções de voto das ordinárias.
/// DREI admite limitação ou supressão de voto.
/// </summary>
public enum VotoOrdinariaMode
{
    NaoDefinido = 0,
    VotoSimplesResidual = 1,       // Opção A: coerência mínima
    VotoLimitadoOrdinarias = 2,    // Opção B: sem poder sobre estruturais
    SemVotoEmControle = 3          // Opção C: voto concentrado na preferencial
}

// ── ENTIDADE PRINCIPAL ───────────────────────────────────────

public sealed class VeiculoProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjetoId { get; set; }

    public VeiculoStatus Status { get; set; } = VeiculoStatus.Draft;

    // ── A. Identificação ─────────────────────────────────
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? Nire { get; set; }
    public string UfJunta { get; set; } = string.Empty;
    public Address EnderecoSede { get; set; } = new();
    public string PrazoDuracao { get; set; } = "indeterminado";

    // ── B. Regência supletiva (Pilar 1) ──────────────────
    /// <summary>
    /// OBRIGATÓRIO. Sem regência supletiva expressa, a Veículo não funciona.
    /// Art. 1.053, parágrafo único, CC.
    /// </summary>
    public bool RegenciaSupletivaExpressa { get; set; } = true;

    // ── C. Objeto social ─────────────────────────────────
    public string ObjetoSocialTexto { get; set; } = string.Empty;
    public string CnaePrincipal { get; set; } = "6462-0/00";

    // ── D. Capital social ────────────────────────────────
    public decimal CapitalSocial { get; set; }
    public decimal ValorDestinoReferencia { get; set; }
    public decimal ValorPreferencialAdicional { get; set; } = 1_000.00m;
    public decimal CapitalCalculado => ValorDestinoReferencia + ValorPreferencialAdicional;

    // ── E. Classes de quotas (Pilar 2) ───────────────────
    public List<ClasseQuotaVeiculo> ClassesQuotas { get; set; } = new();

    /// <summary>
    /// Modo de voto das ordinárias: residual, limitado, ou sem voto em controle.
    /// DREI admite limitação ou supressão de voto.
    /// </summary>
    public VotoOrdinariaMode VotoOrdinarias { get; set; } = VotoOrdinariaMode.NaoDefinido;

    public int TotalQuotasOrdinarias => ClassesQuotas
        .Where(c => c.Classe == ClasseQuota.Ordinaria)
        .Sum(c => c.Quantidade);

    public int TotalQuotasPreferenciais => ClassesQuotas
        .Where(c => c.Classe == ClasseQuota.Preferencial)
        .Sum(c => c.Quantidade);

    // ── F. Sócios ────────────────────────────────────────
    public List<SocioVeiculo> Socios { get; set; } = new();

    // ── G. Administração ─────────────────────────────────
    public string? AdministradorNome { get; set; }
    public bool AdministradorEhSocio { get; set; } = true;

    // ── H. Reserva de capital (Pilar 3) ──────────────────
    /// <summary>
    /// Excedente sobre valor nominal → reserva de capital.
    /// Art. 13, §2º, Lei 6.404/76. Só coerente com regência supletiva.
    /// </summary>
    public bool TemReservaCapital { get; set; }
    public decimal ValorReservaCapital { get; set; }
    public string? DescricaoReserva { get; set; }

    // ── I. Cláusula de call (Pilar 4) ────────────────────
    public bool TemClausulaCall { get; set; }
    public ClausulaCallVeiculo? ClausulaCall { get; set; }

    // ── J. Atos reflexos (Pilar 5) ───────────────────────
    /// <summary>
    /// A Veículo não funciona sozinha.
    /// Exige: alteração na Cofre + compra de ordinárias pela Destino.
    /// </summary>
    public bool AtoReflexoCofre { get; set; }
    public bool CompraOrdinariasPelaDestino { get; set; }
    public bool AjusteDestinoRecomendado { get; set; }

    /// <summary>
    /// Detalhes da compra de ordinárias pela Destino.
    /// Fluxograma: compra pelo valor nominal.
    /// </summary>
    public CompraOrdinariasPelaDestinoInfo? CompraOrdinariosInfo { get; set; }

    // ── K. Matérias reservadas à preferencial ────────────
    /// <summary>
    /// 7 matérias que exigem aprovação do detentor da preferencial.
    /// A preferencial é a peça de comando — essas matérias não podem
    /// ser deliberadas sem ela.
    /// </summary>
    public List<string> MateriasReservadasPreferencial { get; set; } = new()
    {
        "Alteração da finalidade da Veículo",
        "Alteração da regência supletiva",
        "Alteração do regime de classes de quotas",
        "Cessão, oneração ou alienação da participação da Veículo na Cofre",
        "Alteração da cláusula de call",
        "Dissolução, transformação, incorporação, fusão ou cisão",
        "Qualquer ato que esvazie a função de comando da Veículo"
    };

    // ── L. Referências cruzadas ──────────────────────────
    public Guid? CofreId { get; set; }
    public Guid? DestinoId { get; set; }

    // ── Flags de risco ───────────────────────────────────
    public List<string> BlockedBy { get; set; } = new();

    // ── Derivados ────────────────────────────────────────
    public bool IsAlteracao =>
        !string.IsNullOrWhiteSpace(Cnpj) ||
        !string.IsNullOrWhiteSpace(Nire);

    public bool TemPreferenciais => ClassesQuotas.Any(c => c.Classe == ClasseQuota.Preferencial);

    public bool PilaresCompletos =>
        RegenciaSupletivaExpressa &&
        TemPreferenciais &&
        MateriasReservadasPreferencial.Count > 0 &&
        (!TemReservaCapital || ValorReservaCapital > 0) &&
        (!TemClausulaCall || ClausulaCall?.EventoGatilho != CallEventoGatilho.NaoDefinido) &&
        AtoReflexoCofre;
}

// ── CLASSE DE QUOTA ──────────────────────────────────────────

public sealed class ClasseQuotaVeiculo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ClasseQuota Classe { get; set; } = ClasseQuota.Ordinaria;
    public string Descricao { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal ValorNominalUnitario { get; set; } = 1.00m;
    public decimal ValorTotal => Quantidade * ValorNominalUnitario;

    // ── Direitos políticos ───────────────────────────────
    public int PesoVoto { get; set; } = 1;
    public bool VotoEmMateriasOrdinarias { get; set; } = true;
    public bool VotoEmMateriasExtraordinarias { get; set; } = true;

    // ── Direitos econômicos ──────────────────────────────
    public bool ParticipaNosLucros { get; set; } = true;
    public decimal? PercentualLucrosPreferencial { get; set; }
    public bool PrioridadeNaDistribuicao { get; set; }

    // ── Vinculação ───────────────────────────────────────
    /// <summary>
    /// Se preferencial: vinculada aos donos do Cofre.
    /// Se ordinária: espelha distribuição da Destino.
    /// </summary>
    public string? VinculacaoDescricao { get; set; }
}

// ── SÓCIO ────────────────────────────────────────────────────

public sealed class SocioVeiculo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;

    public int QuotasOrdinarias { get; set; }
    public int QuotasPreferenciais { get; set; }
    public int TotalQuotas => QuotasOrdinarias + QuotasPreferenciais;

    public decimal ParticipacaoPercentual { get; set; }

    /// <summary>
    /// Detentor do Cofre = tem preferencial = detém o comando.
    /// </summary>
    public bool EhDetentorCofre { get; set; }
}

// ── CLÁUSULA DE CALL ─────────────────────────────────────────

public sealed class ClausulaCallVeiculo
{
    public CallEventoGatilho EventoGatilho { get; set; } = CallEventoGatilho.NaoDefinido;
    public string? DescricaoEventoPersonalizado { get; set; }

    /// <summary>
    /// Quem compra: herdeiros (padrão do fluxograma).
    /// </summary>
    public string CompradoresDescritos { get; set; } = "herdeiros do titular falecido";

    /// <summary>
    /// Referência de preço: valor nominal, patrimonial, fórmula.
    /// </summary>
    public string MetodoPreco { get; set; } = "valor nominal";

    public int? PrazoExercicioDias { get; set; }
    public string? CondicoesPagamento { get; set; }
}

// ── COMPRA DE ORDINÁRIAS PELA DESTINO ────────────────────────

public sealed class CompraOrdinariasPelaDestinoInfo
{
    /// <summary>
    /// Fluxograma: compra pelo valor nominal.
    /// </summary>
    public string MetodoPreco { get; set; } = "valor nominal";
    public decimal? ValorTotal { get; set; }
    public string FormaPagamento { get; set; } = "à vista";
    public int? ParcelasPagamento { get; set; }

    /// <summary>
    /// Verifica coerência: preço deve ser compatível com o fluxo documental.
    /// </summary>
    public bool PrecoCoerente => !string.IsNullOrWhiteSpace(MetodoPreco);
}
