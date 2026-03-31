#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 4.0 — Contrato Social Master da Célula Destino
// "Plataforma societária de transmissão intergeracional."
//
// 13 cláusulas + 3 blocos condicionais (BC-D1, BC-D2, BC-D3)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Diagnóstico para montagem do contrato social da célula Destino.
/// Mais enxuto que o Cofre — sem integralização de imóvel, sem Tema 796.
/// </summary>
public class DiagnosticoDestino
{
    public string NomeEmpresarial { get; set; } = string.Empty;
    public Address Endereco { get; set; } = new();
    public string Comarca { get; set; } = string.Empty;
    public decimal CapitalSocial { get; set; }
    public decimal ValorPorQuota { get; set; } = 1.00m;

    public List<SocioDestinoProfile> Socios { get; set; } = [];
    public int AdminIndex { get; set; } = 0;
    public bool AdminEhSocio { get; set; } = true;

    // Derivados
    public bool EhUnipessoal => Socios.Count == 1;
    public int TotalQuotas => Socios.Sum(s => s.Quotas);
    public SocioDestinoProfile Admin => Socios[AdminIndex];
}

public class SocioDestinoProfile
{
    public string Nome { get; set; } = string.Empty;
    public string Nacionalidade { get; set; } = "brasileira";
    public MaritalStatus EstadoCivil { get; set; } = MaritalStatus.NaoInformado;
    public MaritalRegime? RegimeBens { get; set; }
    public bool UniaoEstavel { get; set; }
    public string Profissao { get; set; } = string.Empty;
    public string Rg { get; set; } = string.Empty;
    public string RgOrgao { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string EnderecoCompleto { get; set; } = string.Empty;

    public int Quotas { get; set; }
    public decimal Percentual { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// VALIDAÇÃO + MONTAGEM
// ═══════════════════════════════════════════════════════════════

public class DestinoContratoService
{
    public ResultadoMontagem Montar(DiagnosticoDestino diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem
            {
                Sucesso = false,
                Validacao = ToResultadoValidacao(val),
            };

        var sb = new StringBuilder();
        var uni = diag.EhUnipessoal;
        var suj = uni ? "o sócio único" : "os sócios";
        var Suj = uni ? "O sócio único" : "Os sócios";

        // ── TÍTULO ───────────────────────────────────────
        sb.AppendLine($"<h1>CONTRATO SOCIAL DE {diag.NomeEmpresarial.ToUpper()} LTDA.</h1>");

        // ── PREÂMBULO (BC-D1: unipessoal) ────────────────
        if (uni)
        {
            sb.AppendLine("<p>Pelo presente instrumento particular, o único sócio abaixo qualificado:</p>");
            sb.AppendLine(Qualificar(diag.Socios[0]));
            sb.AppendLine("<p>constitui, por este instrumento, uma sociedade empresária limitada unipessoal, que se regerá pelas disposições aplicáveis do Código Civil e pelas cláusulas e condições seguintes.</p>");
        }
        else
        {
            sb.AppendLine("<p>Pelo presente instrumento particular, as partes abaixo qualificadas:</p>");
            foreach (var s in diag.Socios) sb.AppendLine(Qualificar(s));
            sb.AppendLine("<p>têm entre si justo e contratado constituir uma sociedade empresária limitada, que se regerá pelas disposições aplicáveis do Código Civil e pelas cláusulas e condições seguintes.</p>");
        }

        // ── CL.1 DENOMINAÇÃO ─────────────────────────────
        var tipo = uni ? "sociedade empresária limitada unipessoal" : "sociedade empresária limitada";
        sb.AppendLine("<h2>CLÁUSULA 1 — DENOMINAÇÃO, TIPO, SEDE E PRAZO</h2>");
        sb.AppendLine($"<p>A sociedade gira sob a denominação <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, constituída sob a forma de {tipo}, com sede em {EnderecoCompleto(diag.Endereco)}, no Município de {diag.Endereco.Cidade}, Estado de {diag.Endereco.Uf}, podendo abrir e encerrar filiais por deliberação {suj}, observadas as formalidades legais.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O prazo de duração da sociedade é indeterminado.</p>");

        // ── CL.2 OBJETO SOCIAL ───────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 2 — OBJETO SOCIAL</h2>");
        sb.AppendLine("<p>A sociedade tem por objeto:</p>");
        sb.AppendLine("<p>I — a participação em outras sociedades, na qualidade de sócia, quotista ou acionista;</p>");
        sb.AppendLine("<p>II — a administração de participações societárias próprias;</p>");
        sb.AppendLine("<p>III — a organização e acomodação societária de patrimônio familiar para fins de planejamento sucessório e governança patrimonial;</p>");
        sb.AppendLine("<p>IV — o exercício dos direitos inerentes aos ativos de sua titularidade, sem prejuízo de instrumentos próprios de doação, usufruto e governança.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A sociedade tem vocação sucessória e patrimonial-familiar, servindo como plataforma de transmissão intergeracional por quotas, na forma da lei e dos instrumentos societários e civis que a complementarem.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A sociedade não se confunde com célula operacional do grupo familiar, podendo a estrutura patrimonial e societária ser complementada por acordo de quotistas, instrumento de doação, reserva de usufruto e demais atos correlatos.</p>");

        // ── CL.3 CAPITAL ─────────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 3 — CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>O capital social é de {Fmt(diag.CapitalSocial)} ({Ext(diag.CapitalSocial)}), dividido em {diag.TotalQuotas} ({ExtN(diag.TotalQuotas)}) quotas, no valor nominal de {Fmt(diag.ValorPorQuota)} cada uma, totalmente subscritas pelo{(uni ? " sócio único" : "s sócios")} na seguinte proporção:</p>");
        foreach (var s in diag.Socios)
        {
            var vt = s.Quotas * diag.ValorPorQuota;
            sb.AppendLine($"<p>• <strong>{s.Nome}</strong>: {s.Quotas} ({ExtN(s.Quotas)}) quotas, no valor total de {Fmt(vt)};</p>");
        }
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> O capital social poderá ser integralizado em moeda corrente nacional, na forma e no prazo ajustados pelos sócios.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A integralização inicial observará a exata proporção de participação de cada sócio.</p>");

        // ── CL.4 FINALIDADE ESTRUTURAL ───────────────────
        // BC-D3: sempre presente na Destino
        sb.AppendLine("<h2>CLÁUSULA 4 — FINALIDADE ESTRUTURAL DA CÉLULA DESTINO</h2>");
        sb.AppendLine($"<p>{Suj} declara{(uni ? "" : "m")} que a presente sociedade é organizada como célula de transferência patrimonial, vocacionada à futura reorganização da titularidade das quotas em favor de sucessores, herdeiros ou beneficiários definidos pelo{(uni ? " sócio único" : "s sócios")}, mediante os instrumentos jurídicos cabíveis.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A presente cláusula não substitui os instrumentos próprios de doação, usufruto, acordo de quotistas ou demais atos de planejamento sucessório, que poderão complementar esta estrutura.</p>");

        // ── CL.5 RESPONSABILIDADE ────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 5 — RESPONSABILIDADE DOS SÓCIOS</h2>");
        sb.AppendLine(uni
            ? "<p>A responsabilidade do sócio único é restrita ao valor de suas quotas.</p>"
            : "<p>A responsabilidade de cada sócio é restrita ao valor de suas quotas, mas todos respondem solidariamente pela integralização do capital social.</p>");

        // ── CL.6 ADMINISTRAÇÃO ───────────────────────────
        var adm = diag.Admin;
        sb.AppendLine("<h2>CLÁUSULA 6 — ADMINISTRAÇÃO</h2>");
        sb.AppendLine($"<p>A administração da sociedade caberá a <strong>{adm.Nome}</strong>, {adm.Nacionalidade}, {FmtEC(adm)}, {adm.Profissao}, {(diag.AdminEhSocio ? "sócio(a)" : "não sócio(a)")}, com poderes para praticar os atos necessários à gestão da sociedade, observadas as limitações deste contrato.</p>");

        if (!uni)
        {
            sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> Dependem de deliberação {suj}:</p>");
            sb.AppendLine("<p>a) a alteração da estrutura sucessória da sociedade;</p>");
            sb.AppendLine("<p>b) a admissão de terceiros estranhos ao grupo familiar, salvo nos casos previstos em instrumentos próprios;</p>");
            sb.AppendLine("<p>c) a alienação de participações ou ativos relevantes;</p>");
            sb.AppendLine("<p>d) a prática de atos que desvirtuem a função sucessória da célula Destino.</p>");
        }

        sb.AppendLine($"<p><strong>Parágrafo {(uni ? "único" : "segundo")}.</strong> O administrador declara, sob as penas da lei, que não está impedido de exercer a administração da sociedade.</p>");

        // ── CL.7 DELIBERAÇÕES ────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 7 — DELIBERAÇÕES SOCIAIS</h2>");
        sb.AppendLine(uni
            ? "<p>As deliberações sociais serão tomadas pelo sócio único, na forma da lei e deste contrato, mediante documento escrito.</p>"
            : "<p>As deliberações sociais serão tomadas pelos sócios na forma da lei e deste contrato, em reunião, assembleia ou documento assinado por todos.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> As matérias legalmente sujeitas a quórum específico observarão a lei, e as matérias ligadas à reorganização sucessória poderão ser complementadas por acordo de quotistas.</p>");

        // ── CL.8 CESSÃO E TRANSMISSÃO ────────────────────
        sb.AppendLine("<h2>CLÁUSULA 8 — CESSÃO, TRANSMISSÃO E REORGANIZAÇÃO DE QUOTAS</h2>");
        sb.AppendLine("<p>A cessão ou transmissão de quotas obedecerá ao presente contrato, à legislação aplicável e aos instrumentos complementares que vierem a disciplinar a governança familiar e sucessória.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A reorganização da titularidade das quotas, inclusive por doação, poderá ser implementada por ato próprio, observadas as exigências legais, tributárias e societárias.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> O ingresso de novo quotista por liberalidade, sucessão, doação ou outro título dependerá da observância das regras legais, contratuais e, se existente, do acordo de quotistas.</p>");

        // ── CL.9 FALECIMENTO ─────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 9 — FALECIMENTO, INCAPACIDADE E REFLEXOS FAMILIARES</h2>");
        sb.AppendLine("<p>O falecimento ou incapacidade de sócio não acarretará dissolução automática da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A disciplina da sucessão das quotas, do eventual usufruto, da permanência de herdeiros e da governança entre os sucessores poderá ser tratada em instrumento próprio, sem prejuízo das regras legais aplicáveis.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Em caso de dissolução de vínculo conjugal ou convivencial de qualquer sócio, os reflexos patrimoniais sobre as quotas serão tratados segundo a lei, o regime de bens e os instrumentos complementares existentes.</p>");

        // ── CL.10 ACORDO DE QUOTISTAS ────────────────────
        // BC-D3: sempre presente
        sb.AppendLine("<h2>CLÁUSULA 10 — ACORDO DE QUOTISTAS E INSTRUMENTOS COMPLEMENTARES</h2>");
        sb.AppendLine($"<p>{Suj} poderá{(uni ? "" : "ão")} celebrar acordo de quotistas, instrumento de doação de quotas, pacto de usufruto, protocolo familiar e demais atos complementares, para disciplinar governança, voto, administração, restrições à circulação, direitos econômicos, sucessão e demais matérias conexas.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Os instrumentos complementares poderão ser arquivados, quando cabível, para produção de efeitos perante terceiros.</p>");

        // ── CL.11 EXERCÍCIO ──────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 11 — EXERCÍCIO SOCIAL E RESULTADOS</h2>");
        sb.AppendLine("<p>O exercício social encerra-se em 31 de dezembro de cada ano, quando serão levantadas as demonstrações contábeis na forma da legislação aplicável.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A destinação de resultados observará a lei, este contrato e os instrumentos complementares que disciplinem a governança da célula Destino.</p>");

        // ── CL.12 DISSOLUÇÃO ─────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 12 — DISSOLUÇÃO E LIQUIDAÇÃO</h2>");
        sb.AppendLine($"<p>A sociedade dissolver-se-á nos casos previstos em lei ou por deliberação {suj}.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Em caso de liquidação, deverá ser preservada, tanto quanto possível, a coerência entre a estrutura patrimonial da sociedade e a finalidade sucessória que orientou sua constituição.</p>");

        // ── CL.13 FORO ───────────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 13 — FORO</h2>");
        sb.AppendLine($"<p>Fica eleito o foro da Comarca de {diag.Comarca}, Estado de {diag.Endereco.Uf}, para dirimir as controvérsias oriundas deste contrato, sem prejuízo de eventual cláusula arbitral ou instrumento complementar posterior.</p>");

        // ── FECHO ────────────────────────────────────────
        var vias = diag.Socios.Count + 1;
        sb.AppendLine($"<p>E, por estar{(uni ? "" : "em")} assim justo{(uni ? "" : "s")} e contratado{(uni ? "" : "s")}, assina{(uni ? "" : "m")} o presente instrumento em {vias} ({ExtN(vias)}) vias de igual teor e forma, juntamente com duas testemunhas.</p>");
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var s in diag.Socios)
            sb.AppendLine($"<p>________________________________________<br/><strong>{s.Nome}</strong></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        var blocos = new List<string>();
        if (uni) blocos.Add("BC-D1-Unipessoal");
        if (diag.Socios.Any(s => s.EstadoCivil == MaritalStatus.Casado || s.UniaoEstavel)) blocos.Add("BC-D2-RegimeBens");
        blocos.Add("BC-D3-InstrumentosComplementares");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToResultadoValidacao(val),
            BlocosCondicionaisAtivados = blocos,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoDestino diag)
    {
        var r = new ValidationResult();

        if (string.IsNullOrWhiteSpace(diag.NomeEmpresarial))
            r.AddError("DESTINO_001", "Nome empresarial é obrigatório.");

        if (diag.CapitalSocial <= 0)
            r.AddError("DESTINO_002", "Capital social deve ser maior que zero.");

        if (!diag.Socios.Any())
            r.AddError("DESTINO_003", "Deve haver ao menos um sócio.");

        foreach (var s in diag.Socios)
        {
            if (string.IsNullOrWhiteSpace(s.Cpf))
                r.AddError("DESTINO_004", $"O sócio {s.Nome} não possui CPF.");
        }

        var pct = diag.Socios.Sum(s => s.Percentual);
        if (diag.Socios.Any() && Math.Abs(pct - 100m) > 0.01m)
            r.AddError("DESTINO_005", $"Percentuais somam {pct:F2}%, devem somar 100%.");

        // Art. 977 CC
        if (diag.Socios.Count == 2)
        {
            var regimesBloqueados = new[] { MaritalRegime.ComunhaoUniversal, MaritalRegime.SeparacaoObrigatoria };
            if (diag.Socios.Any(s => s.EstadoCivil == MaritalStatus.Casado &&
                s.RegimeBens.HasValue && regimesBloqueados.Contains(s.RegimeBens.Value)))
            {
                r.AddError("DESTINO_006", "Art. 977 CC: vedação de sociedade entre cônjuges neste regime como únicos sócios.");
            }
        }

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private string Qualificar(SocioDestinoProfile s)
    {
        var partes = new List<string> { $"<strong>{s.Nome.ToUpper()}</strong>", s.Nacionalidade };
        var ec = FmtEC(s);
        partes.Add(ec);
        partes.Add(s.Profissao);
        partes.Add($"portador(a) do RG nº {s.Rg} ({s.RgOrgao})");
        partes.Add($"inscrito(a) no CPF sob nº {s.Cpf}");
        partes.Add($"residente e domiciliado(a) à {s.EnderecoCompleto}");
        return $"<p>{string.Join(", ", partes)};</p>";
    }

    private static string FmtEC(SocioDestinoProfile s)
    {
        // BC-D2: regime de bens na qualificação
        var ec = s.EstadoCivil switch
        {
            MaritalStatus.Solteiro => "solteiro(a)",
            MaritalStatus.Casado => "casado(a)",
            MaritalStatus.Divorciado => "divorciado(a)",
            MaritalStatus.Viuvo => "viúvo(a)",
            MaritalStatus.UniaoEstavel => "convivente em união estável",
            _ => "estado civil não informado"
        };

        if (s.UniaoEstavel)
            ec = "convivente em união estável";

        if (s.EstadoCivil == MaritalStatus.Casado && s.RegimeBens.HasValue)
        {
            ec += " sob o regime de " + (s.RegimeBens.Value switch
            {
                MaritalRegime.ComunhaoParcial => "comunhão parcial de bens",
                MaritalRegime.ComunhaoUniversal => "comunhão universal de bens",
                MaritalRegime.SeparacaoConvencional => "separação convencional de bens",
                MaritalRegime.SeparacaoObrigatoria => "separação obrigatória de bens",
                MaritalRegime.ParticipacaoFinalNosAquestos => "participação final nos aquestos",
                _ => s.RegimeBens.Value.ToString().ToLower()
            });
        }

        return ec;
    }

    private static string EnderecoCompleto(Address e)
        => string.Join(", ", new[] { e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, $"CEP {e.Cep}" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    private static ResultadoValidacao ToResultadoValidacao(ValidationResult vr)
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
