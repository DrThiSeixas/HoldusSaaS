#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MATRIZ DE DECISÃO — Acordo de Quotistas da Destino
// 16 perguntas → resposta → cláusula que entra → trava jurídica
//
// "O acordo é motor de governança sucessória,
//  não anexo textual."
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Resultado da avaliação da matriz de decisão.
/// Contém módulos obrigatórios, alertas, bloqueios e cláusulas ativadas.
/// </summary>
public sealed class MatrizDecisaoResult
{
    public bool PodeGerar => !Bloqueios.Any();
    public List<string> ModulosObrigatorios { get; set; } = new();
    public List<string> ModulosAtivados { get; set; } = new();
    public List<DecisaoBloqueio> Bloqueios { get; set; } = new();
    public List<DecisaoAlerta> Alertas { get; set; } = new();
    public List<ClausulaAtivada> ClausulasAtivadas { get; set; } = new();
    public List<PerguntaRespondida> Perguntas { get; set; } = new();
}

public sealed class DecisaoBloqueio
{
    public string Codigo { get; set; } = string.Empty;
    public int PerguntaNumero { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public string? FundamentoLegal { get; set; }
}

public sealed class DecisaoAlerta
{
    public string Codigo { get; set; } = string.Empty;
    public int PerguntaNumero { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}

public sealed class ClausulaAtivada
{
    public string Modulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public bool Obrigatoria { get; set; }
}

public sealed class PerguntaRespondida
{
    public int Numero { get; set; }
    public string Pergunta { get; set; } = string.Empty;
    public string Resposta { get; set; } = string.Empty;
    public bool Respondida { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// ENGINE DA MATRIZ
// ═══════════════════════════════════════════════════════════════

public sealed class MatrizDecisaoAcordoEngine
{
    /// <summary>
    /// Avalia o AcordoQuotistasProfile contra as 16 perguntas da matriz.
    /// Retorna módulos obrigatórios, bloqueios, alertas e cláusulas ativadas.
    /// </summary>
    public MatrizDecisaoResult Avaliar(AcordoQuotistasProfile profile)
    {
        var r = new MatrizDecisaoResult();

        // Resolver módulos obrigatórios automaticamente
        profile.ResolveModulosObrigatorios();
        r.ModulosObrigatorios = new List<string>(profile.MandatoryModules);

        // ── P1: Quem está vinculado? ─────────────────────
        var p1 = profile.Identificacao.Partes.Any();
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 1,
            Pergunta = "Quem assina e em que qualidade?",
            Resposta = p1 ? $"{profile.Identificacao.Partes.Count} signatário(s)" : "Nenhum",
            Respondida = p1
        });
        if (!p1)
            r.Bloqueios.Add(new DecisaoBloqueio
            {
                Codigo = "MD_01",
                PerguntaNumero = 1,
                Mensagem = "Ninguém ingressa na governança sem adesão expressa. Adicione ao menos dois signatários."
            });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.01", Descricao = "Identificação, qualidade e adesão obrigatória", Obrigatoria = true });

        // ── P2: Finalidade sucessória? ───────────────────
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 2,
            Pergunta = "A finalidade da sociedade é sucessória?",
            Resposta = "Sim (fixo — célula Destino)",
            Respondida = true
        });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.02", Descricao = "Finalidade fixa: célula de transferência patrimonial por quotas", Obrigatoria = true });

        // ── P3: Há usufruto? ─────────────────────────────
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 3,
            Pergunta = "Existe usufruto incidente sobre quotas?",
            Resposta = profile.HasUsufruct ? "Sim" : "Não",
            Respondida = true
        });
        if (profile.HasUsufruct)
        {
            r.ModulosAtivados.Add("DQ.03");
            r.ModulosAtivados.Add("DQ.04");
            r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.03", Descricao = "Módulo de usufruto — direitos políticos", Obrigatoria = true });
            r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.04", Descricao = "Módulo de usufruto — direitos econômicos", Obrigatoria = true });
        }

        // ── P4: Quem vota nas quotas com usufruto? ───────
        var dp = profile.DireitosPoliticos;
        var p4resp = dp.ModoVoto switch
        {
            ModoVotoUsufruto.SemUsufruto => "Sem usufruto — quotista vota direto",
            ModoVotoUsufruto.UsufrutuarioIntegral => "Usufrutuário vota",
            ModoVotoUsufruto.NuProprietarioIntegral => "Nu-proprietário vota",
            ModoVotoUsufruto.Misto => $"Misto: ordinário={dp.VotoMateriasOrdinarias}, extraordinário={dp.VotoMateriasExtraordinarias}",
            _ => "NÃO DEFINIDO"
        };
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 4,
            Pergunta = "Quem exerce os direitos políticos?",
            Resposta = p4resp,
            Respondida = dp.EstaDefinido
        });
        if (profile.HasUsufruct && !dp.EstaDefinido)
            r.Bloqueios.Add(new DecisaoBloqueio
            {
                Codigo = "MD_04",
                PerguntaNumero = 4,
                Mensagem = "BLOQUEIO: Há usufruto mas o regime de voto não foi definido. O DREI exige disciplina clara.",
                FundamentoLegal = "DREI — eficácia perante terceiros de instrumento parassocial"
            });

        // ── P5: Quem recebe lucros e frutos? ─────────────
        var de = profile.DireitosEconomicos;
        var p5resp = de.ModoEconomico switch
        {
            ModoEconomicoUsufruto.UsufrutuarioIntegral => "Usufrutuário 100%",
            ModoEconomicoUsufruto.NuProprietarioIntegral => "Nu-proprietário 100%",
            ModoEconomicoUsufruto.Repartido => $"Repartido: {de.FormulaReparticao}",
            _ => "NÃO DEFINIDO"
        };
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 5,
            Pergunta = "Quem exerce os direitos econômicos?",
            Resposta = p5resp,
            Respondida = de.ModoEconomico != ModoEconomicoUsufruto.NaoDefinido
        });
        if (profile.HasUsufruct && de.ModoEconomico == ModoEconomicoUsufruto.NaoDefinido)
            r.Bloqueios.Add(new DecisaoBloqueio
            {
                Codigo = "MD_05",
                PerguntaNumero = 5,
                Mensagem = "BLOQUEIO: Há usufruto mas os direitos econômicos não foram definidos."
            });
        if (de.ExcluiQuotistaDeResultados)
            r.Bloqueios.Add(new DecisaoBloqueio
            {
                Codigo = "MD_05B",
                PerguntaNumero = 5,
                Mensagem = "BLOQUEIO: Não é permitida exclusão de quotista da repartição de lucros na limitada.",
                FundamentoLegal = "DREI + regime legal da Ltda"
            });

        // ── P6: Donatário recebe comando imediato? ───────
        var adm = profile.Administracao;
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 6,
            Pergunta = "O donatário passa a votar e administrar plenamente?",
            Resposta = adm.MateriasVedadasADonatarios.Any()
                ? $"Limitado — {adm.MateriasVedadasADonatarios.Count} matéria(s) vedada(s)"
                : "Pleno (sem restrição)",
            Respondida = true
        });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.05", Descricao = "Governança e matérias reservadas", Obrigatoria = true });

        // ── P7: Quotas podem circular livremente? ────────
        var circ = profile.Circulacao;
        var p7resp = circ.VedacaoCessaoTerceiros ? "Vedada cessão a terceiros" : "Cessão permitida com preferência";
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 7,
            Pergunta = "A Destino permitirá cessão livre?",
            Resposta = p7resp,
            Respondida = true
        });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.06", Descricao = "Restrições à circulação de quotas", Obrigatoria = true });

        // ── P8: Cônjuge do donatário pode entrar? ────────
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 8,
            Pergunta = "Há admissão automática de cônjuge/companheiro?",
            Resposta = circ.EntradaConjugePermitida ? "Ingresso condicionado" : "Vedado — reflexo patrimonial não é ingresso societário",
            Respondida = true
        });

        // ── P9: Eventos familiares críticos? ─────────────
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 9,
            Pergunta = "Quais eventos familiares precisam de disciplina?",
            Resposta = "Falecimento, incapacidade, divórcio, penhora, insolvência",
            Respondida = true
        });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.07", Descricao = "Eventos familiares críticos", Obrigatoria = true });

        // ── P10: Natureza da doação? ─────────────────────
        // (esta pergunta integra com o instrumento de doação — Minuta 5.0)
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 10,
            Pergunta = "Doação é adiantamento de legítima ou parte disponível?",
            Resposta = "(integrado ao instrumento de doação — DoacaoDestinoService)",
            Respondida = true
        });

        // ── P11: Cláusulas restritivas? ──────────────────
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 11,
            Pergunta = "As quotas doadas terão restrições?",
            Resposta = "(integrado ao instrumento de doação — ClausulasRestritivas)",
            Respondida = true
        });

        // ── P12: Quórum das matérias sensíveis? ──────────
        var qTexto = adm.QuorumEleicao switch
        {
            QuorumEleicaoAdmin.Unanimidade => "Unanimidade",
            QuorumEleicaoAdmin.MaioriaQualificada => "Maioria qualificada",
            QuorumEleicaoAdmin.MaioriaSimples => "Maioria simples",
            _ => "Não definido"
        };
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 12,
            Pergunta = "Quórum das matérias sensíveis?",
            Resposta = $"{qTexto} — {adm.MateriasUnanimidade.Count} unanimidade, {adm.MateriasMaioriaQualificada.Count} maioria qualificada",
            Respondida = true
        });

        // ── P13: Direito de saída? ───────────────────────
        var saida = profile.Saida;
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 13,
            Pergunta = "O sistema admite saída voluntária?",
            Resposta = saida.PermiteSaidaVoluntaria
                ? $"Sim — {saida.Valuation?.FormulaContratual ?? "Padrão"}, {saida.ParcelasPagamento}x, notificação {saida.PrazoNotificacaoDias}d"
                : "Não — saída vedada",
            Respondida = true
        });
        if (saida.PermiteSaidaVoluntaria && !string.IsNullOrEmpty(saida.Valuation?.FormulaContratual))
        {
            r.Alertas.Add(new DecisaoAlerta
            {
                Codigo = "MD_13",
                PerguntaNumero = 13,
                Mensagem = "Fórmula contratual selecionada — definir critério objetivo de valuation."
            });
        }

        // ── P14: Sanção por descumprimento? ──────────────
        var sancoes = profile.Sancoes;
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 14,
            Pergunta = "O que acontece se o acordo for violado?",
            Resposta = sancoes.ValorMulta.HasValue && sancoes.ValorMulta > 0
                ? $"Multa {sancoes.ValorMulta:C2} + perdas e danos"
                : "Multa não definida",
            Respondida = sancoes.ValorMulta.HasValue && sancoes.ValorMulta > 0
        });
        if (!sancoes.ValorMulta.HasValue || sancoes.ValorMulta <= 0)
            r.Alertas.Add(new DecisaoAlerta
            {
                Codigo = "MD_14",
                PerguntaNumero = 14,
                Mensagem = "Sanção sem multa definida. Cláusula pode ficar decorativa."
            });

        // ── P15: Arquivamento? ───────────────────────────
        var arq = profile.Arquivamento;
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 15,
            Pergunta = "Qual o nível de publicidade controlada?",
            Resposta = arq.Modo switch
            {
                ModoArquivamento.NaoArquivar => "Não arquivar",
                ModoArquivamento.ArquivarExtrato => "Arquivar extrato",
                ModoArquivamento.ArquivarClausulaCiencia => "Arquivar cláusula de ciência",
                ModoArquivamento.ArquivarIntegral => "Arquivar integralmente",
                _ => "Não definido"
            },
            Respondida = true
        });
        // Regra 5: usufruto + não arquivar = alerta forte
        if (profile.HasUsufruct && arq.Modo == ModoArquivamento.NaoArquivar)
            r.Alertas.Add(new DecisaoAlerta
            {
                Codigo = "MD_15",
                PerguntaNumero = 15,
                Mensagem = "ALERTA FORTE: Acordo regula usufruto mas não será arquivado. O DREI exige arquivamento para eficácia perante terceiros quando usufruto é regulado em instrumento parassocial."
            });

        // ── P16: Controvérsias? ──────────────────────────
        var cont = profile.Controversias;
        var p16resp = cont.Modo switch
        {
            ModoControversia.Foro => $"Foro: {cont.Comarca}/{cont.UfForo}",
            ModoControversia.Arbitragem => $"Arbitragem: {cont.CamaraArbitral}",
            ModoControversia.MediacaoMaisForo => $"Mediação {cont.PrazoMediacaoDias}d + Foro",
            ModoControversia.Escalonada => $"Mediação {cont.PrazoMediacaoDias}d → Arbitragem",
            _ => "Não definido"
        };
        r.Perguntas.Add(new PerguntaRespondida
        {
            Numero = 16,
            Pergunta = "Como as controvérsias serão resolvidas?",
            Resposta = p16resp,
            Respondida = true
        });
        if (cont.Modo == ModoControversia.Arbitragem && string.IsNullOrWhiteSpace(cont.CamaraArbitral))
            r.Bloqueios.Add(new DecisaoBloqueio
            {
                Codigo = "MD_16",
                PerguntaNumero = 16,
                Mensagem = "BLOQUEIO: Arbitragem selecionada sem câmara arbitral definida."
            });
        r.ClausulasAtivadas.Add(new ClausulaAtivada { Modulo = "DQ.12", Descricao = "Solução de controvérsias", Obrigatoria = true });

        // ── Consolidar módulos ativados ──────────────────
        r.ModulosAtivados = r.ClausulasAtivadas
            .Select(c => c.Modulo)
            .Distinct()
            .ToList();

        return r;
    }
}