#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 10.0 — Alteração Contratual da Veículo
// Compra das quotas ordinárias pela Destino (Fase 3 final)
//
// "Desloca a camada econômica ordinária para a Destino
//  e preserva a camada política na preferencial."
//
// 7 cláusulas + 4 travas duras
// Código: VEICULO.ALT_COMPRA_ORDINARIAS.V1
// ═══════════════════════════════════════════════════════════════

public class DiagnosticoCompraOrdinariasPelaDestino
{
    // ── Veículo ──────────────────────────────────────────
    public string VeiculoNomeEmpresarial { get; set; } = string.Empty;
    public string VeiculoCnpj { get; set; } = string.Empty;
    public string VeiculoNire { get; set; } = string.Empty;
    public string VeiculoUfJunta { get; set; } = string.Empty;
    public string VeiculoSede { get; set; } = string.Empty;
    public decimal VeiculoCapitalSocial { get; set; }
    public int ClausulaCapitalNumero { get; set; } = 4;

    // ── Destino (compradora) ─────────────────────────────
    public string DestinoNomeEmpresarial { get; set; } = string.Empty;
    public string DestinoCnpj { get; set; } = string.Empty;
    public string DestinoNire { get; set; } = string.Empty;
    public string DestinoSede { get; set; } = string.Empty;

    // ── Vendedores das ordinárias ─────────────────────────
    public List<VendedorOrdinarias> Vendedores { get; set; } = [];

    // ── Quotas ordinárias transferidas ────────────────────
    public int QuantidadeOrdinariasTransferidas { get; set; }
    public decimal ValorNominalUnitarioOrdinaria { get; set; } = 1.00m;
    public decimal PrecoTotal { get; set; }
    public string FormaPagamento { get; set; } = "à vista, pelo valor nominal";
    public bool OperacaoOnerosa { get; set; } = true;

    // ── Quota preferencial (permanece) ───────────────────
    public string PreferencialTitularNome { get; set; } = string.Empty;
    public int QuantidadePreferenciais { get; set; } = 1;
    public decimal ValorNominalPreferencial { get; set; } = 1_000.00m;
    public bool ConfirmaPreferencialForaDaOperacao { get; set; }

    // ── Confirmações ─────────────────────────────────────
    public bool RegenciaSupletivaAtiva { get; set; }
    public bool TemReservaCapitalAtiva { get; set; }

    // ── Derivados ────────────────────────────────────────
    public decimal ValorTotalOrdinarias => QuantidadeOrdinariasTransferidas * ValorNominalUnitarioOrdinaria;

    public string JuntaComercialNome => VeiculoUfJunta switch
    {
        "SP" => "Junta Comercial do Estado de São Paulo",
        "MG" => "Junta Comercial do Estado de Minas Gerais",
        "RJ" => "Junta Comercial do Estado do Rio de Janeiro",
        _ => $"Junta Comercial do Estado de {VeiculoUfJunta}"
    };
}

public class VendedorOrdinarias
{
    public string Nome { get; set; } = string.Empty;
    public int QuotasVendidas { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SERVIÇO
// ═══════════════════════════════════════════════════════════════

public class AlteracaoVeiculoDestinoService
{
    public ResultadoMontagem Montar(DiagnosticoCompraOrdinariasPelaDestino diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();

        // ── TÍTULO + PREÂMBULO ───────────────────────────
        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</h1>");
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios da sociedade empresária limitada <strong>{diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.VeiculoCnpj}, com sede em {diag.VeiculoSede}, registrada na {diag.JuntaComercialNome} sob NIRE {diag.VeiculoNire}, resolvem alterar o contrato social, nos seguintes termos:</p>");

        // ── CL.1 COMPRA E VENDA ─────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 1 — DA COMPRA E VENDA DAS QUOTAS ORDINÁRIAS</h2>");

        var vendedoresNomes = string.Join(" e ", diag.Vendedores.Select(v => $"<strong>{v.Nome}</strong>"));
        sb.AppendLine($"<p>Os sócios {vendedoresNomes}, titulares das quotas ordinárias da sociedade, declaram que vendem e transferem à sociedade <strong>{diag.DestinoNomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.DestinoCnpj}, com sede em {diag.DestinoSede}, registrada na Junta Comercial sob NIRE {diag.DestinoNire}, a titularidade de {diag.QuantidadeOrdinariasTransferidas} ({ExtN(diag.QuantidadeOrdinariasTransferidas)}) quotas ordinárias, no valor nominal total de {Fmt(diag.ValorTotalOrdinarias)}, pelo preço total de {Fmt(diag.PrecoTotal)}.</p>");

        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> A presente operação é celebrada em caráter {(diag.OperacaoOnerosa ? "oneroso" : "gratuito")}, e a alteração contratual ora firmada tem por objeto refletir, no quadro societário da sociedade, a nova titularidade das quotas ordinárias.</p>");
        sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> O preço e as condições de pagamento da operação são os seguintes: {diag.FormaPagamento}.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A presente transferência observa a arquitetura societária do projeto, em que a camada econômica ordinária da Veículo se desloca à célula Destino, sem transferência automática do núcleo político de controle.</p>");

        // ── CL.2 MANUTENÇÃO DA PREFERENCIAL ──────────────
        sb.AppendLine("<h2>CLÁUSULA 2 — DA MANUTENÇÃO DA QUOTA PREFERENCIAL</h2>");
        sb.AppendLine($"<p>Permanece de titularidade de <strong>{diag.PreferencialTitularNome}</strong> a {diag.QuantidadePreferenciais} ({ExtN(diag.QuantidadePreferenciais)}) quota(s) preferencial(is), no valor nominal de {Fmt(diag.ValorNominalPreferencial)}, com os direitos políticos reforçados e as prerrogativas estruturais já previstas no contrato social da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A transferência das quotas ordinárias ora refletida não importa cessão, modificação ou esvaziamento da quota preferencial.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Permanecem inalterados os direitos da quota preferencial quanto às matérias de controle, veto e preservação do comando político da estrutura, na forma do contrato social vigente.</p>");

        // ── CL.3 NOVA REDAÇÃO DO CAPITAL ─────────────────
        sb.AppendLine("<h2>CLÁUSULA 3 — DA NOVA REDAÇÃO DA CLÁUSULA DO CAPITAL SOCIAL E DA TITULARIDADE</h2>");
        sb.AppendLine($"<p>Em razão da operação acima, a cláusula {diag.ClausulaCapitalNumero}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote><p>\"O capital social da sociedade é de {Fmt(diag.VeiculoCapitalSocial)} ({Ext(diag.VeiculoCapitalSocial)}), dividido em {diag.QuantidadeOrdinariasTransferidas} ({ExtN(diag.QuantidadeOrdinariasTransferidas)}) quotas ordinárias, no valor nominal de {Fmt(diag.ValorNominalUnitarioOrdinaria)} cada, e {diag.QuantidadePreferenciais} ({ExtN(diag.QuantidadePreferenciais)}) quota(s) preferencial(is), no valor nominal de {Fmt(diag.ValorNominalPreferencial)}, distribuídas da seguinte forma:</p>");
        sb.AppendLine($"<p>• <strong>{diag.DestinoNomeEmpresarial.ToUpper()} LTDA.</strong>: {diag.QuantidadeOrdinariasTransferidas} quotas ordinárias, no valor total de {Fmt(diag.ValorTotalOrdinarias)};</p>");
        sb.AppendLine($"<p>• <strong>{diag.PreferencialTitularNome}</strong>: {diag.QuantidadePreferenciais} quota(s) preferencial(is), no valor total de {Fmt(diag.QuantidadePreferenciais * diag.ValorNominalPreferencial)}.\"</p>");
        sb.AppendLine("</blockquote>");

        // ── CL.4 ADESÃO DA DESTINO ──────────────────────
        sb.AppendLine("<h2>CLÁUSULA 4 — DA ADESÃO DA DESTINO AO CONTRATO SOCIAL DA VEÍCULO</h2>");
        sb.AppendLine($"<p>A sociedade <strong>{diag.DestinoNomeEmpresarial.ToUpper()} LTDA.</strong>, na qualidade de nova titular das quotas ordinárias, declara ciência e adesão ao contrato social da sociedade <strong>{diag.VeiculoNomeEmpresarial.ToUpper()} LTDA.</strong>, comprometendo-se a observar:</p>");
        sb.AppendLine("<p>I — a regência supletiva da Lei nº 6.404/1976, na forma já prevista no contrato social;</p>");
        sb.AppendLine("<p>II — a distinção entre quotas ordinárias e quota preferencial;</p>");
        sb.AppendLine("<p>III — as limitações de circulação, administração e reorganização interna previstas no contrato social;</p>");
        sb.AppendLine("<p>IV — a função da Veículo como célula de comando societário.</p>");

        // ── CL.5 MANUTENÇÃO DA FINALIDADE ────────────────
        sb.AppendLine("<h2>CLÁUSULA 5 — DA MANUTENÇÃO DA FINALIDADE DA VEÍCULO</h2>");
        sb.AppendLine("<p>Os sócios reafirmam que a sociedade permanece estruturada como célula de comando societário, destinada à concentração do controle político da arquitetura entre Veículo, Cofre e Destino, sem confusão com célula sucessória principal ou com célula patrimonial-operacional.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A entrada da Destino na titularidade das quotas ordinárias não converte a Veículo em célula sucessória direta, nem afasta a função da quota preferencial como núcleo político de controle.</p>");

        // ── CL.6 VINCULAÇÃO SISTÊMICA ────────────────────
        sb.AppendLine("<h2>CLÁUSULA 6 — DA VINCULAÇÃO COM OS ATOS REFLEXOS DA ESTRUTURA</h2>");
        sb.AppendLine("<p>As partes reconhecem que a presente alteração contratual integra estrutura societária mais ampla, devendo manter coerência com:</p>");
        sb.AppendLine("<p>I — a alteração contratual da célula Cofre que refletiu a entrada da Veículo em seu quadro societário;</p>");
        sb.AppendLine("<p>II — os instrumentos societários e sucessórios da célula Destino;</p>");
        sb.AppendLine("<p>III — o contrato social vigente da própria Veículo, especialmente quanto ao regime de classes de quotas e à quota preferencial.</p>");

        // ── CL.7 RATIFICAÇÃO ─────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 7 — RATIFICAÇÃO</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor todas as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");

        // ── FECHO ────────────────────────────────────────
        sb.AppendLine("<p>[Local], [data].</p>");
        var assinantes = diag.Vendedores.Select(v => v.Nome)
            .Append(diag.PreferencialTitularNome)
            .Append($"{diag.DestinoNomeEmpresarial} LTDA. (p/ representante legal)")
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
            BlocosCondicionaisAtivados = [diag.OperacaoOnerosa ? "ONEROSA" : "GRATUITA"],
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 4 travas
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoCompraOrdinariasPelaDestino diag)
    {
        var r = new ValidationResult();

        if (string.IsNullOrWhiteSpace(diag.VeiculoCnpj))
            r.AddError("AVO_001", "CNPJ da Veículo é obrigatório.");
        if (string.IsNullOrWhiteSpace(diag.VeiculoNire))
            r.AddError("AVO_002", "NIRE da Veículo é obrigatório.");
        if (string.IsNullOrWhiteSpace(diag.DestinoCnpj))
            r.AddError("AVO_003", "CNPJ da Destino é obrigatório.");
        if (!diag.Vendedores.Any())
            r.AddError("AVO_004", "Deve haver ao menos um vendedor.");
        if (diag.QuantidadeOrdinariasTransferidas <= 0)
            r.AddError("AVO_005", "Quantidade de ordinárias transferidas deve ser maior que zero.");
        if (diag.PrecoTotal <= 0 && diag.OperacaoOnerosa)
            r.AddError("AVO_006", "Operação onerosa com preço zero ou negativo.");

        // ── T1: Causa expressa ──────────────────────────
        // Onerosidade é o padrão (DREI), mas se gratuita, deve ser expressa
        if (!diag.OperacaoOnerosa)
            r.AddWarning("AVO_T1", "Operação marcada como gratuita. DREI presume onerosidade — confirme expressamente.");

        // ── T2: Preferencial fora da operação ───────────
        if (!diag.ConfirmaPreferencialForaDaOperacao)
            r.AddError("AVO_T2",
                "TRAVA: Sem confirmação de que a quota preferencial permanece fora da operação. " +
                "Incluir a preferencial destrói a lógica de comando da Veículo.");

        // ── T3: Regência supletiva ──────────────────────
        if (!diag.RegenciaSupletivaAtiva)
            r.AddError("AVO_T3",
                "TRAVA: Sem regência supletiva da Lei das S.A. ativa no contrato da Veículo, " +
                "a coexistência de ordinárias e preferencial não tem base registral defensável.");

        // ── T4: Reserva de capital sem módulo ───────────
        if (diag.PrecoTotal > diag.ValorTotalOrdinarias && !diag.TemReservaCapitalAtiva)
            r.AddWarning("AVO_T4",
                "Preço acima do valor nominal das ordinárias. Se houver excedente para reserva de capital, " +
                "o módulo correspondente deve estar ativo (art. 13, §2º, Lei 6.404).");

        // Checklist
        r.AddInfo("AVO_CK1", "Verificar se a Destino precisa refletir documentalmente a aquisição das ordinárias da Veículo.");
        r.AddInfo("AVO_CK2", "Conferir coerência global entre as três células após este ato.");

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
        r.ChecklistRegistral.AddRange(
            vr.Messages.Where(m => m.Severity == ValidationSeverity.Info).Select(m => m.Message));
        return r;
    }

    private static string Fmt(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    private static string Ext(decimal v) => $"{v:N2} por extenso";
    private static string ExtN(int n) => $"{n} por extenso";
}
