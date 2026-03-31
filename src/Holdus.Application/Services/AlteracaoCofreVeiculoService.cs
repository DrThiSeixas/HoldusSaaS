#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 9.0 — Alteração Contratual da Cofre
// Transferência de quotas à Veículo (Fase 3)
//
// "A alteração não finge que a titularidade mudou por geração
//  espontânea. Ela exige causa, reflete o quadro e preserva
//  a função da Cofre."
//
// 6 cláusulas + 4 travas duras
// Código: COFRE.ALT_TITULARIDADE_VEICULO.V1
// ═══════════════════════════════════════════════════════════════

public enum CausaJuridicaTransferencia
{
    NaoDefinida = 0,
    CessaoOnerosa = 1,
    CessaoGratuita = 2,
    ConferenciaCapitalVeiculo = 3
}

public class DiagnosticoTransferenciaCofreVeiculo
{
    // ── Cofre ────────────────────────────────────────────
    public string CofreNomeEmpresarial { get; set; } = string.Empty;
    public string CofreCnpj { get; set; } = string.Empty;
    public string CofreNire { get; set; } = string.Empty;
    public string CofreUfJunta { get; set; } = string.Empty;
    public string CofreSede { get; set; } = string.Empty;
    public decimal CofreCapitalSocial { get; set; }
    public decimal CofreValorPorQuota { get; set; } = 1.00m;
    public int ClausulaCapitalNumero { get; set; } = 3;

    // ── Veículo (ingressante) ────────────────────────────
    public string VeiculoNomeEmpresarial { get; set; } = string.Empty;
    public string VeiculoCnpj { get; set; } = string.Empty;
    public string VeiculoNire { get; set; } = string.Empty;
    public string VeiculoSede { get; set; } = string.Empty;

    // ── Transferência ────────────────────────────────────
    public CausaJuridicaTransferencia CausaJuridica { get; set; } = CausaJuridicaTransferencia.NaoDefinida;
    public List<TransferenteCofre> Transferentes { get; set; } = [];
    public int QuantidadeQuotasTransferidas { get; set; }
    public decimal PercentualTransferido { get; set; }
    public bool TransferenciaTotal { get; set; }
    public string? InstrumentoVinculante { get; set; }

    // ── Quadro pós-alteração ─────────────────────────────
    public List<SocioQuadroCofre> QuadroPosAlteracao { get; set; } = [];

    // ── Confirmações ─────────────────────────────────────
    public bool AtoCorrelatoVeiculoExiste { get; set; }
    public bool ObjetoCofrenaoAlterado { get; set; } = true;

    // ── Derivados ────────────────────────────────────────
    public int TotalQuotasPosAlteracao => QuadroPosAlteracao.Sum(s => s.Quotas);

    public string JuntaComercialNome => CofreUfJunta switch
    {
        "SP" => "Junta Comercial do Estado de São Paulo",
        "MG" => "Junta Comercial do Estado de Minas Gerais",
        "RJ" => "Junta Comercial do Estado do Rio de Janeiro",
        _ => $"Junta Comercial do Estado de {CofreUfJunta}"
    };
}

public class TransferenteCofre
{
    public string Nome { get; set; } = string.Empty;
    public int QuotasCedidas { get; set; }
}

public class SocioQuadroCofre
{
    public string Nome { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public bool EhPessoaJuridica { get; set; }
    public int Quotas { get; set; }
    public decimal Percentual { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SERVIÇO
// ═══════════════════════════════════════════════════════════════

public class AlteracaoCofreVeiculoService
{
    public ResultadoMontagem Montar(DiagnosticoTransferenciaCofreVeiculo diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();

        // ── TÍTULO ───────────────────────────────────────
        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.CofreNomeEmpresarial.ToUpper()} LTDA.</h1>");

        // ── PREÂMBULO ────────────────────────────────────
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios da sociedade empresária limitada <strong>{diag.CofreNomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.CofreCnpj}, com sede em {diag.CofreSede}, registrada na {diag.JuntaComercialNome} sob NIRE {diag.CofreNire}, resolvem alterar o contrato social, nos seguintes termos:</p>");

        // ── CL.1 TRANSFERÊNCIA ───────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 1 — DA TRANSFERÊNCIA DA TITULARIDADE DAS QUOTAS</h2>");

        var nomesTransferentes = string.Join(" e ", diag.Transferentes.Select(t => $"<strong>{t.Nome}</strong>"));
        var causaTexto = diag.CausaJuridica switch
        {
            CausaJuridicaTransferencia.CessaoOnerosa => "instrumento de cessão onerosa",
            CausaJuridicaTransferencia.CessaoGratuita => "instrumento de cessão gratuita",
            CausaJuridicaTransferencia.ConferenciaCapitalVeiculo => "conferência de quotas para integralização do capital social da sociedade Veículo",
            _ => "[causa jurídica a definir]"
        };

        sb.AppendLine($"<p>Os sócios {nomesTransferentes}, titulares originários das quotas da sociedade, declaram que, por força de {causaTexto}, transferiram à sociedade <strong>{diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.VeiculoCnpj}, com sede em {diag.VeiculoSede}, registrada na Junta Comercial sob NIRE {diag.VeiculoNire}, a titularidade de {diag.QuantidadeQuotasTransferidas} ({ExtN(diag.QuantidadeQuotasTransferidas)}) quotas, representativas de {diag.PercentualTransferido:F2}% do capital social da presente sociedade.</p>");

        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A presente alteração contratual tem por objeto refletir, no quadro societário da sociedade, a nova titularidade das quotas em decorrência do ato jurídico acima identificado.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A causa jurídica da transferência consta expressamente do instrumento vinculado a este ato, vedada a geração da presente alteração sem a correspondente identificação do título transmissivo.</p>");

        // ── CL.2 NOVA COMPOSIÇÃO ─────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 2 — DA NOVA COMPOSIÇÃO SOCIETÁRIA</h2>");
        sb.AppendLine($"<p>Em razão da operação acima, a cláusula {diag.ClausulaCapitalNumero}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote><p>\"O capital social da sociedade é de {Fmt(diag.CofreCapitalSocial)} ({Ext(diag.CofreCapitalSocial)}), dividido em {diag.TotalQuotasPosAlteracao} ({ExtN(diag.TotalQuotasPosAlteracao)}) quotas, no valor nominal de {Fmt(diag.CofreValorPorQuota)} cada uma, totalmente subscritas e integralizadas, distribuídas da seguinte forma:</p>");

        foreach (var s in diag.QuadroPosAlteracao)
        {
            var id = s.EhPessoaJuridica ? $"{s.Nome}, CNPJ nº {s.Cnpj}" : s.Nome;
            var vt = s.Quotas * diag.CofreValorPorQuota;
            sb.AppendLine($"<p>• <strong>{id}</strong>: {s.Quotas} ({ExtN(s.Quotas)}) quotas, no valor total de {Fmt(vt)}, correspondentes a {s.Percentual:F2}% do capital social;\"</p>");
        }
        sb.AppendLine("</blockquote>");

        // ── CL.3 ADESÃO DA VEÍCULO ──────────────────────
        sb.AppendLine("<h2>CLÁUSULA 3 — DA ADESÃO DA VEÍCULO AO CONTRATO SOCIAL DA COFRE</h2>");
        sb.AppendLine($"<p>A sociedade <strong>{diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</strong>, na qualidade de nova titular das quotas acima descritas, declara ciência e adesão integral ao contrato social da sociedade <strong>{diag.CofreNomeEmpresarial.ToUpper()} LTDA.</strong>, comprometendo-se a observar sua finalidade patrimonial-societária, seu objeto social, suas regras de administração e suas limitações estruturais.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A entrada da Veículo no quadro societário da Cofre não altera, por si só, a natureza da Cofre como célula patrimonial-societária de preservação, mantidas as travas já fixadas na sua arquitetura documental.</p>");

        // ── CL.4 MANUTENÇÃO DA FINALIDADE ────────────────
        sb.AppendLine("<h2>CLÁUSULA 4 — DA MANUTENÇÃO DA FINALIDADE DA COFRE</h2>");
        sb.AppendLine("<p>Os sócios reafirmam que a sociedade permanece estruturada como célula Cofre, com função patrimonial-societária de preservação e concentração de participações, sem exercício de atividade operacional própria perante terceiros.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A presente reorganização societária não autoriza desvio funcional da célula Cofre, nem amplia seu objeto social para atividades operacionais, locação, compra e venda habitual de imóveis ou prestação de serviços.</p>");

        // ── CL.5 VINCULAÇÃO SISTÊMICA ────────────────────
        sb.AppendLine("<h2>CLÁUSULA 5 — DA VINCULAÇÃO SISTÊMICA COM A VEÍCULO</h2>");
        sb.AppendLine($"<p>As partes reconhecem que a presente alteração integra estrutura societária mais ampla, em que a sociedade <strong>{diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</strong> exerce função de comando societário e reorganização do controle, sem prejuízo da necessária coerência entre os atos desta sociedade, da Cofre e da célula Destino.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A eficácia estrutural da presente alteração depende da compatibilidade com os atos correlatos praticados na Veículo e, quando cabível, na Destino, conforme a modelagem global do caso.</p>");

        // ── CL.6 RATIFICAÇÃO ─────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 6 — RATIFICAÇÃO</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor todas as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");

        // ── FECHO ────────────────────────────────────────
        sb.AppendLine("<p>[Local], [data].</p>");
        var assinantes = diag.Transferentes.Select(t => t.Nome)
            .Concat(diag.QuadroPosAlteracao.Select(s => s.Nome))
            .Distinct();
        foreach (var nome in assinantes)
            sb.AppendLine($"<p>________________________________________<br/><strong>{nome}</strong></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = [$"CAUSA-{diag.CausaJuridica}"],
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 4 travas
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoTransferenciaCofreVeiculo diag)
    {
        var r = new ValidationResult();

        if (string.IsNullOrWhiteSpace(diag.CofreCnpj))
            r.AddError("ACV_001", "CNPJ da Cofre é obrigatório.");
        if (string.IsNullOrWhiteSpace(diag.CofreNire))
            r.AddError("ACV_002", "NIRE da Cofre é obrigatório para alteração.");
        if (string.IsNullOrWhiteSpace(diag.VeiculoCnpj))
            r.AddError("ACV_003", "CNPJ da Veículo é obrigatório.");
        if (!diag.Transferentes.Any())
            r.AddError("ACV_004", "Deve haver ao menos um transferente.");
        if (diag.QuantidadeQuotasTransferidas <= 0)
            r.AddError("ACV_005", "Quantidade de quotas transferidas deve ser maior que zero.");
        if (!diag.QuadroPosAlteracao.Any())
            r.AddError("ACV_006", "Quadro societário pós-alteração é obrigatório.");

        var totalPct = diag.QuadroPosAlteracao.Sum(s => s.Percentual);
        if (diag.QuadroPosAlteracao.Any() && Math.Abs(totalPct - 100m) > 0.01m)
            r.AddError("ACV_007", $"Percentuais pós-alteração somam {totalPct:F2}%. Devem somar 100%.");

        // ── T1: Sem causa jurídica ──────────────────────
        if (diag.CausaJuridica == CausaJuridicaTransferencia.NaoDefinida)
            r.AddError("ACV_T1",
                "TRAVA: Sem definição expressa da causa jurídica da transferência " +
                "(onerosa, gratuita ou conferência ao capital da Veículo), o documento não gera. " +
                "DREI: transferência presumidamente onerosa, salvo menção expressa de gratuidade.");

        // ── T2: Via instrumento autônomo (alerta) ────────
        if (string.IsNullOrWhiteSpace(diag.InstrumentoVinculante))
            r.AddWarning("ACV_T2",
                "Sem instrumento vinculante identificado. O DREI admite cessão por instrumento autônomo " +
                "com averbação, mas no método Tríade a via preferencial é alteração contratual reflexa.");

        // ── T3: Sem ato correlato na Veículo ─────────────
        if (!diag.AtoCorrelatoVeiculoExiste)
            r.AddError("ACV_T3",
                "TRAVA: Sem ato correlato na Veículo, a alteração da Cofre fica estruturalmente incompleta.");

        // ── T4: Alargamento do objeto da Cofre ───────────
        if (!diag.ObjetoCofrenaoAlterado)
            r.AddError("ACV_T4",
                "TRAVA: Tentativa de alargar o objeto da Cofre na mesma operação. " +
                "Isso destrói a lógica da célula Cofre como patrimonial-societária pura.");

        return r;
    }

    // ═══════════════════════════════════════════════════════════

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
    private static string Ext(decimal v) => $"{v:N2} por extenso";
    private static string ExtN(int n) => $"{n} por extenso";
}
