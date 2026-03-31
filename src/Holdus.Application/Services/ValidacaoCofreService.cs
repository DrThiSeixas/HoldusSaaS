using Holdus.Domain.Entities;
using Holdus.Domain.Enums;

namespace Holdus.Application.Services;

/// <summary>
/// Etapa B do motor documental — Validação.
/// Executa as 10 regras de validação antes da montagem do contrato.
/// </summary>
public class ValidacaoCofreService
{
    public ResultadoValidacao Validar(DiagnosticoCofre diag)
    {
        var resultado = new ResultadoValidacao();

        // ── V1: Art. 977 CC — cônjuges em comunhão universal ───
        ValidarArt977ComunhaoUniversal(diag, resultado);

        // ── V2: Art. 977 CC — cônjuges em separação obrigatória
        ValidarArt977SeparacaoObrigatoria(diag, resultado);

        // ── V3: Art. 1.647 CC — anuência conjugal para imóvel ──
        ValidarAnuenciaConjugal(diag, resultado);

        // ── V4: Tema 796 STF — valor do bem > capital ──────────
        ValidarTema796(diag, resultado);

        // ── V5: Desvio funcional do Cofre ──────────────────────
        ValidarDesvioFuncional(diag, resultado);

        // ── V6: Capital social inválido ────────────────────────
        ValidarCapitalSocial(diag, resultado);

        // ── V7: Sócio sem CPF ──────────────────────────────────
        ValidarCpfSocios(diag, resultado);

        // ── V8: Imóvel urbano sem matrícula ────────────────────
        ValidarMatriculaImovelUrbano(diag, resultado);

        // ── V9: Imóvel rural sem CCIR/NIRF ─────────────────────
        ValidarDocumentosRural(diag, resultado);

        // ── V10: Percentuais ≠ 100% ───────────────────────────
        ValidarPercentuais(diag, resultado);

        // ── Checklist registral ────────────────────────────────
        GerarChecklist(diag, resultado);

        return resultado;
    }

    // ═══════════════════════════════════════════════════════════
    // IMPLEMENTAÇÃO DAS REGRAS
    // ═══════════════════════════════════════════════════════════

    private void ValidarArt977ComunhaoUniversal(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        if (diag.Socios.Count != 2) return;

        var s1 = diag.Socios[0];
        var s2 = diag.Socios[1];

        // Verifica se são cônjuges entre si (mesmo CPF de cônjuge)
        var saoCojuges = (!string.IsNullOrEmpty(s1.CpfConjuge) && s1.CpfConjuge == s2.Cpf)
                      || (!string.IsNullOrEmpty(s2.CpfConjuge) && s2.CpfConjuge == s1.Cpf);

        if (!saoCojuges) return;

        if (s1.RegimeBens == RegimeBens.ComunhaoTotal || s2.RegimeBens == RegimeBens.ComunhaoTotal)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "V1",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "O Código Civil (art. 977) VEDA sociedade entre cônjuges casados sob o regime de comunhão universal de bens quando forem os únicos sócios.",
                FundamentoLegal = "Art. 977, CC/2002",
                Sugestao = "Incluir terceiro sócio, alterar regime de bens ou reestruturar o quadro societário."
            });
        }
    }

    private void ValidarArt977SeparacaoObrigatoria(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        if (diag.Socios.Count != 2) return;

        var s1 = diag.Socios[0];
        var s2 = diag.Socios[1];

        var saoCojuges = (!string.IsNullOrEmpty(s1.CpfConjuge) && s1.CpfConjuge == s2.Cpf)
                      || (!string.IsNullOrEmpty(s2.CpfConjuge) && s2.CpfConjuge == s1.Cpf);

        if (!saoCojuges) return;

        if (s1.RegimeBens == RegimeBens.SeparacaoObrigatoria || s2.RegimeBens == RegimeBens.SeparacaoObrigatoria)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "V2",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "O Código Civil (art. 977) VEDA sociedade entre cônjuges casados sob o regime de separação obrigatória de bens quando forem os únicos sócios.",
                FundamentoLegal = "Art. 977, CC/2002",
                Sugestao = "Incluir terceiro sócio ou reestruturar o quadro societário."
            });
        }
    }

    private void ValidarAnuenciaConjugal(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        foreach (var socio in diag.Socios)
        {
            // Casado ou em união estável + integralizando imóvel
            var casadoOuUE = socio.EstadoCivil == EstadoCivil.Casado || socio.UniaoEstavel;
            var temImovel = socio.Integralizacoes.Any(i =>
                i.Tipo == TipoIntegralizacao.ImovelUrbano || i.Tipo == TipoIntegralizacao.ImovelRural);

            if (casadoOuUE && temImovel && !socio.NecessitaAnuenciaConjugal)
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "V3",
                    Severidade = SeveridadeValidacao.Alerta,
                    Mensagem = $"O sócio {socio.Nome} é {(socio.UniaoEstavel ? "convivente em união estável" : "casado(a)")} e está integralizando imóvel. Verificar necessidade de anuência conjugal/convivencial (art. 1.647 CC).",
                    FundamentoLegal = "Art. 1.647, CC/2002",
                    Sugestao = "Obter outorga uxória/marital ou verificar se o regime de bens dispensa."
                });
            }
        }
    }

    private void ValidarTema796(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        foreach (var socio in diag.Socios)
        {
            var valorBensSocio = socio.Integralizacoes.Sum(i => i.Valor);
            var valorQuotasSocio = socio.Quotas * diag.ValorPorQuota;

            if (valorBensSocio > valorQuotasSocio)
            {
                var excedente = valorBensSocio - valorQuotasSocio;
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "V4",
                    Severidade = SeveridadeValidacao.Alerta,
                    Mensagem = $"TEMA 796 STF: O valor dos bens do sócio {socio.Nome} ({valorBensSocio:C}) excede o valor das quotas subscritas ({valorQuotasSocio:C}) em {excedente:C}. A imunidade do ITBI não alcança o valor excedente ao capital social integralizado.",
                    FundamentoLegal = "Tema 796, STF — RE 796.376/SC",
                    Sugestao = "Ajustar o valor atribuído ao bem para que corresponda exatamente ao capital subscrito, ou rever a estrutura de integralização."
                });
            }
        }
    }

    private void ValidarDesvioFuncional(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        if (diag.CofrePuro && diag.AtividadeOperacionalPropria)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "V5",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "DESVIO FUNCIONAL: A célula Cofre está marcada como pura, mas possui atividade operacional própria. Isso rompe a coerência do objeto social e enfraquece a linha patrimonial.",
                Sugestao = "Desmarque 'cofre puro' ou remova a atividade operacional. Se houver locação, compra/venda ou serviços, a célula deve ser reclassificada."
            });
        }
    }

    private void ValidarCapitalSocial(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        if (diag.CapitalSocial <= 0)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "V6",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = "O capital social deve ser maior que zero.",
            });
        }
    }

    private void ValidarCpfSocios(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        foreach (var socio in diag.Socios)
        {
            if (string.IsNullOrWhiteSpace(socio.Cpf))
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "V7",
                    Severidade = SeveridadeValidacao.Bloqueio,
                    Mensagem = $"O sócio {socio.Nome} não possui CPF cadastrado. O CPF é obrigatório para qualificação no contrato social.",
                });
            }
        }
    }

    private void ValidarMatriculaImovelUrbano(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        var imoveis = diag.Socios
            .SelectMany(s => s.Integralizacoes)
            .Where(i => i.Tipo == TipoIntegralizacao.ImovelUrbano);

        foreach (var im in imoveis)
        {
            if (string.IsNullOrWhiteSpace(im.Matricula))
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "V8",
                    Severidade = SeveridadeValidacao.Alerta,
                    Mensagem = $"O imóvel urbano '{im.Descricao}' não possui matrícula informada. A matrícula é essencial para o registro do contrato social.",
                    Sugestao = "Preencher o número da matrícula e o cartório de registro de imóveis."
                });
            }
        }
    }

    private void ValidarDocumentosRural(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        var rurais = diag.Socios
            .SelectMany(s => s.Integralizacoes)
            .Where(i => i.Tipo == TipoIntegralizacao.ImovelRural);

        foreach (var im in rurais)
        {
            var problemas = new List<string>();
            if (string.IsNullOrWhiteSpace(im.Matricula)) problemas.Add("matrícula");
            if (string.IsNullOrWhiteSpace(im.Ccir)) problemas.Add("CCIR");
            if (string.IsNullOrWhiteSpace(im.NirfCafir)) problemas.Add("NIRF/CAFIR");

            if (problemas.Any())
            {
                r.Erros.Add(new ItemValidacao
                {
                    Codigo = "V9",
                    Severidade = SeveridadeValidacao.Alerta,
                    Mensagem = $"O imóvel rural '{im.Descricao ?? im.Denominacao}' não possui: {string.Join(", ", problemas)}. Esses dados são exigidos pelo DREI para registro.",
                    Sugestao = "Preencher todos os campos obrigatórios para imóvel rural."
                });
            }
        }
    }

    private void ValidarPercentuais(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        var totalPct = diag.Socios.Sum(s => s.PercentualParticipacao);

        if (Math.Abs(totalPct - 100m) > 0.01m)
        {
            r.Erros.Add(new ItemValidacao
            {
                Codigo = "V10",
                Severidade = SeveridadeValidacao.Bloqueio,
                Mensagem = $"A soma dos percentuais de participação dos sócios é {totalPct:F2}%. Deve ser exatamente 100%.",
            });
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CHECKLIST REGISTRAL
    // ═══════════════════════════════════════════════════════════

    private void GerarChecklist(DiagnosticoCofre diag, ResultadoValidacao r)
    {
        r.ChecklistRegistral.Add("Contrato social assinado por todos os sócios");
        r.ChecklistRegistral.Add("Duas testemunhas com nome, CPF e assinatura");
        r.ChecklistRegistral.Add("Capa padrão JUCESP/DREI com FCN integrada");
        r.ChecklistRegistral.Add("DBE (CNPJ) via Redesim/Coletor Nacional");
        r.ChecklistRegistral.Add("Consulta prévia de viabilidade na prefeitura");

        if (diag.Socios.Any(s => s.EstadoCivil == EstadoCivil.Casado || s.UniaoEstavel))
            r.ChecklistRegistral.Add("Qualificação completa com regime de bens e/ou indicação de união estável");

        if (diag.TemImóvelUrbano || diag.TemImovelRural)
        {
            r.ChecklistRegistral.Add("Laudo de avaliação ou declaração de valor dos imóveis");
            r.ChecklistRegistral.Add("Registro da transferência no CRI competente após arquivamento");
        }

        if (diag.TemImovelRural)
        {
            r.ChecklistRegistral.Add("CCIR (Certificado de Cadastro de Imóvel Rural) válido");
            r.ChecklistRegistral.Add("Comprovante de quitação do ITR ou NIRF/CAFIR");
            r.ChecklistRegistral.Add("Verificar necessidade de georreferenciamento (Lei 10.267/2001)");
        }

        if (diag.TemParticipacoes)
            r.ChecklistRegistral.Add("Alteração contratual reflexa na sociedade investida");

        if (diag.Socios.Any(s => s.NecessitaAnuenciaConjugal))
            r.ChecklistRegistral.Add("Outorga conjugal/convivencial formalizada");

        r.ChecklistRegistral.Add("Pagamento DARE/DARF (se exigível)");
        r.ChecklistRegistral.Add("Alvará de funcionamento (se aplicável ao município)");
    }
}
