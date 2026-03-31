using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using System.Text;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// VALIDAÇÃO — Minuta 2.0: Alteração Contratual Cofre
// 4 bloqueios duros + 3 alertas
// ═══════════════════════════════════════════════════════════════

public class ValidacaoAlteracaoCofreService
{
    public ResultadoValidacao Validar(DiagnosticoAlteracaoCofre diag)
    {
        var r = new ResultadoValidacao();

        // ── B1: Excedente fora do capital (Tema 796 STF) ────
        if (Math.Abs(diag.Imovel.ValorAtribuido - diag.ValorAumento) > 0.01m)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B1",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = $"TEMA 796 STF: O valor atribuído ao imóvel ({FormatarMoeda(diag.Imovel.ValorAtribuido)}) é diferente do valor do aumento de capital ({FormatarMoeda(diag.ValorAumento)}). A imunidade do ITBI não alcança o valor que exceder o capital social integralizado. O documento não pode conter parcela lançada fora do capital.",
                FundamentoLegal = "Tema 796, STF — RE 796.376/SC",
                Sugestao = "Igualar o valor atribuído ao imóvel com o valor exato do aumento de capital."
            });
        }

        // ── B2: Desvio funcional do Cofre ───────────────────
        if (diag.CofrePuro && diag.AtividadeOperacionalPropria)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B2",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "DESVIO FUNCIONAL: A célula Cofre está marcada como pura, mas possui atividade operacional própria (locação, compra/venda habitual, serviços). Isso rompe a coerência do objeto social e enfraquece a linha patrimonial perante o CTN e o Tema 1348 STF.",
                FundamentoLegal = "CTN, art. 37 + Tema 1348 STF (pendente)",
                Sugestao = "Reclassificar a célula ou remover a atividade operacional antes de prosseguir."
            });
        }

        // ── B3: Objeto social incompatível ──────────────────
        if (!string.IsNullOrWhiteSpace(diag.ObjetoSocialAtual))
        {
            var objLower = diag.ObjetoSocialAtual.ToLower();
            var termosOperacionais = new[] { "locação", "locacao", "compra e venda", "prestação de serviço", "incorporação", "loteamento", "construção para venda" };

            if (diag.CofrePuro && termosOperacionais.Any(t => objLower.Contains(t)))
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "B3",
                    Severidade = SeveridadeValidacao.Bloqueio,
                    Mensagem = "OBJETO SOCIAL INCOMPATÍVEL: O objeto social atual da sociedade contém atividade operacional estranha à célula Cofre. O DREI exige objeto social preciso e claro. Antes de integralizar o imóvel, é necessário alterar o objeto social ou realizar alteração simultânea.",
                    FundamentoLegal = "Manual DREI — IN DREI nº 81/2020",
                    Sugestao = "Alterar o objeto social previamente ou incluir a alteração do objeto nesta mesma alteração contratual."
                });
            }
        }

        // ── B4: Descrição incompleta do imóvel ──────────────
        var camposFaltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(diag.Imovel.Matricula)) camposFaltantes.Add("matrícula");
        if (string.IsNullOrWhiteSpace(diag.Imovel.Cartorio)) camposFaltantes.Add("cartório");
        if (string.IsNullOrWhiteSpace(diag.Imovel.Endereco)) camposFaltantes.Add("localização/endereço");
        if (string.IsNullOrWhiteSpace(diag.Imovel.Area)) camposFaltantes.Add("área");

        if (camposFaltantes.Any())
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "B4",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = $"DESCRIÇÃO INCOMPLETA: O imóvel não possui: {string.Join(", ", camposFaltantes)}. O DREI exige descrição registral completa do imóvel no ato que constata o aumento de capital.",
                FundamentoLegal = "Manual DREI — IN DREI nº 81/2020",
                Sugestao = "Preencher todos os campos obrigatórios antes de gerar a minuta."
            });
        }

        // ── A1: Anuência conjugal ───────────────────────────
        var conf = diag.Conferente;
        if ((conf.EstadoCivil == EstadoCivil.Casado || conf.UniaoEstavel))
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "A1",
                Severidade = SeveridadeValidacao.Alerta,
                Mensagem = $"O sócio conferente {conf.Nome} é {(conf.UniaoEstavel ? "convivente em união estável" : "casado(a)")}. Verificar necessidade de anuência conjugal/convivencial para conferência de imóvel ao capital social (art. 1.647 CC).",
                FundamentoLegal = "Art. 1.647, CC/2002",
                Sugestao = "Obter outorga uxória/marital em documento apartado ou no próprio ato."
            });
        }

        // ── A2: Imóvel rural sem CCIR/NIRF ──────────────────
        if (diag.Imovel.Tipo == TipoImovelIntegralizacao.Rural)
        {
            var ruraisFaltantes = new List<string>();
            if (string.IsNullOrWhiteSpace(diag.Imovel.Ccir)) ruraisFaltantes.Add("CCIR");
            if (string.IsNullOrWhiteSpace(diag.Imovel.NirfCafir)) ruraisFaltantes.Add("NIRF/CAFIR");

            if (ruraisFaltantes.Any())
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "A2",
                    Severidade = SeveridadeValidacao.Alerta,
                    Mensagem = $"Imóvel rural sem: {string.Join(", ", ruraisFaltantes)}. Esses documentos são exigidos para registro da alteração contratual.",
                    FundamentoLegal = "Lei 4.947/66 + IN INCRA nº 82/2015",
                    Sugestao = "Incluir CCIR válido e comprovante de quitação do ITR."
                });
            }
        }

        // ── A3: Percentuais pós-aumento ─────────────────────
        var totalPct = diag.QuadroSocietario.Sum(s => s.PercentualPosAumento);
        if (Math.Abs(totalPct - 100m) > 0.01m)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "A3",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = $"Os percentuais de participação pós-aumento somam {totalPct:F2}%. Devem somar exatamente 100%.",
                Sugestao = "Recalcular a distribuição de quotas no quadro pós-alteração."
            });
        }

        // ── Checklist registral ─────────────────────────────
        GerarChecklist(diag, r);

        return r;
    }

    private void GerarChecklist(DiagnosticoAlteracaoCofre diag, ResultadoValidacao r)
    {
        r.ChecklistRegistral.Add("Alteração contratual assinada por todos os sócios");
        r.ChecklistRegistral.Add("Duas testemunhas com nome, CPF e assinatura");
        r.ChecklistRegistral.Add("Capa padrão JUCESP/DREI");
        r.ChecklistRegistral.Add("DBE de alteração de dados cadastrais (se houver mudança de capital)");
        r.ChecklistRegistral.Add("Laudo de avaliação ou declaração de valor do imóvel");
        r.ChecklistRegistral.Add("Registro da transferência no CRI competente após arquivamento na Junta");
        r.ChecklistRegistral.Add("Guia de ITBI (verificar imunidade — Tema 796)");

        if (diag.Imovel.Tipo == TipoImovelIntegralizacao.Rural)
        {
            r.ChecklistRegistral.Add("CCIR válido do INCRA");
            r.ChecklistRegistral.Add("Comprovante de quitação do ITR ou certidão negativa");
            r.ChecklistRegistral.Add("Verificar necessidade de georreferenciamento (Lei 10.267/2001)");
        }

        if (diag.Conferente.EstadoCivil == EstadoCivil.Casado || diag.Conferente.UniaoEstavel)
            r.ChecklistRegistral.Add("Outorga/anuência conjugal formalizada");

        r.ChecklistRegistral.Add("Pagamento DARE da Junta Comercial");
    }

    private static string FormatarMoeda(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
}

// ═══════════════════════════════════════════════════════════════
// MONTAGEM — Minuta 2.0: Alteração Contratual Cofre
// 5 cláusulas + 3 blocos condicionais
// ═══════════════════════════════════════════════════════════════

public class MontagemAlteracaoCofreService
{
    private readonly ValidacaoAlteracaoCofreService _validacao = new();

    public ResultadoMontagem Montar(DiagnosticoAlteracaoCofre diag)
    {
        var validacao = _validacao.Validar(diag);
        if (!validacao.PodeGerar)
            return new ResultadoMontagem { Sucesso = false, Validacao = validacao };

        var sb = new StringBuilder();

        MontarTitulo(sb, diag);
        MontarPreambulo(sb, diag);
        MontarClausula1_Deliberacao(sb, diag);
        MontarClausula2_Integralizacao(sb, diag);
        MontarClausula3_NovaRedacao(sb, diag);
        MontarClausula4_NaturezaCofre(sb, diag);
        MontarClausula5_Ratificacao(sb, diag);
        MontarFecho(sb, diag);

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = validacao,
            BlocosCondicionaisAtivados = IdentificarBlocos(diag),
        };
    }

    // ═══════════════════════════════════════════════════════════

    private void MontarTitulo(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine($"<h1>ALTERAÇÃO CONTRATUAL DA SOCIEDADE {diag.NomeEmpresarial.ToUpper()} LTDA.</h1>");
    }

    private void MontarPreambulo(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine($"<p>Pelo presente instrumento particular, os sócios da sociedade empresária limitada <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cnpj}, com sede em {diag.Endereco.Completo}, registrada na {diag.JuntaComercialNome} sob NIRE {diag.Nire}, resolvem alterar o contrato social, nos seguintes termos:</p>");
    }

    private void MontarClausula1_Deliberacao(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 1 — DELIBERAÇÃO DE AUMENTO DE CAPITAL</h2>");
        sb.AppendLine($"<p>Os sócios resolvem aumentar o capital social da sociedade de {Fmt(diag.CapitalAtual)} ({Extenso(diag.CapitalAtual)}) para {Fmt(diag.CapitalNovo)} ({Extenso(diag.CapitalNovo)}), mediante integralização em bens imóveis, na forma desta alteração contratual.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O aumento de capital ora deliberado guarda coerência com a finalidade patrimonial-societária da sociedade, estruturada como célula Cofre, sem exercício de atividade operacional própria perante terceiros.</p>");
    }

    private void MontarClausula2_Integralizacao(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        var conf = diag.Conferente;
        var im = diag.Imovel;

        sb.AppendLine("<h2>CLÁUSULA 2 — INTEGRALIZAÇÃO DO AUMENTO DE CAPITAL</h2>");

        // Qualificação do conferente
        var qualificacao = MontarQualificacao(conf);
        sb.AppendLine($"<p>Para fins de realização do aumento de capital social ora aprovado, o sócio {qualificacao} integraliza o montante de {Fmt(diag.ValorAumento)} ({Extenso(diag.ValorAumento)}), mediante conferência do seguinte imóvel:</p>");

        // ── BC-A1: Imóvel urbano vs rural ────────────────────
        if (im.Tipo == TipoImovelIntegralizacao.Urbano)
        {
            sb.AppendLine($"<p>{im.Descricao}, objeto da matrícula nº {im.Matricula}, do {im.Cartorio}, situado em {im.Endereco}, com área de {im.Area}, cadastro municipal nº {im.CadastroMunicipal}, adquirido por {im.TituloAquisitivo}.</p>");
        }
        else
        {
            // Rural: inclui CCIR e NIRF
            sb.AppendLine($"<p>{im.Descricao}, objeto da matrícula nº {im.Matricula}, do {im.Cartorio}, denominado \"{im.Denominacao}\", com área de {im.Area}, localizado no município de {im.MunicipioUf}, CCIR nº {im.Ccir}, NIRF/CAFIR nº {im.NirfCafir}, adquirido por {im.TituloAquisitivo}.</p>");
        }

        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> As partes atribuem ao bem, para esta operação societária, o valor de {Fmt(im.ValorAtribuido)} ({Extenso(im.ValorAtribuido)}), integralmente destinado à realização do aumento do capital social deliberado neste ato.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A integralização ora promovida é feita para fins de realização de capital social, não havendo, neste ato, destinação de parcela do valor do bem a conta patrimonial diversa do capital social.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> O sócio conferente responde pela titularidade, disponibilidade, legitimidade e regularidade do bem aportado, na forma da lei.</p>");
    }

    private void MontarClausula3_NovaRedacao(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 3 — NOVA REDAÇÃO DA CLÁUSULA DO CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>Em razão da deliberação acima, a cláusula {diag.ClausulaCapitalNumero}ª do contrato social passa a vigorar com a seguinte redação:</p>");
        sb.AppendLine($"<blockquote>");
        sb.AppendLine($"<p>\"O capital social da sociedade é de {Fmt(diag.CapitalNovo)} ({Extenso(diag.CapitalNovo)}), dividido em {diag.TotalQuotasNovo} ({ExtensoNum(diag.TotalQuotasNovo)}) quotas, no valor nominal de {Fmt(diag.ValorPorQuota)} cada uma, totalmente subscritas e integralizadas pelos sócios, na seguinte proporção:</p>");

        foreach (var socio in diag.QuadroSocietario)
        {
            var valorTotal = socio.QuotasPosAumento * diag.ValorPorQuota;
            sb.AppendLine($"<p>• <strong>{socio.Nome}</strong>: {socio.QuotasPosAumento} ({ExtensoNum(socio.QuotasPosAumento)}) quotas, no valor total de {Fmt(valorTotal)};\"</p>");
        }

        sb.AppendLine($"</blockquote>");
    }

    private void MontarClausula4_NaturezaCofre(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 4 — MANUTENÇÃO DA NATUREZA DA CÉLULA COFRE</h2>");
        sb.AppendLine("<p>Os sócios reafirmam que a sociedade permanece estruturada como holding não financeira de função patrimonial-societária, destinada à concentração e preservação do núcleo patrimonial familiar, sem exercício de atividade operacional própria, sem exploração direta de locação imobiliária, compra e venda habitual de imóveis, prestação de serviços a terceiros ou outra atividade de linha de frente negocial.</p>");

        // ── BC-A2: Reforço narrativo ─────────────────────────
        if (diag.DeveReforcoNarrativo)
        {
            sb.AppendLine("<p><strong>Parágrafo único.</strong> Os sócios declaram que a presente operação de aumento de capital insere-se na organização patrimonial da família, mantendo-se a sociedade como célula patrimonial-societária de preservação, sem desvio para atividade operacional própria.</p>");
        }
    }

    private void MontarClausula5_Ratificacao(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 5 — RATIFICAÇÃO DAS DEMAIS CLÁUSULAS</h2>");
        sb.AppendLine("<p>Permanecem inalteradas e em pleno vigor todas as demais cláusulas do contrato social que não conflitarem com a presente alteração.</p>");
    }

    private void MontarFecho(StringBuilder sb, DiagnosticoAlteracaoCofre diag)
    {
        var qtdVias = diag.QuadroSocietario.Count + 1;
        sb.AppendLine($"<p>E, por estarem justos e contratados, assinam o presente instrumento em {qtdVias} ({ExtensoNum(qtdVias)}) vias de igual teor e forma.</p>");
        sb.AppendLine("<p>[Local], [data].</p>");

        foreach (var socio in diag.QuadroSocietario)
        {
            sb.AppendLine($"<p>________________________________________<br/><strong>{socio.Nome}</strong></p>");
        }

        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private List<string> IdentificarBlocos(DiagnosticoAlteracaoCofre diag)
    {
        var blocos = new List<string>();
        blocos.Add(diag.Imovel.Tipo == TipoImovelIntegralizacao.Rural ? "BC-A1-Rural" : "BC-A1-Urbano");
        if (diag.DeveReforcoNarrativo) blocos.Add("BC-A2-ReforcoNarrativo");
        if (diag.Conferente.EstadoCivil == EstadoCivil.Casado || diag.Conferente.UniaoEstavel) blocos.Add("BC-A3-AnuenciaConjugal");
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
            EstadoCivil.SeparadoJudicialmente => "separado(a) judicialmente",
            _ => s.EstadoCivil.ToString().ToLower()
        };

        if (s.EstadoCivil == EstadoCivil.Casado && s.RegimeBens.HasValue)
        {
            var rb = s.RegimeBens.Value switch
            {
                RegimeBens.ComunhaoTotal => "comunhão universal de bens",
                RegimeBens.ComunhaoParcial => "comunhão parcial de bens",
                RegimeBens.SeparacaoTotal => "separação total de bens",
                RegimeBens.SeparacaoObrigatoria => "separação obrigatória de bens",
                RegimeBens.ParticipacaoFinalAquestos => "participação final nos aquestos",
                _ => s.RegimeBens.Value.ToString().ToLower()
            };
            ec += $" sob o regime de {rb}";
        }
        partes.Add(ec);

        partes.Add(s.Profissao);
        partes.Add($"portador(a) do RG nº {s.Rg} ({s.RgOrgao})");
        partes.Add($"inscrito(a) no CPF sob nº {s.Cpf}");
        partes.Add($"residente e domiciliado(a) à {s.EnderecoCompleto}");

        return string.Join(", ", partes);
    }

    private static string Fmt(decimal v) => v.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    private static string Extenso(decimal v) => $"{v:N2} por extenso"; // Placeholder: usar Humanizer em produção
    private static string ExtensoNum(int n) => $"{n} por extenso";
}
