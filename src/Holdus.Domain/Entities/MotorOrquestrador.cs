namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// MOTOR DOCUMENTAL — Orquestrador
// "Primeiro define qual célula. Depois qual ato.
//  Depois valida riscos e travas. Só então monta a minuta."
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Código interno de identificação: CELULA.TIPOATO.VERSAO
/// Ex: COFRE.CONSTITUICAO.V1, COFRE.ALT_CAPITAL_IMOVEL.V1
/// </summary>
public class CodigoDocumento
{
    public CelulaDocumental Celula { get; set; }
    public TipoAtoDocumental TipoAto { get; set; }
    public int Versao { get; set; } = 1;

    public string Codigo => $"{Celula}.{TipoAto}.V{Versao}";

    public override string ToString() => Codigo;
}

// ═══════════════════════════════════════════════════════════════
// ENUMS DO ORQUESTRADOR
// ═══════════════════════════════════════════════════════════════

public enum CelulaDocumental
{
    COFRE,
    VEICULO,
    DESTINO
}

/// <summary>
/// Tipos de ato por célula.
/// COFRE: 6 módulos implementados.
/// VEICULO/DESTINO: futuros.
/// </summary>
public enum TipoAtoDocumental
{
    // ── COFRE ────────────────────────────────────────────
    CONSTITUICAO,               // COFRE.CONSTITUICAO.V1
    ALT_CAPITAL_IMOVEL,         // COFRE.ALT_CAPITAL_IMOVEL.V1
    ALT_CAPITAL_PARTICIPACOES,  // COFRE.ALT_CAPITAL_PARTICIPACOES.V1
    ATO_REFLEXO_INVESTIDA,      // COFRE.ATO_REFLEXO_INVESTIDA.V1
    CHECKLIST_REGISTRO,         // COFRE.CHECKLIST_REGISTRO.V1
    NOTA_ITBI,                  // COFRE.NOTA_ITBI.V1
    ALT_TITULARIDADE_VEICULO,   // COFRE.ALT_TITULARIDADE_VEICULO.V1

    // ── VEÍCULO (futuro) ────────────────────────────────
    CONSTITUICAO_VEICULO,
    MODULO_OPERACIONAL,
    ALT_COMPRA_ORDINARIAS,

    // ── DESTINO (futuro) ────────────────────────────────
    CONSTITUICAO_DESTINO,
    MODULO_PATRIMONIAL,
    DOACAO_QUOTAS,
    ALT_DOACAO,
    ACORDO_QUOTISTAS,
}

// ═══════════════════════════════════════════════════════════════
// TAGS-MÃE — 23 tags globais do motor
// O motor gera documento por tags, não por modelo fechado.
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Estado completo de tags para decisão do motor.
/// Todas as 23 tags-mãe num único objeto.
/// </summary>
public class TagsMotorDocumental
{
    // ── Célula e ato ─────────────────────────────────────
    public CelulaDocumental Celula { get; set; }
    public TipoAtoDocumental TipoAto { get; set; }

    // ── Sociedade ────────────────────────────────────────
    public TipoSociedadeLtda TipoSociedade { get; set; }
    public bool SocioUnico => TipoSociedade == TipoSociedadeLtda.Unipessoal;

    // ── Família / cônjuge ────────────────────────────────
    public bool TemCasadoOuUniaoEstavel { get; set; }
    public bool UniaoEstavel { get; set; }
    public bool HaAnuenciaConjugal { get; set; }

    // ── Integralização ───────────────────────────────────
    public TipoIntegralizacaoTag TipoIntegralizacao { get; set; }
    public bool IntegralizacaoImovel => TipoIntegralizacao == TipoIntegralizacaoTag.Imovel;
    public bool IntegralizacaoParticipacoes => TipoIntegralizacao == TipoIntegralizacaoTag.Participacoes;
    public bool ImovelUrbano { get; set; }
    public bool ImovelRural { get; set; }
    public bool ParticipacaoTotal { get; set; }
    public bool ParticipacaoParcial => !ParticipacaoTotal && IntegralizacaoParticipacoes;

    // ── Cofre ────────────────────────────────────────────
    public bool CofrePuro { get; set; } = true;
    public bool AtividadeOperacionalPropria { get; set; }

    // ── Riscos ───────────────────────────────────────────
    public bool HaRiscoTema796 { get; set; }
    public bool HaAtoReflexo { get; set; }

    // ── Tramitação ───────────────────────────────────────
    public bool MesmaUf { get; set; }
    public bool ExigeTramitacaoConjunta => MesmaUf && HaAtoReflexo;
}

public enum TipoIntegralizacaoTag
{
    Dinheiro,
    Imovel,
    Participacoes,
    BemMovel,
    Misto
}

// ═══════════════════════════════════════════════════════════════
// TRAVAS INTERNAS — Textos do painel do sistema
// ═══════════════════════════════════════════════════════════════

public static class TravasMotor
{
    public const string TravaCofre =
        "Esta célula é patrimonial-societária pura. Não deve ser utilizada para atividade operacional própria.";

    public const string TravaItbi =
        "Há risco jurídico-tributário se o valor do bem não ingressar efetivamente como capital social.";

    public const string TravaParticipacoes =
        "A integralização com quotas ou ações exige compatibilidade com a estrutura da sociedade investida e ato reflexo correspondente.";

    public const string TravaConjugal =
        "O regime de bens dos sócios pode gerar vedação legal para esta composição societária (art. 977 CC).";

    public const string TravaDescricaoImovel =
        "O DREI exige descrição registral completa do imóvel no ato de constituição ou alteração contratual.";
}

// ═══════════════════════════════════════════════════════════════
// BIBLIOTECA POR CÉLULA
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Catálogo de documentos disponíveis por célula.
/// Cada célula tem sua própria biblioteca — nada de biblioteca única.
/// </summary>
public static class BibliotecaCelular
{
    public static readonly Dictionary<CelulaDocumental, List<ModuloDocumental>> Catalogo = new()
    {
        [CelulaDocumental.COFRE] =
        [
            new("COFRE.CONSTITUICAO.V1", "Contrato Social Master", TipoAtoDocumental.CONSTITUICAO, true),
            new("COFRE.ALT_CAPITAL_IMOVEL.V1", "Alteração Contratual — Aumento de Capital por Imóvel", TipoAtoDocumental.ALT_CAPITAL_IMOVEL, true),
            new("COFRE.ALT_CAPITAL_PARTICIPACOES.V1", "Alteração Contratual — Aumento de Capital por Participações", TipoAtoDocumental.ALT_CAPITAL_PARTICIPACOES, true),
            new("COFRE.ATO_REFLEXO_INVESTIDA.V1", "Ato Reflexo — Sociedade Investida", TipoAtoDocumental.ATO_REFLEXO_INVESTIDA, true),
            new("COFRE.CHECKLIST_REGISTRO.V1", "Checklist Registral", TipoAtoDocumental.CHECKLIST_REGISTRO, true),
            new("COFRE.NOTA_ITBI.V1", "Nota Técnica Interna — ITBI", TipoAtoDocumental.NOTA_ITBI, false),
            new("COFRE.ALT_TITULARIDADE_VEICULO.V1", "Alteração Contratual — Transferência à Veículo", TipoAtoDocumental.ALT_TITULARIDADE_VEICULO, true),
        ],
        [CelulaDocumental.VEICULO] =
        [
            new("VEICULO.CONSTITUICAO.V1", "Contrato Social Master — Veículo", TipoAtoDocumental.CONSTITUICAO_VEICULO, true),
            new("VEICULO.ALT_COMPRA_ORDINARIAS.V1", "Alteração — Compra Ordinárias pela Destino", TipoAtoDocumental.ALT_COMPRA_ORDINARIAS, true),
            new("VEICULO.MODULO_OPERACIONAL.V1", "Módulo de Operação/Gestão", TipoAtoDocumental.MODULO_OPERACIONAL, false),
        ],
        [CelulaDocumental.DESTINO] =
        [
            new("DESTINO.CONSTITUICAO.V1", "Contrato Social Master — Destino", TipoAtoDocumental.CONSTITUICAO_DESTINO, true),
            new("DESTINO.DOACAO_QUOTAS.V1", "Instrumento de Doação de Quotas", TipoAtoDocumental.DOACAO_QUOTAS, true),
            new("DESTINO.ALT_DOACAO.V1", "Alteração Contratual — Reflexo da Doação", TipoAtoDocumental.ALT_DOACAO, true),
            new("DESTINO.ACORDO_QUOTISTAS.V1", "Acordo de Quotistas", TipoAtoDocumental.ACORDO_QUOTISTAS, true),
            new("DESTINO.MODULO_PATRIMONIAL.V1", "Módulo de Desenvolvimento Patrimonial", TipoAtoDocumental.MODULO_PATRIMONIAL, false),
        ],
    };
}

/// <summary>
/// Módulo documental no catálogo.
/// </summary>
public class ModuloDocumental
{
    public string Codigo { get; set; }
    public string Nome { get; set; }
    public TipoAtoDocumental TipoAto { get; set; }
    public bool Implementado { get; set; }

    public ModuloDocumental(string codigo, string nome, TipoAtoDocumental tipoAto, bool implementado)
    {
        Codigo = codigo;
        Nome = nome;
        TipoAto = tipoAto;
        Implementado = implementado;
    }
}
