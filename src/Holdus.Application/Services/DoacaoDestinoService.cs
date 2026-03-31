#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 5.0 — Instrumento de Doação de Quotas (Célula Destino)
// "É este instrumento que começa a transferência patrimonial."
//
// 9 cláusulas + 5 blocos condicionais + 4 bloqueios duros
// ═══════════════════════════════════════════════════════════════

// ── ENUMS ────────────────────────────────────────────────────

public enum NaturezaDoacao
{
    AdiantamentoLegitima = 0,   // Padrão: art. 544 CC
    ParteDisponivelComDispensa = 1  // Exige declaração expressa
}

public enum DireitosUsufruto
{
    EconomicosPoliticos = 0,        // Usufrutuário com voto + frutos
    EconomicosComVotoNuProprietario = 1,  // Frutos p/ usufrutuário, voto p/ nu-prop
    CompartilhadoEmAcordo = 2       // Regulado em acordo específico
}

[Flags]
public enum ClausulasRestritivas
{
    Nenhuma = 0,
    Incomunicabilidade = 1,
    Impenhorabilidade = 2,
    Inalienabilidade = 4,
    Reversao = 8
}

// ── DIAGNÓSTICO ──────────────────────────────────────────────

public class DiagnosticoDoacaoDestino
{
    // Sociedade
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Nire { get; set; } = string.Empty;
    public string UfJunta { get; set; } = string.Empty;
    public string SedeSociedade { get; set; } = string.Empty;
    public string Comarca { get; set; } = string.Empty;

    // Partes
    public List<ParteDoacao> Doadores { get; set; } = [];
    public List<ParteDoacao> Donatarios { get; set; } = [];

    // Quotas
    public int QuantidadeQuotas { get; set; }
    public decimal ValorNominalQuota { get; set; } = 1.00m;
    public decimal ValorTotalDoacao => QuantidadeQuotas * ValorNominalQuota;

    // Natureza (BC-DOA1)
    public NaturezaDoacao Natureza { get; set; } = NaturezaDoacao.AdiantamentoLegitima;

    // Usufruto (BC-DOA2 + BC-DOA3)
    public bool ReservaUsufruto { get; set; }
    public DireitosUsufruto? DireitosDoUsufruto { get; set; }

    // Restrições (BC-DOA4)
    public ClausulasRestritivas Restricoes { get; set; } = ClausulasRestritivas.Nenhuma;

    // Acordo (BC-DOA5)
    public bool AcordoQuotistasVinculado { get; set; }

    // Confirmações
    public bool ConfirmaLimiteParteDisponivel { get; set; }
    public bool ConfirmaNaoCompromeSubsistencia { get; set; }
    public bool AlteracaoContratualCorrelata { get; set; }

    // Derivados
    public bool TemUsufruto => ReservaUsufruto;
    public bool TemRestricoes => Restricoes != ClausulasRestritivas.Nenhuma;
    public bool EhParteDisponivel => Natureza == NaturezaDoacao.ParteDisponivelComDispensa;
}

public class ParteDoacao
{
    public string Nome { get; set; } = string.Empty;
    public string Nacionalidade { get; set; } = "brasileira";
    public string Profissao { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string RgOrgao { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;
    public MaritalStatus EstadoCivil { get; set; } = MaritalStatus.NaoInformado;
    public MaritalRegime? RegimeBens { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// SERVIÇO DE VALIDAÇÃO + MONTAGEM
// ═══════════════════════════════════════════════════════════════

public class DoacaoDestinoService
{
    public ResultadoMontagem Montar(DiagnosticoDoacaoDestino diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();

        MontarTitulo(sb, diag);
        MontarPreambulo(sb, diag);
        MontarCl1_Sociedade(sb, diag);
        MontarCl2_Natureza(sb, diag);
        MontarCl3_Aceitacao(sb, diag);
        MontarCl4_Usufruto(sb, diag);
        MontarCl5_Restricoes(sb, diag);
        MontarCl6_Governanca(sb, diag);
        MontarCl7_Eficacia(sb, diag);
        MontarCl8_Declaracoes(sb, diag);
        MontarCl9_Foro(sb, diag);
        MontarFecho(sb, diag);

        var blocos = new List<string>();
        blocos.Add(diag.EhParteDisponivel ? "BC-DOA1-ParteDisponivel" : "BC-DOA1-AdiantamentoLegitima");
        if (diag.TemUsufruto) blocos.Add("BC-DOA2-Usufruto");
        if (diag.TemUsufruto && diag.DireitosDoUsufruto.HasValue) blocos.Add($"BC-DOA3-{diag.DireitosDoUsufruto.Value}");
        if (diag.TemRestricoes) blocos.Add("BC-DOA4-Restricoes");
        if (diag.AcordoQuotistasVinculado) blocos.Add("BC-DOA5-AcordoVinculado");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = blocos,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 4 bloqueios + alertas
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoDoacaoDestino diag)
    {
        var r = new ValidationResult();

        if (!diag.Doadores.Any())
            r.AddError("DOA_001", "Deve haver ao menos um doador.");
        if (!diag.Donatarios.Any())
            r.AddError("DOA_002", "Deve haver ao menos um donatário.");
        if (diag.QuantidadeQuotas <= 0)
            r.AddError("DOA_003", "A quantidade de quotas doadas deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(diag.Cnpj))
            r.AddError("DOA_004", "O CNPJ da sociedade é obrigatório.");

        foreach (var d in diag.Doadores.Where(d => string.IsNullOrWhiteSpace(d.Cpf)))
            r.AddError("DOA_005", $"Doador {d.Nome} não possui CPF.");
        foreach (var d in diag.Donatarios.Where(d => string.IsNullOrWhiteSpace(d.Cpf)))
            r.AddError("DOA_006", $"Donatário {d.Nome} não possui CPF.");

        // ── B1: Parte disponível sem declaração expressa ────
        if (diag.EhParteDisponivel && !diag.ConfirmaLimiteParteDisponivel)
        {
            r.AddError("DOA_B1",
                "BLOQUEIO: Doação à conta da parte disponível exige declaração expressa e validação interna de limite legal (arts. 548-549 CC).",
                "ConfirmaLimiteParteDisponivel");
        }

        // ── B2: Subsistência do doador ──────────────────────
        if (!diag.ConfirmaNaoCompromeSubsistencia)
        {
            r.AddError("DOA_B2",
                "BLOQUEIO: O sistema não pode prosseguir se a doação comprometer a subsistência do doador (art. 548 CC).",
                "ConfirmaNaoCompromeSubsistencia");
        }

        // ── B3: Usufruto sem disciplina de voto ─────────────
        if (diag.ReservaUsufruto && !diag.DireitosDoUsufruto.HasValue)
        {
            r.AddError("DOA_B3",
                "BLOQUEIO: Reserva de usufruto sem disciplina expressa de voto e direitos econômicos. O DREI exige regulação clara para eficácia perante terceiros.");
        }

        // ── B4: Sem alteração contratual correlata ──────────
        if (!diag.AlteracaoContratualCorrelata)
        {
            r.AddError("DOA_B4",
                "BLOQUEIO: Sem alteração contratual correlata da Destino, a doação fica documentalmente incompleta.",
                "AlteracaoContratualCorrelata");
        }

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // MONTAGEM — 9 CLÁUSULAS
    // ═══════════════════════════════════════════════════════════

    private void MontarTitulo(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine($"<h1>INSTRUMENTO PARTICULAR DE DOAÇÃO DE QUOTAS SOCIAIS DA SOCIEDADE {diag.NomeEmpresarial.ToUpper()} LTDA.</h1>");
    }

    private void MontarPreambulo(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine("<p>Pelo presente instrumento particular:</p>");

        sb.AppendLine("<p><strong>DOADOR(ES):</strong></p>");
        foreach (var d in diag.Doadores)
            sb.AppendLine(Qualificar(d));

        sb.AppendLine("<p><strong>DONATÁRIO(S):</strong></p>");
        foreach (var d in diag.Donatarios)
            sb.AppendLine(Qualificar(d));

        sb.AppendLine("<p>têm entre si justo e contratado o presente Instrumento de Doação de Quotas, que se regerá pelas cláusulas e condições seguintes.</p>");
    }

    private void MontarCl1_Sociedade(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        var doadorPlural = diag.Doadores.Count > 1;
        var donatarioPlural = diag.Donatarios.Count > 1;

        sb.AppendLine("<h2>CLÁUSULA 1 — SOCIEDADE E QUOTAS OBJETO DA DOAÇÃO</h2>");
        sb.AppendLine($"<p>O{(doadorPlural ? "s" : "")} doador{(doadorPlural ? "es são titulares" : " é titular")} de quotas da sociedade <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cnpj}, com sede em {diag.SedeSociedade}, devidamente registrada na Junta Comercial do Estado de {diag.UfJunta} sob NIRE {diag.Nire}.</p>");
        sb.AppendLine($"<p>Por este instrumento, o{(doadorPlural ? "s" : "")} doador{(doadorPlural ? "es doam" : " doa")} ao{(donatarioPlural ? "s" : "")} donatário{(donatarioPlural ? "s" : "")} {diag.QuantidadeQuotas} ({ExtN(diag.QuantidadeQuotas)}) quotas, no valor nominal de {Fmt(diag.ValorNominalQuota)} cada, totalizando {Fmt(diag.ValorTotalDoacao)}, livres e desembaraçadas, ressalvadas as restrições e gravames expressamente previstos neste instrumento e no contrato social.</p>");
    }

    private void MontarCl2_Natureza(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 2 — NATUREZA DA LIBERALIDADE</h2>");

        // ── BC-DOA1 ──────────────────────────────────────────
        if (diag.Natureza == NaturezaDoacao.AdiantamentoLegitima)
        {
            sb.AppendLine("<p>A presente doação é realizada como adiantamento do que cabe ao(s) donatário(s) por herança, nos termos do art. 544 do Código Civil.</p>");
        }
        else
        {
            sb.AppendLine("<p>A presente doação é realizada à conta da parte disponível do patrimônio do(s) doador(es), com dispensa de colação, sem exceder o limite legal, nos termos dos arts. 2.005 e seguintes do Código Civil.</p>");
            sb.AppendLine("<p><strong>Parágrafo único.</strong> O(s) doador(es) declara(m) expressamente que a presente liberalidade sai da parte disponível do seu patrimônio e não excede o limite que poderia dispor em testamento.</p>");
        }
    }

    private void MontarCl3_Aceitacao(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        var plural = diag.Donatarios.Count > 1;
        sb.AppendLine("<h2>CLÁUSULA 3 — ACEITAÇÃO</h2>");
        sb.AppendLine($"<p>O{(plural ? "s" : "")} donatário{(plural ? "s aceitam" : " aceita")} a presente doação, para todos os fins de direito.</p>");
    }

    private void MontarCl4_Usufruto(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        // ── BC-DOA2: só se tem reserva de usufruto ───────────
        if (!diag.TemUsufruto) return;

        var plural = diag.Doadores.Count > 1;

        sb.AppendLine("<h2>CLÁUSULA 4 — RESERVA DE USUFRUTO</h2>");
        sb.AppendLine($"<p>O{(plural ? "s" : "")} doador{(plural ? "es reservam" : " reserva")} para si, em caráter vitalício, o usufruto das quotas ora doadas.</p>");

        // ── BC-DOA3: disciplina de voto obrigatória ──────────
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> Enquanto perdurar o usufruto, os direitos sobre as quotas serão exercidos da seguinte forma:</p>");

        switch (diag.DireitosDoUsufruto)
        {
            case DireitosUsufruto.EconomicosPoliticos:
                sb.AppendLine("<p>O usufrutuário exercerá os direitos econômicos (frutos, dividendos, participação nos resultados) e os direitos políticos (voto em deliberações sociais) das quotas gravadas, enquanto perdurar o usufruto.</p>");
                break;

            case DireitosUsufruto.EconomicosComVotoNuProprietario:
                sb.AppendLine("<p>O usufrutuário exercerá os direitos econômicos (frutos, dividendos, participação nos resultados) das quotas gravadas. O exercício do direito de voto em deliberações sociais competirá ao nu-proprietário.</p>");
                break;

            case DireitosUsufruto.CompartilhadoEmAcordo:
                sb.AppendLine("<p>O exercício dos direitos econômicos e políticos sobre as quotas gravadas com usufruto será disciplinado em acordo de quotistas ou instrumento complementar específico, ao qual as partes se obrigam a aderir.</p>");
                break;
        }

        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A disciplina do voto em quotas gravadas com usufruto deverá ser arquivada na Junta Comercial, quando cabível, para produção de efeitos perante terceiros, conforme orientação do DREI.</p>");
    }

    private void MontarCl5_Restricoes(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        // ── BC-DOA4: só se tem restrições ────────────────────
        if (!diag.TemRestricoes) return;

        sb.AppendLine("<h2>CLÁUSULA 5 — RESTRIÇÕES INCIDENTES SOBRE AS QUOTAS</h2>");
        sb.AppendLine("<p>As quotas ora doadas ficam gravadas, nos termos permitidos em lei e na extensão aqui convencionada, com as seguintes restrições:</p>");

        if (diag.Restricoes.HasFlag(ClausulasRestritivas.Incomunicabilidade))
            sb.AppendLine("<p>I — incomunicabilidade;</p>");
        if (diag.Restricoes.HasFlag(ClausulasRestritivas.Impenhorabilidade))
            sb.AppendLine("<p>II — impenhorabilidade;</p>");
        if (diag.Restricoes.HasFlag(ClausulasRestritivas.Inalienabilidade))
            sb.AppendLine("<p>III — inalienabilidade;</p>");
        if (diag.Restricoes.HasFlag(ClausulasRestritivas.Reversao))
            sb.AppendLine("<p>IV — reversão ao patrimônio do doador em caso de pré-morte do donatário.</p>");
    }

    private void MontarCl6_Governanca(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        var plural = diag.Donatarios.Count > 1;

        sb.AppendLine("<h2>CLÁUSULA 6 — GOVERNANÇA E SUBMISSÃO AO CONTRATO SOCIAL E AO ACORDO DE QUOTISTAS</h2>");
        sb.AppendLine($"<p>O{(plural ? "s" : "")} donatário{(plural ? "s declaram" : " declara")} ciência e adesão:</p>");
        sb.AppendLine("<p>I — ao contrato social da sociedade;</p>");

        // ── BC-DOA5: reforço se há acordo vinculado ──────────
        if (diag.AcordoQuotistasVinculado)
            sb.AppendLine("<p>II — ao acordo de quotistas/protocolo familiar existente, ao qual adere formalmente neste ato;</p>");
        else
            sb.AppendLine("<p>II — ao eventual acordo de quotistas ou protocolo familiar que vier a ser celebrado;</p>");

        sb.AppendLine("<p>III — às regras de administração, voto, circulação de quotas, eventos de morte, incapacidade, divórcio, retirada e exclusão.</p>");
    }

    private void MontarCl7_Eficacia(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 7 — EFICÁCIA SOCIETÁRIA</h2>");
        sb.AppendLine("<p>As partes obrigam-se a praticar todos os atos necessários à plena eficácia societária da presente doação, inclusive:</p>");
        sb.AppendLine("<p>I — assinatura da correspondente alteração contratual da sociedade;</p>");
        sb.AppendLine("<p>II — atualização do quadro societário;</p>");
        sb.AppendLine("<p>III — providências perante a Junta Comercial;</p>");
        sb.AppendLine("<p>IV — recolhimento do ITCMD e demais atos acessórios.</p>");
    }

    private void MontarCl8_Declaracoes(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        var plural = diag.Doadores.Count > 1;

        sb.AppendLine("<h2>CLÁUSULA 8 — DECLARAÇÕES DO DOADOR</h2>");
        sb.AppendLine($"<p>O{(plural ? "s" : "")} doador{(plural ? "es declaram" : " declara")}, sob as penas da lei:</p>");
        sb.AppendLine("<p>I — que a presente doação não compromete sua subsistência;</p>");
        sb.AppendLine("<p>II — que a liberalidade respeita os limites legais aplicáveis;</p>");
        sb.AppendLine("<p>III — que as quotas doadas integram validamente seu patrimônio;</p>");
        sb.AppendLine("<p>IV — que a operação se insere em planejamento sucessório familiar estruturado.</p>");
    }

    private void MontarCl9_Foro(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 9 — DISPOSIÇÕES FINAIS</h2>");
        sb.AppendLine($"<p>As partes elegem o foro da Comarca de {diag.Comarca}, Estado de {diag.UfJunta}, para dirimir controvérsias oriundas deste instrumento, sem prejuízo de cláusula arbitral ou instrumento complementar posterior.</p>");
    }

    private void MontarFecho(StringBuilder sb, DiagnosticoDoacaoDestino diag)
    {
        sb.AppendLine("<p>[Local], [data].</p>");

        sb.AppendLine("<p><em>Doador(es):</em></p>");
        foreach (var d in diag.Doadores)
            sb.AppendLine($"<p>________________________________________<br/><strong>{d.Nome}</strong></p>");

        sb.AppendLine("<p><em>Donatário(s):</em></p>");
        foreach (var d in diag.Donatarios)
            sb.AppendLine($"<p>________________________________________<br/><strong>{d.Nome}</strong></p>");

        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private string Qualificar(ParteDoacao p)
    {
        var partes = new List<string> { $"<strong>{p.Nome.ToUpper()}</strong>", p.Nacionalidade };

        var ec = p.EstadoCivil switch
        {
            MaritalStatus.Solteiro => "solteiro(a)",
            MaritalStatus.Casado => "casado(a)",
            MaritalStatus.Divorciado => "divorciado(a)",
            MaritalStatus.Viuvo => "viúvo(a)",
            MaritalStatus.UniaoEstavel => "convivente em união estável",
            _ => "estado civil não informado"
        };
        if (p.EstadoCivil == MaritalStatus.Casado && p.RegimeBens.HasValue)
        {
            ec += " sob o regime de " + (p.RegimeBens.Value switch
            {
                MaritalRegime.ComunhaoParcial => "comunhão parcial de bens",
                MaritalRegime.ComunhaoUniversal => "comunhão universal de bens",
                MaritalRegime.SeparacaoConvencional => "separação convencional de bens",
                MaritalRegime.SeparacaoObrigatoria => "separação obrigatória de bens",
                MaritalRegime.ParticipacaoFinalNosAquestos => "participação final nos aquestos",
                _ => p.RegimeBens.Value.ToString().ToLower()
            });
        }
        partes.Add(ec);
        partes.Add(p.Profissao);
        partes.Add($"portador(a) do RG nº {p.Rg} ({p.RgOrgao})");
        partes.Add($"inscrito(a) no CPF sob nº {p.Cpf}");
        partes.Add($"residente e domiciliado(a) à {p.EnderecoCompleto}");

        return $"<p>{string.Join(", ", partes)};</p>";
    }

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
