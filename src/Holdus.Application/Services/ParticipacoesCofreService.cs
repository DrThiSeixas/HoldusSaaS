using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using System.Text;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 3.0 — Integralização com Participações Societárias
// Validação + Montagem de até 3 atos simultâneos
// ═══════════════════════════════════════════════════════════════

public class ParticipacoesCofreService
{
    public ResultadoMontagemParticipacoes Montar(DiagnosticoParticipacoesCofre diag)
    {
        var validacao = Validar(diag);
        if (!validacao.PodeGerar)
            return new ResultadoMontagemParticipacoes { Sucesso = false, Validacao = validacao };

        var resultado = new ResultadoMontagemParticipacoes
        {
            Sucesso = true,
            Validacao = validacao,
            BlocosCondicionaisAtivados = IdentificarBlocos(diag),
        };

        // Ato 1: investida (somente se quotas de Ltda)
        if (diag.DeveGerarAtoInvestida)
            resultado.HtmlAtoInvestida = MontarAtoInvestida(diag);

        // Ato 2: Cofre (sempre)
        resultado.HtmlAtoCofre = MontarAtoCofre(diag);

        // Ato 3: checklist S.A. (somente se ações)
        if (diag.DeveGerarChecklistSA)
            resultado.ChecklistSA = GerarChecklistSA(diag);

        return resultado;
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 4 bloqueios + 3 alertas
    // ═══════════════════════════════════════════════════════════

    private ResultadoValidacao Validar(DiagnosticoParticipacoesCofre diag)
    {
        var r = new ResultadoValidacao();

        // ── B1: Capital não integralizado na investida ──────
        if (!diag.ConfirmaCapitalIntegralizado)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B1",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "O capital objeto da operação na sociedade investida não está confirmado como totalmente integralizado. O DREI exige integralização total do capital objeto da operação para esta modalidade.",
                FundamentoLegal = "Manual DREI — IN DREI nº 81/2020",
                Sugestao = "Confirmar que o capital da investida está totalmente integralizado antes de prosseguir."
            });
        }

        // ── B2: Ausência do ato reflexo ─────────────────────
        if (diag.EhQuotasLtda && diag.Investida.Cnpj == string.Empty)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B2",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "A integralização com quotas de Ltda exige alteração contratual reflexa na sociedade investida. Os dados da investida estão incompletos — sem eles, o ato reflexo não pode ser gerado.",
                FundamentoLegal = "Manual DREI — integralização com quotas de outra sociedade",
                Sugestao = "Preencher todos os dados da sociedade investida (nome, CNPJ, NIRE, sede)."
            });
        }

        // ── B3: Desvio funcional do Cofre ───────────────────
        if (diag.CofrePuro && diag.AtividadeOperacionalPropria)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B3",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "DESVIO FUNCIONAL: O Cofre está marcado como puro mas possui atividade operacional própria. Uma holding de participações (CNAE 6462-0/00) não opera — ela concentra participações.",
                FundamentoLegal = "CNAE 6462-0/00 + Manual DREI",
                Sugestao = "Reclassificar a célula ou remover a atividade operacional."
            });
        }

        // ── B4: Dados incompletos da investida ──────────────
        if (diag.EhQuotasLtda)
        {
            var faltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(diag.Investida.NomeEmpresarial)) faltantes.Add("nome empresarial");
            if (string.IsNullOrWhiteSpace(diag.Investida.Cnpj)) faltantes.Add("CNPJ");
            if (string.IsNullOrWhiteSpace(diag.Investida.Nire)) faltantes.Add("NIRE");

            if (faltantes.Any())
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "B4",
                    Severidade = SeveridadeValidacao.Bloqueio,
                    Mensagem = $"Dados incompletos da sociedade investida: falta {string.Join(", ", faltantes)}. O DREI exige identificação completa da investida no ato de integralização.",
                    Sugestao = "Preencher todos os dados da sociedade investida."
                });
            }
        }

        // ── A1: Mesma UF — tramitação conjunta ──────────────
        if (diag.MesmaUf)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "A1",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = $"As sociedades estão na mesma UF ({diag.Cofre.Uf}). Os processos devem tramitar conjuntamente na Junta Comercial.",
                FundamentoLegal = "Manual DREI — tramitação conjunta",
                Sugestao = "Protocolar os dois atos (investida + Cofre) simultaneamente."
            });
        }

        // ── A2: Restrições contratuais ──────────────────────
        if (!diag.ConfirmaSemRestricoesTransferencia)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "A2",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = "Não foi confirmada a ausência de restrições contratuais à cessão/transferência de quotas na sociedade investida. Verificar se o contrato social da investida exige anuência de outros sócios ou impõe direito de preferência.",
                Sugestao = "Analisar o contrato social da investida antes de prosseguir."
            });
        }

        // ── A3: Percentuais pós-aumento ─────────────────────
        var totalPctCofre = diag.QuadroCofre.Sum(s => s.PercentualPosAumento);
        if (Math.Abs(totalPctCofre - 100m) > 0.01m)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "A3",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = $"Os percentuais de participação no Cofre pós-aumento somam {totalPctCofre:F2}%. Devem somar 100%.",
            });
        }

        // ── Checklist registral ─────────────────────────────
        r.ChecklistRegistral.Add("Alteração contratual do Cofre assinada por todos os sócios");
        r.ChecklistRegistral.Add("Duas testemunhas em cada ato");
        r.ChecklistRegistral.Add("Capa padrão JUCESP/DREI para cada ato");

        if (diag.DeveGerarAtoInvestida)
        {
            r.ChecklistRegistral.Add("Alteração contratual da investida assinada por todos os sócios da investida");
            if (diag.MesmaUf)
                r.ChecklistRegistral.Add("Tramitação conjunta obrigatória (mesma UF)");
            else
                r.ChecklistRegistral.Add("Protocolar ato da investida na Junta Comercial da UF correspondente");
        }

        if (diag.DeveGerarChecklistSA)
        {
            r.ChecklistRegistral.Add("Averbação no Livro de Registro de Ações Nominativas");
            r.ChecklistRegistral.Add("Averbação no Livro de Transferência de Ações Nominativas");
            r.ChecklistRegistral.Add("Comunicação à administração da companhia");
        }

        r.ChecklistRegistral.Add("DBE de alteração de dados cadastrais");
        r.ChecklistRegistral.Add("Pagamento DARE da Junta Comercial");

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // ATO 1 — ALTERAÇÃO DA INVESTIDA
    // ═══════════════════════════════════════════════════════════

    private string MontarAtoInvestida(DiagnosticoParticipacoesCofre diag)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.Investida.NomeEmpresarial.ToUpper()} LTDA.</h1>");
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios de <strong>{diag.Investida.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Investida.Cnpj}, com sede em {diag.Investida.Endereco.Completo}, registrada na {diag.Investida.JuntaComercialNome} sob NIRE {diag.Investida.Nire}, resolvem alterar o contrato social, nos seguintes termos:</p>");

        // ── BC-P2: Total vs Parcial ──────────────────────────
        if (diag.UsoTotalOuParcial == UsoParticipacao.Total)
        {
            sb.AppendLine("<h2>CLÁUSULA ÚNICA — TRANSFERÊNCIA DE PARTICIPAÇÃO PARA INTEGRALIZAÇÃO EM OUTRA SOCIEDADE</h2>");
            sb.AppendLine($"<p>Os sócios resolvem registrar que o sócio <strong>{diag.Conferente.Nome}</strong> utiliza a totalidade de sua participação societária, correspondente a {diag.QuantidadeAportada} ({ExtensoNum(diag.QuantidadeAportada)}) quotas, no valor de {Fmt(diag.ValorAtribuido)} ({Extenso(diag.ValorAtribuido)}), para integralizar o capital social da sociedade <strong>{diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cofre.Cnpj}.</p>");
            sb.AppendLine("<p>Em consequência:</p>");
            sb.AppendLine($"<p>I — retira-se do quadro societário, quanto à participação transferida, o sócio <strong>{diag.Conferente.Nome}</strong>;</p>");
            sb.AppendLine($"<p>II — ingressa no quadro societário a sociedade <strong>{diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cofre.Cnpj}, que passa a ser titular das referidas quotas.</p>");
        }
        else
        {
            sb.AppendLine("<h2>CLÁUSULA ÚNICA — TRANSFERÊNCIA PARCIAL DE PARTICIPAÇÃO PARA INTEGRALIZAÇÃO EM OUTRA SOCIEDADE</h2>");
            sb.AppendLine($"<p>Os sócios resolvem registrar que o sócio <strong>{diag.Conferente.Nome}</strong> utiliza parte de sua participação societária, correspondente a {diag.QuantidadeAportada} ({ExtensoNum(diag.QuantidadeAportada)}) quotas, no valor de {Fmt(diag.ValorAtribuido)} ({Extenso(diag.ValorAtribuido)}), para integralizar o capital social da sociedade <strong>{diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cofre.Cnpj}.</p>");
            sb.AppendLine("<p>Em consequência:</p>");
            sb.AppendLine($"<p>I — reduz-se a participação do sócio <strong>{diag.Conferente.Nome}</strong> de {diag.QuotasConferenteAntesInvestida} ({ExtensoNum(diag.QuotasConferenteAntesInvestida ?? 0)}) quotas para {diag.QuotasConferenteDepoisInvestida} ({ExtensoNum(diag.QuotasConferenteDepoisInvestida ?? 0)}) quotas;</p>");
            sb.AppendLine($"<p>II — ingressa no quadro societário a sociedade <strong>{diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cofre.Cnpj}, que passa a ser titular de {diag.QuantidadeAportada} ({ExtensoNum(diag.QuantidadeAportada)}) quotas.</p>");
        }

        // Nova redação do capital da investida
        sb.AppendLine($"<p>A cláusula {diag.ClausulaCapitalNumeroInvestida}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote><p>\"O capital social da sociedade permanece em {Fmt(diag.CapitalInvestida)} ({Extenso(diag.CapitalInvestida)}), dividido em {diag.QuadroInvestidaPosAlteracao.Sum(s => s.Quotas)} quotas, distribuídas da seguinte forma:</p>");

        foreach (var socio in diag.QuadroInvestidaPosAlteracao)
        {
            var identificacao = socio.EhPessoaJuridica
                ? $"{socio.Nome}, CNPJ nº {socio.Cnpj}"
                : $"{socio.Nome}, CPF nº {socio.Cpf}";
            var valorTotal = socio.Quotas * diag.ValorPorQuotaInvestida;
            sb.AppendLine($"<p>• <strong>{identificacao}</strong>: {socio.Quotas} quotas, no valor total de {Fmt(valorTotal)};\"</p>");
        }
        sb.AppendLine("</blockquote>");

        sb.AppendLine("<h2>RATIFICAÇÃO</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");

        // Fecho
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var socio in diag.QuadroInvestidaPosAlteracao)
            sb.AppendLine($"<p>________________________________________<br/><strong>{socio.Nome}</strong></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    // ATO 2 — ALTERAÇÃO DO COFRE
    // ═══════════════════════════════════════════════════════════

    private string MontarAtoCofre(DiagnosticoParticipacoesCofre diag)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</h1>");
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios de <strong>{diag.Cofre.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cofre.Cnpj}, com sede em {diag.Cofre.Endereco.Completo}, registrada na {diag.Cofre.JuntaComercialNome} sob NIRE {diag.Cofre.Nire}, resolvem alterar o contrato social, nos seguintes termos:</p>");

        // Cláusula 1 — Aumento de capital
        sb.AppendLine("<h2>CLÁUSULA 1 — AUMENTO DE CAPITAL</h2>");
        sb.AppendLine($"<p>Os sócios resolvem aumentar o capital social da sociedade de {Fmt(diag.CapitalAtualCofre)} ({Extenso(diag.CapitalAtualCofre)}) para {Fmt(diag.CapitalNovoCofre)} ({Extenso(diag.CapitalNovoCofre)}), mediante integralização de participações societárias, na forma desta alteração contratual.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O aumento de capital ora deliberado guarda coerência com a finalidade patrimonial-societária da sociedade, estruturada como célula Cofre, voltada à concentração de participações e à preservação do núcleo patrimonial familiar, sem exercício de atividade operacional própria perante terceiros.</p>");

        // Cláusula 2 — Integralização
        sb.AppendLine("<h2>CLÁUSULA 2 — INTEGRALIZAÇÃO COM PARTICIPAÇÕES SOCIETÁRIAS</h2>");

        var qualificacao = MontarQualificacao(diag.Conferente);

        // ── BC-P1: Quotas vs Ações ───────────────────────────
        if (diag.EhQuotasLtda)
        {
            sb.AppendLine($"<p>Para fins de realização do aumento de capital social ora aprovado, o sócio {qualificacao} integraliza o montante de {Fmt(diag.ValorAumento)} ({Extenso(diag.ValorAumento)}), mediante conferência de {diag.QuantidadeAportada} ({ExtensoNum(diag.QuantidadeAportada)}) quotas de emissão da sociedade <strong>{diag.Investida.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Investida.Cnpj}, com sede em {diag.Investida.Endereco.Completo}, registrada na {diag.Investida.JuntaComercialNome} sob NIRE {diag.Investida.Nire}, correspondentes a {diag.PercentualInvestida:F2}% de seu capital social.</p>");
        }
        else
        {
            // Ações de S.A.
            var a = diag.Acoes!;
            sb.AppendLine($"<p>Para fins de realização do aumento de capital social ora aprovado, o sócio {qualificacao} integraliza o montante de {Fmt(diag.ValorAumento)} ({Extenso(diag.ValorAumento)}), mediante conferência de {diag.QuantidadeAportada} ({ExtensoNum(diag.QuantidadeAportada)}) ações {a.Especie}, {a.Forma}{(a.Classe != null ? $", classe {a.Classe}" : "")}{(a.ValorNominal.HasValue ? $", valor nominal de {Fmt(a.ValorNominal.Value)}" : "")}, de emissão da companhia <strong>{diag.Investida.NomeEmpresarial.ToUpper()}</strong>, CNPJ nº {diag.Investida.Cnpj}, às quais as partes atribuem, para esta operação, o valor de {Fmt(diag.ValorAtribuido)} ({Extenso(diag.ValorAtribuido)}), integralmente destinado ao aumento do capital social da sociedade receptora.</p>");
        }

        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> As partes atribuem às participações societárias ora conferidas, para esta operação societária, o valor de {Fmt(diag.ValorAtribuido)} ({Extenso(diag.ValorAtribuido)}), integralmente destinado à realização do aumento do capital social da sociedade receptora.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> O sócio conferente declara que as participações societárias aportadas encontram-se livres e desembaraçadas de ônus, gravames, restrições à circulação ou litígios que impeçam a sua transferência, ressalvadas as limitações legais e contratuais expressamente identificadas no presente ato.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> O sócio conferente declara, ainda, que o capital objeto da operação encontra-se totalmente integralizado, nos termos exigidos para a presente modalidade de integralização.</p>");
        sb.AppendLine($"<p><strong>Parágrafo quarto.</strong> A presente operação importa, quando cabível, na correspondente alteração do quadro societário da sociedade investida, conforme ato próprio e correlato.</p>");

        // Cláusula 3 — Nova redação do capital
        sb.AppendLine("<h2>CLÁUSULA 3 — NOVA REDAÇÃO DA CLÁUSULA DO CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>Em razão da deliberação acima, a cláusula {diag.ClausulaCapitalNumeroCofre}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote><p>\"O capital social da sociedade é de {Fmt(diag.CapitalNovoCofre)} ({Extenso(diag.CapitalNovoCofre)}), dividido em {diag.TotalQuotasNovoCofre} ({ExtensoNum(diag.TotalQuotasNovoCofre)}) quotas, no valor nominal de {Fmt(diag.ValorPorQuotaCofre)} cada uma, totalmente subscritas e integralizadas, distribuídas entre os sócios da seguinte forma:</p>");

        foreach (var socio in diag.QuadroCofre)
        {
            var valorTotal = socio.QuotasPosAumento * diag.ValorPorQuotaCofre;
            sb.AppendLine($"<p>• <strong>{socio.Nome}</strong>: {socio.QuotasPosAumento} ({ExtensoNum(socio.QuotasPosAumento)}) quotas, no valor total de {Fmt(valorTotal)};\"</p>");
        }
        sb.AppendLine("</blockquote>");

        // Cláusula 4 — Manutenção da natureza do Cofre
        sb.AppendLine("<h2>CLÁUSULA 4 — MANUTENÇÃO DA NATUREZA DA CÉLULA COFRE</h2>");
        sb.AppendLine("<p>Os sócios reafirmam que a sociedade permanece estruturada como holding não financeira de função patrimonial-societária, destinada à concentração de participações, ao exercício dos direitos inerentes aos ativos societários detidos e à preservação do núcleo patrimonial familiar, sem exploração de atividade operacional própria, locação imobiliária operacional, compra e venda habitual de imóveis, prestação de serviços a terceiros ou outra atuação de linha de frente negocial.</p>");

        // Cláusula 5 — Ratificação
        sb.AppendLine("<h2>CLÁUSULA 5 — RATIFICAÇÃO</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");

        // Fecho
        var qtdVias = diag.QuadroCofre.Count + 1;
        sb.AppendLine($"<p>E, por estarem justos e contratados, assinam o presente instrumento em {qtdVias} ({ExtensoNum(qtdVias)}) vias de igual teor e forma.</p>");
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var socio in diag.QuadroCofre)
            sb.AppendLine($"<p>________________________________________<br/><strong>{socio.Nome}</strong></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════
    // ATO 3 — CHECKLIST AVERBAÇÃO S.A.
    // ═══════════════════════════════════════════════════════════

    private List<string> GerarChecklistSA(DiagnosticoParticipacoesCofre diag)
    {
        var a = diag.Acoes!;
        return
        [
            $"Averbar transferência de {diag.QuantidadeAportada} ações {a.Especie} {a.Forma} no Livro de Registro de Ações Nominativas da companhia {diag.Investida.NomeEmpresarial}",
            $"Averbar transferência no Livro de Transferência de Ações Nominativas",
            $"Comunicar a administração da companhia sobre a transferência",
            $"Espécie: {a.Especie}, Classe: {a.Classe ?? "única"}, Forma: {a.Forma}, Valor nominal: {(a.ValorNominal.HasValue ? Fmt(a.ValorNominal.Value) : "sem valor nominal")}",
            $"Novo titular: {diag.Cofre.NomeEmpresarial} LTDA., CNPJ {diag.Cofre.Cnpj}",
        ];
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private List<string> IdentificarBlocos(DiagnosticoParticipacoesCofre diag)
    {
        var blocos = new List<string>();
        blocos.Add(diag.EhQuotasLtda ? "BC-P1-Quotas" : "BC-P1-Acoes");
        blocos.Add(diag.UsoTotalOuParcial == UsoParticipacao.Total ? "BC-P2-Total" : "BC-P2-Parcial");
        if (diag.MesmaUf) blocos.Add("BC-P3-MesmaUF");
        if (diag.CofrePuro) blocos.Add("BC-P4-ReforcoNarrativo");
        return blocos;
    }

    private static string MontarQualificacao(SocioQuadroAlteracao s)
    {
        var partes = new List<string> { $"<strong>{s.Nome}</strong>", s.Nacionalidade };
        var ec = s.EstadoCivil switch
        {
            EstadoCivil.Solteiro => "solteiro(a)",
            EstadoCivil.Casado => "casado(a)",
            EstadoCivil.Divorciado => "divorciado(a)",
            EstadoCivil.Viuvo => "viúvo(a)",
            EstadoCivil.UniaoEstavel => "convivente em união estável",
            _ => s.EstadoCivil.ToString().ToLower()
        };
        if (s.EstadoCivil == EstadoCivil.Casado && s.RegimeBens.HasValue)
        {
            ec += " sob o regime de " + (s.RegimeBens.Value switch
            {
                RegimeBens.ComunhaoTotal => "comunhão universal de bens",
                RegimeBens.ComunhaoParcial => "comunhão parcial de bens",
                RegimeBens.SeparacaoTotal => "separação total de bens",
                RegimeBens.SeparacaoObrigatoria => "separação obrigatória de bens",
                _ => s.RegimeBens.Value.ToString().ToLower()
            });
        }
        partes.Add(ec);
        partes.Add(s.Profissao);
        partes.Add($"portador(a) do RG nº {s.Rg} ({s.RgOrgao})");
        partes.Add($"inscrito(a) no CPF sob nº {s.Cpf}");
        partes.Add($"residente e domiciliado(a) à {s.EnderecoCompleto}");
        return string.Join(", ", partes);
    }

    private static string Fmt(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    private static string Extenso(decimal v) => $"{v:N2} por extenso";
    private static string ExtensoNum(int n) => $"{n} por extenso";
}
