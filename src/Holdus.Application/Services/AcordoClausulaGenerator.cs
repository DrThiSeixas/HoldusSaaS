#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// BIBLIOTECA DE CLÁUSULAS + RESOLVER
// Acordo de Quotistas da Célula Destino
//
// 15 módulos (MOD.01..15)
// 9 obrigatórios por padrão
// 4 condicionais (usufruto → 04,05,06,14 | doação → 03 | restrições → 10)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// DTO de parametrização — entrada do gerador.
/// </summary>
public sealed class AcordoParametrizacaoDTO
{
    public string AgreementCode { get; set; } = "DESTINO.ACQ.V1";

    // Sociedade
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Nire { get; set; } = string.Empty;

    // Partes
    public List<ParteAcordoDTO> Signatarios { get; set; } = new();

    // Condições que ativam módulos
    public bool HasUsufruct { get; set; }
    public bool GiftToDescendant { get; set; }
    public bool HasRestrictiveClauses { get; set; }

    // MOD.03 — Natureza
    public string NaturezaDoacao { get; set; } = "adiantamento";  // adiantamento | parte_disponivel
    public bool ConfirmaLimiteParteDisponivel { get; set; }

    // MOD.04 — Usufruto
    public string? UsufrutuarioNome { get; set; }
    public string? NuProprietarioNome { get; set; }

    // MOD.05 — Voto
    public string ModoPolitico { get; set; } = "nao_definido";
    // usufrutuario | nu_proprietario | misto | nao_definido
    public string? VotoOrdinario { get; set; }
    public string? VotoExtraordinario { get; set; }

    // MOD.06 — Econômico
    public string ModoEconomico { get; set; } = "usufrutuario";
    // usufrutuario | nu_proprietario | repartido
    public string? FormulaReparticao { get; set; }

    // MOD.07 — Administração
    public string? AdministradorNome { get; set; }
    public string Quorum { get; set; } = "unanimidade";
    public List<string> MateriasReservadas { get; set; } = new();

    // MOD.08 — Circulação
    public bool VedacaoCessao { get; set; } = true;
    public bool EntradaConjuge { get; set; } = false;
    public int PrazoPreferenciaDias { get; set; } = 30;

    // MOD.10 — Restrições
    public bool Incomunicabilidade { get; set; }
    public bool Impenhorabilidade { get; set; }
    public bool Inalienabilidade { get; set; }
    public bool Reversao { get; set; }

    // MOD.11 — Saída
    public string MetodoValuation { get; set; } = "balanço de determinação";
    public int Parcelas { get; set; } = 12;
    public int PrazoNotificacaoDias { get; set; } = 90;

    // MOD.12 — Sanções
    public decimal? ValorMulta { get; set; }

    // MOD.14 — Arquivamento
    public string ModoArquivamento { get; set; } = "extrato";
    // nenhum | extrato | clausula_ciencia | integral

    // MOD.15 — Controvérsias
    public string ModoControversia { get; set; } = "foro";
    // foro | arbitragem | mediacao_foro | escalonada
    public string? Comarca { get; set; }
    public string? UfForo { get; set; }
    public string? CamaraArbitral { get; set; }
}

public sealed class ParteAcordoDTO
{
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Qualidade { get; set; } = "quotista";
    public string? QualificacaoCompleta { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// RESOLVER — Determina quais módulos entram
// ═══════════════════════════════════════════════════════════════

public static class AcordoClausulaResolver
{
    private static readonly HashSet<string> RequiredByDefault = new()
    {
        "01", "02", "07", "08", "09", "11", "12", "14", "15"
    };

    public static List<string> Resolver(AcordoParametrizacaoDTO dto)
    {
        var modulos = new List<string>(RequiredByDefault);

        if (dto.HasUsufruct)
        {
            modulos.Add("04");
            modulos.Add("05");
            modulos.Add("06");
            // Reforça 14 (já está, mas marca como elevado)
        }

        if (dto.GiftToDescendant)
            modulos.Add("03");

        if (dto.HasRestrictiveClauses)
            modulos.Add("10");

        // MOD.13 (confidencialidade) — tratado como obrigatório no Holdus
        modulos.Add("13");

        return modulos.Distinct().OrderBy(m => m).ToList();
    }
}

// ═══════════════════════════════════════════════════════════════
// GERADOR DE CLÁUSULAS — Monta o HTML módulo a módulo
// ═══════════════════════════════════════════════════════════════

public sealed class AcordoClausulaGenerator
{
    public ResultadoMontagem Gerar(AcordoParametrizacaoDTO dto)
    {
        // 1. Validar
        var val = Validar(dto);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        // 2. Resolver módulos
        var modulos = AcordoClausulaResolver.Resolver(dto);

        // 3. Montar cláusulas
        var sb = new StringBuilder();
        int clausulaNum = 0;

        sb.AppendLine($"<h1>ACORDO DE QUOTISTAS</h1>");
        sb.AppendLine($"<h2>da sociedade {dto.NomeEmpresarial.ToUpper()} LTDA.</h2>");

        // Preâmbulo
        sb.AppendLine("<p>Pelo presente instrumento particular, as partes abaixo identificadas:</p>");
        foreach (var s in dto.Signatarios)
        {
            var qual = s.QualificacaoCompleta ?? $"{s.Nome}, CPF nº {s.Cpf}";
            sb.AppendLine($"<p><strong>{s.Nome.ToUpper()}</strong>, {qual}, na qualidade de {s.Qualidade};</p>");
        }
        sb.AppendLine($"<p>têm entre si justo e contratado o presente Acordo de Quotistas da sociedade <strong>{dto.NomeEmpresarial.ToUpper()} LTDA.</strong>, CNPJ nº {dto.Cnpj}, que se regerá pelas cláusulas e condições seguintes.</p>");

        // MOD.01 — Finalidade e vinculação
        if (modulos.Contains("01"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — FINALIDADE E VINCULAÇÃO</h2>");
            sb.AppendLine($"<p>O presente acordo disciplina a governança societária e sucessória da sociedade <strong>{dto.NomeEmpresarial.ToUpper()} LTDA.</strong>, organizada como célula de transferência patrimonial por quotas, complementando o contrato social, os instrumentos de doação, os pactos de usufruto e os demais atos de planejamento patrimonial e sucessório a ela vinculados.</p>");
        }

        // MOD.02 — Adesão obrigatória
        if (modulos.Contains("02"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — ADESÃO OBRIGATÓRIA DE INGRESSANTES</h2>");
            sb.AppendLine("<p>Ficam vinculados ao presente acordo todos os quotistas atuais e futuros, bem como os que ingressarem na sociedade por doação, sucessão, cessão ou qualquer outro título juridicamente admitido, condicionando-se a plena eficácia interna do ingresso à adesão expressa a este instrumento.</p>");
        }

        // MOD.03 — Natureza sucessória (condicional: doação a descendente)
        if (modulos.Contains("03"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — NATUREZA SUCESSÓRIA DA DOAÇÃO</h2>");

            if (dto.NaturezaDoacao == "parte_disponivel")
            {
                sb.AppendLine("<p>As partes reconhecem que a liberalidade foi imputada à parte disponível do patrimônio do doador, com dispensa de colação, nos limites legais e na forma expressamente declarada no instrumento de doação.</p>");
            }
            else
            {
                sb.AppendLine("<p>As partes reconhecem que a doação de quotas realizada por ascendente a descendente constitui, por padrão, adiantamento do que cabe ao donatário por herança, sem prejuízo do tratamento específico constante do instrumento de doação.</p>");
            }
        }

        // MOD.04 — Usufruto (condicional)
        if (modulos.Contains("04"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — USUFRUTO SOBRE QUOTAS</h2>");
            sb.AppendLine($"<p>As quotas indicadas no instrumento de doação permanecem gravadas com usufruto em favor de <strong>{dto.UsufrutuarioNome}</strong>, observando-se, quanto aos direitos políticos e econômicos, o regime previsto neste acordo, no contrato social e no título constitutivo do usufruto.</p>");
        }

        // MOD.05 — Direitos políticos (condicional)
        if (modulos.Contains("05"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — EXERCÍCIO DOS DIREITOS POLÍTICOS</h2>");

            switch (dto.ModoPolitico)
            {
                case "usufrutuario":
                    sb.AppendLine("<p>Enquanto perdurar o usufruto, os direitos políticos inerentes às quotas gravadas serão exercidos pelo usufrutuário.</p>");
                    break;
                case "nu_proprietario":
                    sb.AppendLine("<p>Enquanto perdurar o usufruto, os direitos políticos inerentes às quotas gravadas serão exercidos pelo nu-proprietário.</p>");
                    break;
                case "misto":
                    sb.AppendLine($"<p>Nas matérias ordinárias, o voto será exercido pelo {dto.VotoOrdinario}; nas matérias extraordinárias, dependerá de manifestação conjunta de usufrutuário e nu-proprietário.</p>");
                    break;
            }
        }

        // MOD.06 — Direitos econômicos (condicional)
        if (modulos.Contains("06"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — DIREITOS ECONÔMICOS</h2>");

            switch (dto.ModoEconomico)
            {
                case "usufrutuario":
                    sb.AppendLine("<p>Enquanto perdurar o usufruto, os frutos civis, lucros, dividendos e demais resultados econômicos das quotas gravadas pertencerão ao usufrutuário.</p>");
                    break;
                case "nu_proprietario":
                    sb.AppendLine("<p>Os resultados econômicos das quotas gravadas pertencerão ao nu-proprietário, ressalvado o usufruto limitado aos direitos definidos em instrumento próprio.</p>");
                    break;
                case "repartido":
                    sb.AppendLine($"<p>Os resultados econômicos serão repartidos entre usufrutuário e nu-proprietário na proporção de {dto.FormulaReparticao}.</p>");
                    break;
            }

            sb.AppendLine("<p><strong>Parágrafo único.</strong> Este acordo não autoriza exclusão de quotista da repartição de resultados em desconformidade com a disciplina legal aplicável à limitada.</p>");
        }

        // MOD.07 — Administração e matérias reservadas
        if (modulos.Contains("07"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — ADMINISTRAÇÃO E MATÉRIAS RESERVADAS</h2>");

            if (!string.IsNullOrWhiteSpace(dto.AdministradorNome))
                sb.AppendLine($"<p>A administração da sociedade observará o contrato social, cabendo a <strong>{dto.AdministradorNome}</strong> a gestão ordinária.</p>");
            else
                sb.AppendLine("<p>A administração da sociedade observará o contrato social.</p>");

            var materias = dto.MateriasReservadas.Any()
                ? dto.MateriasReservadas
                : new List<string>
                {
                    "alteração da finalidade da sociedade",
                    "ingresso de terceiro estranho ao núcleo familiar",
                    "alienação ou oneração relevante de ativos",
                    "modificação da engenharia sucessória",
                    "renúncia de usufruto",
                    "alteração deste acordo"
                };

            sb.AppendLine($"<p><strong>Parágrafo único.</strong> Ficam sujeitas à aprovação por {dto.Quorum} as seguintes matérias:</p>");
            for (int i = 0; i < materias.Count; i++)
                sb.AppendLine($"<p>{ToRoman(i + 1)} — {materias[i]};</p>");
        }

        // MOD.08 — Circulação e bloqueio
        if (modulos.Contains("08"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — CIRCULAÇÃO E BLOQUEIO DE QUOTAS</h2>");
            sb.AppendLine("<p>É vedada a cessão, alienação, promessa de cessão, oneração ou qualquer forma de disposição das quotas, salvo nas hipóteses previstas neste acordo e no contrato social, ficando assegurado direito de preferência aos signatários e vedado o ingresso automático de terceiros, inclusive cônjuge, companheiro, ex-cônjuge ou ex-companheiro, por mero reflexo patrimonial externo.</p>");
            sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> O direito de preferência deverá ser exercido no prazo de {dto.PrazoPreferenciaDias} ({ExtN(dto.PrazoPreferenciaDias)}) dias.</p>");
            if (!dto.EntradaConjuge)
                sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Efeito patrimonial não equivale a ingresso societário.</p>");
        }

        // MOD.09 — Eventos familiares
        if (modulos.Contains("09"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — EVENTOS FAMILIARES CRÍTICOS</h2>");
            sb.AppendLine("<p>Em caso de falecimento, incapacidade, divórcio, dissolução de união estável, penhora, insolvência ou outro evento apto a comprometer a estabilidade da estrutura societária, aplicar-se-ão as regras específicas deste acordo, vedado o ingresso automático de sucessores ou terceiros sem observância do contrato social, do presente instrumento e dos atos complementares pertinentes.</p>");
        }

        // MOD.10 — Restrições (condicional)
        if (modulos.Contains("10"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — RESTRIÇÕES INCIDENTES SOBRE AS QUOTAS</h2>");

            var restricoes = new List<string>();
            if (dto.Incomunicabilidade) restricoes.Add("incomunicabilidade");
            if (dto.Impenhorabilidade) restricoes.Add("impenhorabilidade");
            if (dto.Inalienabilidade) restricoes.Add("inalienabilidade");
            if (dto.Reversao) restricoes.Add("reversão");

            sb.AppendLine($"<p>As quotas objeto da doação permanecem sujeitas às cláusulas de {string.Join(", ", restricoes)}, nos exatos limites definidos no instrumento de doação e neste acordo.</p>");
        }

        // MOD.11 — Saída e valuation
        if (modulos.Contains("11"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — SAÍDA, LIQUIDEZ E APURAÇÃO</h2>");
            sb.AppendLine($"<p>O signatário que desejar sair da estrutura deverá ofertar suas quotas prioritariamente aos demais vinculados, observando-se o critério de apuração de haveres por {dto.MetodoValuation}, com pagamento em {dto.Parcelas} ({ExtN(dto.Parcelas)}) parcelas, precedido de notificação com antecedência mínima de {dto.PrazoNotificacaoDias} ({ExtN(dto.PrazoNotificacaoDias)}) dias.</p>");
        }

        // MOD.12 — Sanções
        if (modulos.Contains("12"))
        {
            clausulaNum++;
            var multa = dto.ValorMulta.HasValue && dto.ValorMulta > 0
                ? Fmt(dto.ValorMulta.Value) : "R$ [●]";

            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — SANÇÕES E REMÉDIOS</h2>");
            sb.AppendLine($"<p>O descumprimento das obrigações previstas neste acordo sujeitará o infrator à multa não compensatória de {multa}, sem prejuízo de obrigação de fazer ou não fazer, perdas e danos, tutela específica e demais medidas societárias cabíveis.</p>");
        }

        // MOD.13 — Confidencialidade
        if (modulos.Contains("13"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — CONFIDENCIALIDADE E COOPERAÇÃO</h2>");
            sb.AppendLine("<p>Os signatários obrigam-se a manter sigilo sobre os termos deste acordo, dos instrumentos sucessórios correlatos e das informações patrimoniais e familiares a que tenham acesso, bem como a cooperar para a assinatura, averbação, arquivamento e atualização dos atos necessários à manutenção da coerência da estrutura societária e sucessória.</p>");
        }

        // MOD.14 — Arquivamento
        if (modulos.Contains("14"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — ARQUIVAMENTO E EFICÁCIA PERANTE TERCEIROS</h2>");

            var textoArq = dto.ModoArquivamento switch
            {
                "integral" => "Os signatários autorizam o arquivamento integral deste acordo perante a Junta Comercial, para produção de efeitos perante terceiros.",
                "extrato" => "Os signatários autorizam o arquivamento de extrato deste acordo perante a Junta Comercial, para produção de efeitos perante terceiros, na forma admitida pela regulamentação aplicável.",
                "clausula_ciencia" => "Os signatários autorizam o arquivamento de ato que dê ciência da existência deste acordo perante a Junta Comercial, com indicação das partes, data e prazo, para produção de efeitos perante terceiros.",
                _ => "Os signatários deliberaram por não proceder ao arquivamento do presente acordo perante a Junta Comercial neste momento."
            };
            sb.AppendLine($"<p>{textoArq}</p>");

            if (dto.HasUsufruct)
                sb.AppendLine("<p><strong>Parágrafo único.</strong> Considerando que o presente acordo regula usufruto de quotas, o arquivamento é relevante para eficácia da disciplina perante terceiros.</p>");
        }

        // MOD.15 — Controvérsias
        if (modulos.Contains("15"))
        {
            clausulaNum++;
            sb.AppendLine($"<h2>CLÁUSULA {clausulaNum} — SOLUÇÃO DE CONTROVÉRSIAS</h2>");

            switch (dto.ModoControversia)
            {
                case "foro":
                    sb.AppendLine($"<p>Fica eleito o foro da Comarca de {dto.Comarca}, Estado de {dto.UfForo}, para dirimir as controvérsias oriundas deste acordo.</p>");
                    break;
                case "arbitragem":
                    sb.AppendLine($"<p>As controvérsias oriundas deste acordo serão resolvidas por arbitragem, administrada pela {dto.CamaraArbitral}, de acordo com suas regras vigentes.</p>");
                    break;
                case "mediacao_foro":
                    sb.AppendLine($"<p>As controvérsias serão submetidas previamente à mediação, no prazo de 30 dias, e, persistindo o impasse, ao foro da Comarca de {dto.Comarca}, Estado de {dto.UfForo}.</p>");
                    break;
                case "escalonada":
                    sb.AppendLine($"<p>As controvérsias serão submetidas previamente à mediação e, não havendo solução, resolvidas por arbitragem administrada pela {dto.CamaraArbitral}.</p>");
                    break;
            }
        }

        // Fecho
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var s in dto.Signatarios)
            sb.AppendLine($"<p>________________________________________<br/><strong>{s.Nome}</strong><br/><em>{s.Qualidade}</em></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = modulos.Select(m => $"DESTINO.ACQ.MOD.{m}").ToList(),
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(AcordoParametrizacaoDTO dto)
    {
        var r = new ValidationResult();

        if (!dto.Signatarios.Any())
            r.AddError("ACQ_001", "Deve haver ao menos dois signatários.");

        if (string.IsNullOrWhiteSpace(dto.Cnpj))
            r.AddError("ACQ_002", "CNPJ da sociedade é obrigatório.");

        foreach (var s in dto.Signatarios.Where(s => string.IsNullOrWhiteSpace(s.Cpf)))
            r.AddError("ACQ_003", $"Signatário {s.Nome} sem CPF.");

        // Parte disponível sem confirmação
        if (dto.NaturezaDoacao == "parte_disponivel" && !dto.ConfirmaLimiteParteDisponivel)
            r.AddError("ACQ_T03", "TRAVA: Parte disponível exige declaração expressa e validação de limite (arts. 548-549 CC).");

        // Usufruto sem voto definido
        if (dto.HasUsufruct && dto.ModoPolitico == "nao_definido")
            r.AddError("ACQ_T05", "TRAVA: Há usufruto mas regime de voto não foi definido. DREI exige disciplina clara.");

        // Usufruto misto sem detalhes
        if (dto.HasUsufruct && dto.ModoPolitico == "misto")
        {
            if (string.IsNullOrWhiteSpace(dto.VotoOrdinario))
                r.AddError("ACQ_T05B", "Regime misto: definir quem vota em matérias ordinárias.");
            if (string.IsNullOrWhiteSpace(dto.VotoExtraordinario))
                r.AddError("ACQ_T05C", "Regime misto: definir quem vota em matérias extraordinárias.");
        }

        // Econômico repartido sem fórmula
        if (dto.HasUsufruct && dto.ModoEconomico == "repartido" && string.IsNullOrWhiteSpace(dto.FormulaReparticao))
            r.AddError("ACQ_T06", "TRAVA: Repartição econômica sem fórmula definida.");

        // Multa vazia
        if (!dto.ValorMulta.HasValue || dto.ValorMulta <= 0)
            r.AddWarning("ACQ_W12", "Multa por descumprimento não definida. Sanção pode ficar decorativa.");

        // Controvérsias — arbitragem sem câmara
        if (dto.ModoControversia is "arbitragem" or "escalonada" && string.IsNullOrWhiteSpace(dto.CamaraArbitral))
            r.AddError("ACQ_T15", "TRAVA: Arbitragem sem câmara arbitral definida.");

        // Foro sem comarca
        if (dto.ModoControversia is "foro" or "mediacao_foro" && string.IsNullOrWhiteSpace(dto.Comarca))
            r.AddError("ACQ_T15B", "TRAVA: Foro sem comarca definida.");

        // Alerta forte: usufruto + sem arquivamento
        if (dto.HasUsufruct && dto.ModoArquivamento == "nenhum")
            r.AddWarning("ACQ_W14", "ALERTA FORTE: Acordo regula usufruto mas não será arquivado. DREI exige arquivamento para eficácia perante terceiros.");

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private static string ToRoman(int n) => n switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
        6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
        _ => n.ToString()
    };

    private static ResultadoValidacao ToRV(ValidationResult vr)
    {
        var r = new ResultadoValidacao();
        foreach (var m in vr.Messages.Where(m => m.Severity == ValidationSeverity.Error))
            r.Erros.Add(new ItemValidacao { Codigo = m.Code, Mensagem = m.Message, Severidade = SeveridadeValidacao.Bloqueio });
        foreach (var m in vr.Messages.Where(m => m.Severity == ValidationSeverity.Warning))
            r.Erros.Add(new ItemValidacao { Codigo = m.Code, Mensagem = m.Message, Severidade = SeveridadeValidacao.Alerta });
        return r;
    }

    private static string Fmt(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    private static string ExtN(int n) => $"{n} por extenso";
}
