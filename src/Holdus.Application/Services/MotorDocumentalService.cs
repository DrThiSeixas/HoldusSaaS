using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// ORQUESTRADOR DO MOTOR DOCUMENTAL
// "Não é abrir modelo e editar. É engenharia documental."
//
// Fluxo:
// 1. Define qual célula
// 2. Define qual ato
// 3. Valida riscos e travas
// 4. Só então monta a minuta
// ═══════════════════════════════════════════════════════════════

public interface IMotorDocumentalService
{
    /// <summary>
    /// Lista os módulos disponíveis para uma célula.
    /// </summary>
    List<ModuloDocumental> ListarModulos(CelulaDocumental celula);

    /// <summary>
    /// Valida as tags globais ANTES de entrar em qualquer módulo.
    /// Retorna bloqueios e alertas que se aplicam a qualquer ato da célula.
    /// </summary>
    ResultadoValidacao ValidarTagsGlobais(TagsMotorDocumental tags);

    /// <summary>
    /// Roteia para o módulo correto e executa a montagem.
    /// </summary>
    ResultadoOrquestrador Executar(TagsMotorDocumental tags, object diagnostico);
}

public class MotorDocumentalService : IMotorDocumentalService
{
    private readonly ValidacaoCofreService _validacaoCofre = new();
    private readonly MontagemCofreService _montagemCofre = new();
    private readonly MontagemAlteracaoCofreService _alteracaoImovel = new();
    private readonly ParticipacoesCofreService _participacoes = new();
    private readonly AlteracaoCofreVeiculoService _alteracaoCofreVeiculo = new();
    private readonly DestinoContratoService _destinoContrato = new();
    private readonly DoacaoDestinoService _doacaoDestino = new();
    private readonly AlteracaoDestinoService _alteracaoDestino = new();
    private readonly AcordoQuotistasDestinoService _acordoQuotistas = new();
    private readonly VeiculoContratoService _veiculoContrato = new();
    private readonly AlteracaoVeiculoDestinoService _alteracaoVeiculoDestino = new();

    // ═══════════════════════════════════════════════════════════
    // LISTAR MÓDULOS
    // ═══════════════════════════════════════════════════════════

    public List<ModuloDocumental> ListarModulos(CelulaDocumental celula)
    {
        return BibliotecaCelular.Catalogo.TryGetValue(celula, out var modulos)
            ? modulos
            : [];
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO GLOBAL DE TAGS
    // Travas que se aplicam antes de entrar em qualquer módulo.
    // ═══════════════════════════════════════════════════════════

    public ResultadoValidacao ValidarTagsGlobais(TagsMotorDocumental tags)
    {
        var r = new ResultadoValidacao();

        // ── Trava funcional do Cofre ─────────────────────────
        if (tags.Celula == CelulaDocumental.COFRE && tags.AtividadeOperacionalPropria)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "TRAVA-COFRE",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = TravasMotor.TravaCofre,
                Sugestao = "Reclassificar a célula ou remover a atividade operacional."
            });
        }

        // ── Trava ITBI ──────────────────────────────────────
        if (tags.HaRiscoTema796)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "TRAVA-ITBI",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = TravasMotor.TravaItbi,
                FundamentoLegal = "Tema 796, STF"
            });
        }

        // ── Trava de participações ──────────────────────────
        if (tags.IntegralizacaoParticipacoes && !tags.HaAtoReflexo)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "TRAVA-PARTICIPACOES",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = TravasMotor.TravaParticipacoes
            });
        }

        // ── Trava conjugal ──────────────────────────────────
        if (tags.TemCasadoOuUniaoEstavel && tags.IntegralizacaoImovel && !tags.HaAnuenciaConjugal)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "TRAVA-CONJUGAL",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = TravasMotor.TravaConjugal,
                FundamentoLegal = "Art. 977 e 1.647, CC/2002"
            });
        }

        // ── Trava de tramitação conjunta ────────────────────
        if (tags.ExigeTramitacaoConjunta)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "TRAVA-TRAMITACAO",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = "As sociedades estão na mesma UF. Os processos devem tramitar conjuntamente na Junta Comercial.",
                FundamentoLegal = "Manual DREI"
            });
        }

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // ROTEAMENTO — Seleciona o módulo correto
    // ═══════════════════════════════════════════════════════════

    public ResultadoOrquestrador Executar(TagsMotorDocumental tags, object diagnostico)
    {
        // 1. Validar tags globais
        var validacaoGlobal = ValidarTagsGlobais(tags);
        if (!validacaoGlobal.PodeGerar)
        {
            return new ResultadoOrquestrador
            {
                Sucesso = false,
                CodigoModulo = new CodigoDocumento { Celula = tags.Celula, TipoAto = tags.TipoAto }.Codigo,
                ValidacaoGlobal = validacaoGlobal,
                TravasAtivas = ObterTravasAtivas(tags),
            };
        }

        // 2. Verificar se o módulo está implementado
        var modulo = ListarModulos(tags.Celula)
            .FirstOrDefault(m => m.TipoAto == tags.TipoAto);

        if (modulo == null || !modulo.Implementado)
        {
            return new ResultadoOrquestrador
            {
                Sucesso = false,
                CodigoModulo = new CodigoDocumento { Celula = tags.Celula, TipoAto = tags.TipoAto }.Codigo,
                Erro = $"Módulo '{tags.Celula}.{tags.TipoAto}' não está implementado ainda.",
                ValidacaoGlobal = validacaoGlobal,
            };
        }

        // 3. Rotear para o serviço correto
        return tags.Celula switch
        {
            CelulaDocumental.COFRE => RotearCofre(tags, diagnostico, modulo, validacaoGlobal),
            CelulaDocumental.VEICULO => RotearVeiculo(tags, diagnostico, modulo, validacaoGlobal),
            CelulaDocumental.DESTINO => RotearDestino(tags, diagnostico, modulo, validacaoGlobal),
            _ => NaoImplementado(modulo)
        };
    }

    // ═══════════════════════════════════════════════════════════
    // ROTEAMENTO DO COFRE
    // ═══════════════════════════════════════════════════════════

    private ResultadoOrquestrador RotearCofre(TagsMotorDocumental tags, object diagnostico, ModuloDocumental modulo, ResultadoValidacao validacaoGlobal)
    {
        return tags.TipoAto switch
        {
            // ── COFRE.CONSTITUICAO.V1 ────────────────────────
            TipoAtoDocumental.CONSTITUICAO when diagnostico is DiagnosticoCofre diag =>
                FromMontagem(_montagemCofre.Montar(diag), modulo, validacaoGlobal, tags),

            // ── COFRE.ALT_CAPITAL_IMOVEL.V1 ──────────────────
            TipoAtoDocumental.ALT_CAPITAL_IMOVEL when diagnostico is DiagnosticoAlteracaoCofre diag =>
                FromAlteracao(_alteracaoImovel.Montar(diag), modulo, validacaoGlobal, tags),

            // ── COFRE.ALT_CAPITAL_PARTICIPACOES.V1 ───────────
            TipoAtoDocumental.ALT_CAPITAL_PARTICIPACOES when diagnostico is DiagnosticoParticipacoesCofre diag =>
                FromParticipacoes(_participacoes.Montar(diag), modulo, validacaoGlobal, tags),

            // ── COFRE.ALT_TITULARIDADE_VEICULO.V1 ───────────
            TipoAtoDocumental.ALT_TITULARIDADE_VEICULO when diagnostico is DiagnosticoTransferenciaCofreVeiculo diag =>
                FromMontagem(_alteracaoCofreVeiculo.Montar(diag), modulo, validacaoGlobal, tags),

            _ => new ResultadoOrquestrador
            {
                Sucesso = false,
                CodigoModulo = modulo.Codigo,
                Erro = $"Tipo de diagnóstico incompatível com o módulo '{modulo.Codigo}'.",
                ValidacaoGlobal = validacaoGlobal,
            }
        };
    }

    // ═══════════════════════════════════════════════════════════
    // CONVERSORES DE RESULTADO
    // ═══════════════════════════════════════════════════════════

    private ResultadoOrquestrador FromMontagem(ResultadoMontagem r, ModuloDocumental modulo, ResultadoValidacao global, TagsMotorDocumental tags) => new()
    {
        Sucesso = r.Sucesso,
        CodigoModulo = modulo.Codigo,
        ValidacaoGlobal = global,
        ValidacaoModulo = r.Validacao,
        DocumentosPrincipais = r.Sucesso ? [new DocumentoGeradoMotor("Contrato Social", r.ConteudoHtml!)] : [],
        BlocosCondicionais = r.BlocosCondicionaisAtivados,
        ChecklistRegistral = r.Validacao.ChecklistRegistral,
        TravasAtivas = ObterTravasAtivas(tags),
    };

    private ResultadoOrquestrador FromAlteracao(ResultadoMontagem r, ModuloDocumental modulo, ResultadoValidacao global, TagsMotorDocumental tags) => new()
    {
        Sucesso = r.Sucesso,
        CodigoModulo = modulo.Codigo,
        ValidacaoGlobal = global,
        ValidacaoModulo = r.Validacao,
        DocumentosPrincipais = r.Sucesso ? [new DocumentoGeradoMotor("Alteração Contratual — Aumento de Capital por Imóvel", r.ConteudoHtml!)] : [],
        BlocosCondicionais = r.BlocosCondicionaisAtivados,
        ChecklistRegistral = r.Validacao.ChecklistRegistral,
        TravasAtivas = ObterTravasAtivas(tags),
    };

    private ResultadoOrquestrador FromParticipacoes(ResultadoMontagemParticipacoes r, ModuloDocumental modulo, ResultadoValidacao global, TagsMotorDocumental tags) => new()
    {
        Sucesso = r.Sucesso,
        CodigoModulo = modulo.Codigo,
        ValidacaoGlobal = global,
        ValidacaoModulo = r.Validacao,
        DocumentosPrincipais = r.Sucesso
            ? [
                .. r.HtmlAtoInvestida != null ? [new DocumentoGeradoMotor("Ato Reflexo — Investida", r.HtmlAtoInvestida)] : Array.Empty<DocumentoGeradoMotor>(),
                new DocumentoGeradoMotor("Alteração Contratual — Aumento de Capital por Participações", r.HtmlAtoCofre!),
              ]
            : [],
        ChecklistSA = r.ChecklistSA,
        BlocosCondicionais = r.BlocosCondicionaisAtivados,
        ChecklistRegistral = r.Validacao.ChecklistRegistral,
        TravasAtivas = ObterTravasAtivas(tags),
    };

    // ═══════════════════════════════════════════════════════════
    // ROTEAMENTO DO DESTINO
    // ═══════════════════════════════════════════════════════════

    private ResultadoOrquestrador RotearDestino(TagsMotorDocumental tags, object diagnostico, ModuloDocumental modulo, ResultadoValidacao validacaoGlobal)
    {
        return tags.TipoAto switch
        {
            TipoAtoDocumental.CONSTITUICAO_DESTINO when diagnostico is DiagnosticoDestino diag =>
                FromMontagem(_destinoContrato.Montar(diag), modulo, validacaoGlobal, tags),

            TipoAtoDocumental.DOACAO_QUOTAS when diagnostico is DiagnosticoDoacaoDestino diag =>
                FromMontagem(_doacaoDestino.Montar(diag), modulo, validacaoGlobal, tags),

            TipoAtoDocumental.ALT_DOACAO when diagnostico is DiagnosticoAlteracaoDestino diag =>
                FromMontagem(_alteracaoDestino.Montar(diag), modulo, validacaoGlobal, tags),

            TipoAtoDocumental.ACORDO_QUOTISTAS when diagnostico is DiagnosticoAcordoDestino diag =>
                FromMontagem(_acordoQuotistas.Montar(diag), modulo, validacaoGlobal, tags),

            _ => new ResultadoOrquestrador
            {
                Sucesso = false,
                CodigoModulo = modulo.Codigo,
                Erro = $"Tipo de diagnóstico incompatível com o módulo '{modulo.Codigo}'.",
                ValidacaoGlobal = validacaoGlobal,
            }
        };
    }

    // ═══════════════════════════════════════════════════════════
    // ROTEAMENTO DO VEÍCULO
    // ═══════════════════════════════════════════════════════════

    private ResultadoOrquestrador RotearVeiculo(TagsMotorDocumental tags, object diagnostico, ModuloDocumental modulo, ResultadoValidacao validacaoGlobal)
    {
        return tags.TipoAto switch
        {
            TipoAtoDocumental.CONSTITUICAO_VEICULO when diagnostico is VeiculoProfile diag =>
                FromMontagem(_veiculoContrato.Montar(diag), modulo, validacaoGlobal, tags),

            TipoAtoDocumental.ALT_COMPRA_ORDINARIAS when diagnostico is DiagnosticoCompraOrdinariasPelaDestino diag =>
                FromMontagem(_alteracaoVeiculoDestino.Montar(diag), modulo, validacaoGlobal, tags),

            _ => new ResultadoOrquestrador
            {
                Sucesso = false,
                CodigoModulo = modulo.Codigo,
                Erro = $"Tipo de diagnóstico incompatível com o módulo '{modulo.Codigo}'.",
                ValidacaoGlobal = validacaoGlobal,
            }
        };
    }

    private static ResultadoOrquestrador NaoImplementado(ModuloDocumental modulo) => new()
    {
        Sucesso = false,
        CodigoModulo = modulo.Codigo,
        Erro = $"Módulo '{modulo.Codigo}' ainda não foi implementado."
    };

    // ═══════════════════════════════════════════════════════════
    // TRAVAS ATIVAS
    // ═══════════════════════════════════════════════════════════

    private static List<TravaAtiva> ObterTravasAtivas(TagsMotorDocumental tags)
    {
        var travas = new List<TravaAtiva>();

        if (tags.Celula == CelulaDocumental.COFRE && tags.CofrePuro)
            travas.Add(new("COFRE-PURO", TravasMotor.TravaCofre, "info"));

        if (tags.HaRiscoTema796)
            travas.Add(new("ITBI-796", TravasMotor.TravaItbi, "danger"));

        if (tags.IntegralizacaoParticipacoes)
            travas.Add(new("PARTICIPACOES", TravasMotor.TravaParticipacoes, "warning"));

        if (tags.TemCasadoOuUniaoEstavel && tags.IntegralizacaoImovel)
            travas.Add(new("CONJUGAL", TravasMotor.TravaConjugal, "warning"));

        if (tags.IntegralizacaoImovel)
            travas.Add(new("DESCRICAO-IMOVEL", TravasMotor.TravaDescricaoImovel, "info"));

        return travas;
    }
}

// ═══════════════════════════════════════════════════════════════
// RESULTADO DO ORQUESTRADOR
// ═══════════════════════════════════════════════════════════════

public class ResultadoOrquestrador
{
    public bool Sucesso { get; set; }
    public string CodigoModulo { get; set; } = string.Empty;
    public string? Erro { get; set; }

    // Validações
    public ResultadoValidacao ValidacaoGlobal { get; set; } = new();
    public ResultadoValidacao ValidacaoModulo { get; set; } = new();

    // Documentos gerados (pode ser mais de 1 — ex: investida + Cofre)
    public List<DocumentoGeradoMotor> DocumentosPrincipais { get; set; } = [];

    // Extras
    public List<string>? ChecklistSA { get; set; }
    public List<string> ChecklistRegistral { get; set; } = [];
    public List<string> BlocosCondicionais { get; set; } = [];
    public List<TravaAtiva> TravasAtivas { get; set; } = [];
}

public class DocumentoGeradoMotor
{
    public string Titulo { get; set; }
    public string Html { get; set; }

    public DocumentoGeradoMotor(string titulo, string html)
    {
        Titulo = titulo;
        Html = html;
    }
}

public class TravaAtiva
{
    public string Codigo { get; set; }
    public string Mensagem { get; set; }
    public string Nivel { get; set; }  // info, warning, danger

    public TravaAtiva(string codigo, string mensagem, string nivel)
    {
        Codigo = codigo;
        Mensagem = mensagem;
        Nivel = nivel;
    }
}
