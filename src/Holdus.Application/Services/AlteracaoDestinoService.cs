#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 6.0 — Alteração Contratual da Destino
// Reflexo societário da doação de quotas (Minuta 5.0)
//
// "Não deixar a doação solta. Ela precisa ter reflexo
//  societário imediato na Destino."
//
// 8 cláusulas + 4 blocos condicionais + 4 travas
// Código: DESTINO.ALT_DOACAO.V1
// ═══════════════════════════════════════════════════════════════

public class DiagnosticoAlteracaoDestino
{
    // ── Sociedade ────────────────────────────────────────
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Nire { get; set; } = string.Empty;
    public string UfJunta { get; set; } = string.Empty;
    public string Sede { get; set; } = string.Empty;
    public string Comarca { get; set; } = string.Empty;
    public decimal CapitalSocial { get; set; }
    public decimal ValorPorQuota { get; set; } = 1.00m;
    public int ClausulaCapitalNumero { get; set; } = 3;

    // ── Doadores (cedentes) ──────────────────────────────
    public List<ParteDoacaoAlteracao> Doadores { get; set; } = [];

    // ── Donatários (ingressantes) ────────────────────────
    public List<ParteDoacaoAlteracao> Donatarios { get; set; } = [];

    // ── Quadro pós-alteração (todos os sócios) ───────────
    public List<SocioQuadroDestino> QuadroPosAlteracao { get; set; } = [];

    // ── Natureza (BC-AD1) ────────────────────────────────
    public NaturezaDoacao Natureza { get; set; } = NaturezaDoacao.AdiantamentoLegitima;

    // ── Usufruto (BC-AD2) ────────────────────────────────
    public bool ReservaUsufruto { get; set; }
    public string? UsufrutuarioNome { get; set; }
    public DireitosUsufruto? DireitosDoUsufruto { get; set; }

    // ── Restrições (BC-AD3) ──────────────────────────────
    public ClausulasRestritivas Restricoes { get; set; } = ClausulasRestritivas.Nenhuma;

    // ── Acordo (BC-AD4) ──────────────────────────────────
    public bool AcordoQuotistasVinculado { get; set; }

    // ── Confirmações (travas) ────────────────────────────
    public bool InstrumentoDoacaoVinculado { get; set; }
    public bool ConfirmaLimiteParteDisponivel { get; set; }
    public bool DonatariosAderemAoCS { get; set; }

    // ── Derivados ────────────────────────────────────────
    public int TotalQuotas => QuadroPosAlteracao.Sum(s => s.Quotas);
    public bool TemUsufruto => ReservaUsufruto;
    public bool TemRestricoes => Restricoes != ClausulasRestritivas.Nenhuma;
    public bool EhParteDisponivel => Natureza == NaturezaDoacao.ParteDisponivelComDispensa;

    public string JuntaComercialNome => UfJunta switch
    {
        "SP" => "Junta Comercial do Estado de São Paulo",
        "MG" => "Junta Comercial do Estado de Minas Gerais",
        "RJ" => "Junta Comercial do Estado do Rio de Janeiro",
        _ => $"Junta Comercial do Estado de {UfJunta}"
    };
}

public class ParteDoacaoAlteracao
{
    public string Nome { get; set; } = string.Empty;
    public int QuotasCedidas { get; set; }
    public decimal ValorNominalTotal { get; set; }
}

public class SocioQuadroDestino
{
    public string Nome { get; set; } = string.Empty;
    public int Quotas { get; set; }
    public decimal Percentual { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SERVIÇO
// ═══════════════════════════════════════════════════════════════

public class AlteracaoDestinoService
{
    public ResultadoMontagem Montar(DiagnosticoAlteracaoDestino diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();

        // ── TÍTULO ───────────────────────────────────────
        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.NomeEmpresarial.ToUpper()} LTDA.</h1>");

        // ── PREÂMBULO ────────────────────────────────────
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios da sociedade empresária limitada <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cnpj}, com sede em {diag.Sede}, registrada na {diag.JuntaComercialNome} sob NIRE {diag.Nire}, resolvem alterar o contrato social, nos seguintes termos:</p>");

        // ── CL.1 — DOAÇÃO E REORGANIZAÇÃO ────────────────
        sb.AppendLine("<h2>CLÁUSULA 1 — DA DOAÇÃO DE QUOTAS E DA REORGANIZAÇÃO DA TITULARIDADE</h2>");

        var doadorNomes = string.Join(" e ", diag.Doadores.Select(d => $"<strong>{d.Nome}</strong>"));
        sb.AppendLine($"<p>Os sócios {doadorNomes}, na qualidade de titulares originários das quotas da sociedade, declaram que promoveram a doação de quotas de sua titularidade aos seguintes donatários:</p>");

        foreach (var dn in diag.Donatarios)
            sb.AppendLine($"<p>• <strong>{dn.Nome}</strong>, que recebe {dn.QuotasCedidas} ({ExtN(dn.QuotasCedidas)}) quotas, no valor nominal total de {Fmt(dn.ValorNominalTotal)};</p>");

        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A doação acima referida foi formalizada por instrumento próprio, ao qual esta alteração contratual se vincula para todos os fins societários, patrimoniais e registrais.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A presente alteração tem por objeto refletir, no quadro societário da sociedade, a nova titularidade das quotas em decorrência da liberalidade praticada.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A operação observará a legislação civil, societária e tributária aplicável, inclusive quanto à eficácia perante a sociedade e terceiros.</p>");

        // ── CL.2 — NATUREZA SUCESSÓRIA (BC-AD1) ─────────
        sb.AppendLine("<h2>CLÁUSULA 2 — DA NATUREZA SUCESSÓRIA DA OPERAÇÃO</h2>");
        sb.AppendLine("<p>A presente reorganização societária decorre de operação de planejamento sucessório familiar, devendo a doação ser qualificada no instrumento próprio como:</p>");

        if (diag.EhParteDisponivel)
            sb.AppendLine("<p>liberalidade imputada à parte disponível do patrimônio do(s) doador(es), com dispensa de colação, nos limites legais.</p>");
        else
            sb.AppendLine("<p>adiantamento do que cabe ao(s) donatário(s) por herança.</p>");

        sb.AppendLine("<p><strong>Parágrafo único.</strong> A presente alteração contratual não substitui as declarações materiais do instrumento de doação quanto à legítima, colação, parte disponível, subsistência do doador e demais efeitos civis da liberalidade.</p>");

        // ── CL.3 — NOVA REDAÇÃO DO CAPITAL ───────────────
        sb.AppendLine("<h2>CLÁUSULA 3 — DA NOVA REDAÇÃO DA CLÁUSULA DO CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>Em razão da doação refletida neste ato, a cláusula {diag.ClausulaCapitalNumero}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote><p>\"O capital social da sociedade é de {Fmt(diag.CapitalSocial)} ({Ext(diag.CapitalSocial)}), dividido em {diag.TotalQuotas} ({ExtN(diag.TotalQuotas)}) quotas, no valor nominal de {Fmt(diag.ValorPorQuota)} cada uma, totalmente subscritas e integralizadas, distribuídas da seguinte forma:</p>");

        foreach (var s in diag.QuadroPosAlteracao)
        {
            var vt = s.Quotas * diag.ValorPorQuota;
            sb.AppendLine($"<p>• <strong>{s.Nome}</strong>: {s.Quotas} ({ExtN(s.Quotas)}) quotas, no valor total de {Fmt(vt)};\"</p>");
        }
        sb.AppendLine("</blockquote>");

        // ── CL.4 — ADESÃO DOS NOVOS QUOTISTAS (BC-AD4) ──
        sb.AppendLine("<h2>CLÁUSULA 4 — DA ADESÃO DOS NOVOS QUOTISTAS</h2>");
        sb.AppendLine("<p>Os donatários que passam a integrar o quadro societário da sociedade declaram, neste ato, plena ciência e adesão:</p>");
        sb.AppendLine("<p>I — ao contrato social da sociedade;</p>");
        sb.AppendLine("<p>II — à finalidade patrimonial-societária e sucessória da célula Destino;</p>");

        if (diag.AcordoQuotistasVinculado)
            sb.AppendLine("<p>III — ao acordo de quotistas, protocolo familiar ou instrumento complementar existente, ao qual aderem formalmente neste ato;</p>");
        else
            sb.AppendLine("<p>III — ao eventual acordo de quotistas, protocolo familiar ou instrumento complementar que vier a ser celebrado;</p>");

        sb.AppendLine("<p>IV — às regras de administração, deliberação, circulação de quotas e governança familiar e societária.</p>");

        // ── CL.5 — USUFRUTO (BC-AD2) ────────────────────
        if (diag.TemUsufruto)
        {
            sb.AppendLine("<h2>CLÁUSULA 5 — DO USUFRUTO SOBRE AS QUOTAS</h2>");
            sb.AppendLine($"<p>As quotas objeto da doação permanecem gravadas com usufruto em favor de <strong>{diag.UsufrutuarioNome}</strong>, na forma do instrumento próprio de doação/usufruto.</p>");
            sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A disciplina dos direitos políticos e econômicos relacionados às quotas gravadas observará o contrato social, o instrumento de doação/usufruto e, se houver, o acordo de quotistas.</p>");
            sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Quando o usufruto for regulado em acordo de sócios ou outro instrumento parassocial, seu arquivamento é relevante para eficácia perante terceiros.</p>");
        }

        // ── CL.6 — RESTRIÇÕES (BC-AD3) ──────────────────
        if (diag.TemRestricoes)
        {
            sb.AppendLine("<h2>CLÁUSULA 6 — DAS RESTRIÇÕES INCIDENTES SOBRE AS QUOTAS</h2>");
            sb.AppendLine("<p>As quotas recebidas pelos donatários permanecem sujeitas, quando previsto no instrumento próprio, às cláusulas de:</p>");

            if (diag.Restricoes.HasFlag(ClausulasRestritivas.Incomunicabilidade))
                sb.AppendLine("<p>I — incomunicabilidade;</p>");
            if (diag.Restricoes.HasFlag(ClausulasRestritivas.Impenhorabilidade))
                sb.AppendLine("<p>II — impenhorabilidade;</p>");
            if (diag.Restricoes.HasFlag(ClausulasRestritivas.Inalienabilidade))
                sb.AppendLine("<p>III — inalienabilidade;</p>");
            if (diag.Restricoes.HasFlag(ClausulasRestritivas.Reversao))
                sb.AppendLine("<p>IV — reversão.</p>");
        }

        // ── CL.7 — MANUTENÇÃO DA FINALIDADE ─────────────
        sb.AppendLine("<h2>CLÁUSULA 7 — DA MANUTENÇÃO DA FINALIDADE DA CÉLULA DESTINO</h2>");
        sb.AppendLine("<p>Os sócios reafirmam que a sociedade permanece estruturada como célula de transferência patrimonial por quotas, voltada à organização da sucessão familiar e à acomodação societária dos sucessores, sem prejuízo de instrumentos complementares de governança.</p>");

        // ── CL.8 — RATIFICAÇÃO ───────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 8 — DA RATIFICAÇÃO DAS DEMAIS CLÁUSULAS</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");

        // ── FECHO ────────────────────────────────────────
        sb.AppendLine("<p>[Local], [data].</p>");

        // Assinam todos: doadores + donatários
        var todosAssinantes = diag.Doadores.Select(d => d.Nome)
            .Concat(diag.Donatarios.Select(d => d.Nome))
            .Distinct();

        foreach (var nome in todosAssinantes)
            sb.AppendLine($"<p>________________________________________<br/><strong>{nome}</strong></p>");

        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        // Blocos condicionais ativados
        var blocos = new List<string>();
        blocos.Add(diag.EhParteDisponivel ? "BC-AD1-ParteDisponivel" : "BC-AD1-AdiantamentoLegitima");
        if (diag.TemUsufruto) blocos.Add("BC-AD2-Usufruto");
        if (diag.TemRestricoes) blocos.Add("BC-AD3-Restricoes");
        if (diag.AcordoQuotistasVinculado) blocos.Add("BC-AD4-AcordoVinculado");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = blocos,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 4 travas
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoAlteracaoDestino diag)
    {
        var r = new ValidationResult();

        if (!diag.Doadores.Any())
            r.AddError("ADT_001", "Deve haver ao menos um doador.");
        if (!diag.Donatarios.Any())
            r.AddError("ADT_002", "Deve haver ao menos um donatário.");
        if (string.IsNullOrWhiteSpace(diag.Cnpj))
            r.AddError("ADT_003", "O CNPJ da sociedade é obrigatório.");
        if (string.IsNullOrWhiteSpace(diag.Nire))
            r.AddError("ADT_004", "O NIRE da sociedade é obrigatório para alteração contratual.");
        if (!diag.QuadroPosAlteracao.Any())
            r.AddError("ADT_005", "O quadro societário pós-alteração deve ser informado.");

        var totalPct = diag.QuadroPosAlteracao.Sum(s => s.Percentual);
        if (diag.QuadroPosAlteracao.Any() && Math.Abs(totalPct - 100m) > 0.01m)
            r.AddError("ADT_006", $"Os percentuais pós-alteração somam {totalPct:F2}%. Devem somar 100%.");

        // ── T1: Sem instrumento de doação vinculado ──────
        if (!diag.InstrumentoDoacaoVinculado)
        {
            r.AddError("ADT_T1",
                "TRAVA: Sem instrumento de doação vinculado, a alteração contratual não pode ser gerada. A doação precisa ter reflexo societário, mas o reflexo não substitui a doação.");
        }

        // ── T2: Usufruto sem disciplina de voto ─────────
        if (diag.ReservaUsufruto && !diag.DireitosDoUsufruto.HasValue)
        {
            r.AddError("ADT_T2",
                "TRAVA: Usufruto sem disciplina expressa de voto e direitos econômicos. O DREI exige regulação clara para eficácia perante terceiros.");
        }

        // ── T3: Parte disponível sem declaração ─────────
        if (diag.EhParteDisponivel && !diag.ConfirmaLimiteParteDisponivel)
        {
            r.AddError("ADT_T3",
                "TRAVA: Doação à conta da parte disponível exige declaração expressa e validação de limite legal (arts. 548-549 CC).");
        }

        // ── T4: Donatários não aderiram ─────────────────
        if (!diag.DonatariosAderemAoCS)
        {
            r.AddError("ADT_T4",
                "TRAVA: Os novos quotistas devem declarar adesão ao contrato social e, se houver, ao acordo de quotistas.");
        }

        // Checklist
        r.AddInfo("ADT_CK1", "Recolhimento do ITCMD obrigatório.");
        r.AddInfo("ADT_CK2", "Arquivamento na Junta Comercial obrigatório.");
        if (diag.TemUsufruto)
            r.AddInfo("ADT_CK3", "Arquivamento do instrumento de usufruto na Junta (quando parassocial) para eficácia perante terceiros.");

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private static ResultadoValidacao ToRV(ValidationResult vr)
    {
        var r = new ResultadoValidacao();
        foreach (var m in vr.Messages.Where(m => m.Severity == ValidationSeverity.Error))
            r.Erros.Add(new ItemValidacao { Codigo = m.Code, Mensagem = m.Message, Severidade = SeveridadeValidacao.Bloqueio });
        foreach (var m in vr.Messages.Where(m => m.Severity == ValidationSeverity.Warning))
            r.Erros.Add(new ItemValidacao { Codigo = m.Code, Mensagem = m.Message, Severidade = SeveridadeValidacao.Alerta });
        r.ChecklistRegistral.AddRange(
            vr.Messages.Where(m => m.Severity == ValidationSeverity.Info).Select(m => m.Message));
        return r;
    }

    private static string Fmt(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    private static string Ext(decimal v) => $"{v:N2} por extenso";
    private static string ExtN(int n) => $"{n} por extenso";
}
