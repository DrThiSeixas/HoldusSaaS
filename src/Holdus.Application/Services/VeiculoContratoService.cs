#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 8.0 — Contrato Social Master da Célula Veículo
// "A Veículo segura o comando."
//
// 16 cláusulas + 4 blocos condicionais
// Código: VEICULO.CONSTITUICAO.V1
//
// Pilares: regência supletiva Lei S.A., classes de quotas,
// reserva de capital, cláusula de call, atos reflexos.
// ═══════════════════════════════════════════════════════════════

public class VeiculoContratoService
{
    public ResultadoMontagem Montar(VeiculoProfile v)
    {
        // Validar via VeiculoProfileValidator
        var validator = new VeiculoProfileValidator();
        var val = validator.Validate(v);
        if (val.HasErrors)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();
        var uni = v.Socios.Count == 1;

        // ── TÍTULO + PREÂMBULO ───────────────────────────
        sb.AppendLine($"<h1>CONTRATO SOCIAL DE {v.NomeEmpresarial.ToUpper()} LTDA.</h1>");
        sb.AppendLine("<p>Pelo presente instrumento particular, as partes abaixo qualificadas:</p>");
        foreach (var s in v.Socios)
            sb.AppendLine($"<p><strong>{s.NomeCompleto.ToUpper()}</strong>, CPF nº {s.Cpf};</p>");
        sb.AppendLine("<p>têm entre si justo e contratado constituir uma sociedade empresária limitada, que se regerá pelas disposições aplicáveis do Código Civil, pela regência supletiva da Lei das Sociedades por Ações, na forma deste contrato, e pelas cláusulas e condições seguintes.</p>");

        // ── CL.1 DENOMINAÇÃO ─────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 1 — DENOMINAÇÃO, TIPO, SEDE E PRAZO</h2>");
        sb.AppendLine($"<p>A sociedade gira sob a denominação <strong>{v.NomeEmpresarial.ToUpper()} LTDA.</strong>, constituída sob a forma de sociedade empresária limitada, com sede em {FmtEnd(v.EnderecoSede)}, no Município de {v.EnderecoSede.Cidade}, Estado de {v.EnderecoSede.Uf}, podendo abrir, alterar e encerrar filiais por deliberação dos sócios, observadas as formalidades legais aplicáveis.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O prazo de duração da sociedade é indeterminado.</p>");

        // ── CL.2 REGÊNCIA SUPLETIVA ─────────────────────
        sb.AppendLine("<h2>CLÁUSULA 2 — REGÊNCIA SUPLETIVA</h2>");
        sb.AppendLine("<p>Nos termos do art. 1.053, parágrafo único, do Código Civil, a sociedade adota regência supletiva da Lei nº 6.404/1976, naquilo que for compatível com a natureza da sociedade limitada e não contrariar este contrato nem as normas cogentes do regime das limitadas.</p>");

        // ── CL.3 OBJETO SOCIAL ──────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 3 — OBJETO SOCIAL</h2>");
        sb.AppendLine("<p>A sociedade tem por objeto:</p>");
        sb.AppendLine("<p>I — a participação em outras sociedades, no país ou no exterior, na qualidade de sócia, quotista ou acionista;</p>");
        sb.AppendLine("<p>II — a administração de participações societárias próprias;</p>");
        sb.AppendLine("<p>III — a concentração e o exercício do comando político da estrutura societária da família, mediante titularidade e governança de participações;</p>");
        sb.AppendLine("<p>IV — a reorganização societária interna, inclusive para fins de concentração de controle, acomodação de classes de quotas e implementação de atos societários reflexos compatíveis com sua finalidade.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A sociedade é estruturada como célula de comando societário, não se confundindo com célula de operação patrimonial direta nem com célula sucessória principal.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A finalidade da sociedade é preservar o controle político da arquitetura societária, admitindo separação técnica entre titularidade econômica e comando, nos limites deste contrato.</p>");

        // ── CL.4 CAPITAL E CLASSES ──────────────────────
        var totalOrd = v.TotalQuotasOrdinarias;
        var totalPref = v.TotalQuotasPreferenciais;
        var prefClasse = v.ClassesQuotas.FirstOrDefault(c => c.Classe == ClasseQuota.Preferencial);
        var ordClasse = v.ClassesQuotas.FirstOrDefault(c => c.Classe == ClasseQuota.Ordinaria);

        sb.AppendLine("<h2>CLÁUSULA 4 — CAPITAL SOCIAL E CLASSES DE QUOTAS</h2>");
        sb.AppendLine($"<p>O capital social da sociedade é de {Fmt(v.CapitalSocial)} ({Ext(v.CapitalSocial)}), dividido em:</p>");
        if (ordClasse != null)
            sb.AppendLine($"<p>I — {totalOrd} ({ExtN(totalOrd)}) quotas ordinárias, no valor nominal de {Fmt(ordClasse.ValorNominalUnitario)} cada; e</p>");
        if (prefClasse != null)
            sb.AppendLine($"<p>II — {totalPref} ({ExtN(totalPref)}) quota(s) preferencial(is), no valor nominal de {Fmt(prefClasse.ValorNominalUnitario)}.</p>");

        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> As quotas ordinárias serão distribuídas, em regra, na mesma quantidade e na mesma proporção da distribuição societária-base adotada na célula Destino, conforme a lógica estrutural do método.</p>");

        var prefTitulares = v.Socios.Where(s => s.QuotasPreferenciais > 0).Select(s => s.NomeCompleto);
        sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> A(s) quota(s) preferencial(is) pertencerá(ão), inicialmente, a <strong>{string.Join(" e ", prefTitulares)}</strong>, conforme a estrutura de controle definida pelos instituidores do arranjo.</p>");

        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> As quotas são de classes distintas, com direitos econômicos e políticos diversos, na forma deste contrato.</p>");

        // ── CL.5 TITULARIDADE INICIAL ────────────────────
        sb.AppendLine("<h2>CLÁUSULA 5 — TITULARIDADE INICIAL DAS QUOTAS</h2>");
        sb.AppendLine("<p>As quotas são subscritas e distribuídas da seguinte forma:</p>");
        foreach (var s in v.Socios)
        {
            var partes = new List<string>();
            if (s.QuotasOrdinarias > 0 && ordClasse != null)
                partes.Add($"{s.QuotasOrdinarias} quotas ordinárias, no valor total de {Fmt(s.QuotasOrdinarias * ordClasse.ValorNominalUnitario)}");
            if (s.QuotasPreferenciais > 0 && prefClasse != null)
                partes.Add($"{s.QuotasPreferenciais} quota(s) preferencial(is), no valor total de {Fmt(s.QuotasPreferenciais * prefClasse.ValorNominalUnitario)}");
            sb.AppendLine($"<p>• <strong>{s.NomeCompleto}</strong>: {string.Join("; e ", partes)};</p>");
        }
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O quadro societário inicial poderá ser ajustado por ato próprio para refletir a engenharia de controle adotada no projeto.</p>");

        // ── CL.6 INTEGRALIZAÇÃO ─────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 6 — INTEGRALIZAÇÃO DO CAPITAL SOCIAL</h2>");
        sb.AppendLine("<p>O capital social será integralizado em moeda corrente nacional ou mediante conferência de bens ou direitos, na forma e prazo ajustados entre os sócios.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> Quando o modelo concreto exigir que os sócios aportem à Veículo valor correspondente ao total do capital social da Cofre, a integralização observará essa parametrização em instrumento e demonstração próprios.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Se a contribuição do subscritor ultrapassar o valor nominal das quotas emitidas, o excedente poderá ser lançado em reserva de capital, nos limites da arquitetura contratual aqui adotada e com base no art. 13, § 2º, da Lei nº 6.404/1976, aplicado supletivamente.</p>");

        // ── CL.7 DIREITOS DAS ORDINÁRIAS (BC-V4) ────────
        sb.AppendLine("<h2>CLÁUSULA 7 — DIREITOS DAS QUOTAS ORDINÁRIAS</h2>");
        sb.AppendLine("<p>As quotas ordinárias conferem a seus titulares:</p>");
        sb.AppendLine("<p>I — direitos econômicos ordinários, na proporção de sua participação;</p>");

        switch (v.VotoOrdinarias)
        {
            case VotoOrdinariaMode.VotoSimplesResidual:
                sb.AppendLine("<p>II — participação nas deliberações sociais com voto simples, na forma deste contrato;</p>");
                break;
            case VotoOrdinariaMode.VotoLimitadoOrdinarias:
                sb.AppendLine("<p>II — participação nas deliberações sobre matérias ordinárias, sem poder de voto sobre matérias estruturais reservadas à quota preferencial;</p>");
                break;
            case VotoOrdinariaMode.SemVotoEmControle:
                sb.AppendLine("<p>II — participação nas deliberações sociais, ressalvado que as matérias de controle são de competência exclusiva do titular da quota preferencial;</p>");
                break;
            default:
                sb.AppendLine("<p>II — participação nas deliberações sociais, na forma deste contrato;</p>");
                break;
        }

        sb.AppendLine("<p>III — sujeição às restrições de circulação, preferência e reorganização previstas neste contrato.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> As quotas ordinárias compõem, por padrão, a camada econômica transferível da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A circulação das quotas ordinárias poderá ser restrita para preservar a coerência da estrutura de controle e a eventual futura aquisição pela célula Destino, por ato próprio.</p>");

        // ── CL.8 DIREITOS DA PREFERENCIAL ────────────────
        var pesoVoto = prefClasse?.PesoVoto ?? (totalOrd + 1);
        sb.AppendLine("<h2>CLÁUSULA 8 — DIREITOS DA QUOTA PREFERENCIAL</h2>");
        sb.AppendLine("<p>A quota preferencial confere a seu titular direitos políticos reforçados, nos termos deste contrato.</p>");
        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> O peso de voto da quota preferencial será equivalente a {pesoVoto} ({ExtN(pesoVoto)}) vezes o peso de voto de cada quota ordinária.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Compete privativamente, ou com poder de veto do titular da quota preferencial, deliberar sobre:</p>");
        var materias = v.MateriasReservadasPreferencial;
        for (int i = 0; i < materias.Count; i++)
            sb.AppendLine($"<p>{Chr(i)} {materias[i].ToLower()};</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A quota preferencial não precisa concentrar a maior parte da economia da sociedade; sua função primordial é a preservação do comando político da estrutura.</p>");

        // ── CL.9 ADMINISTRAÇÃO ──────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 9 — ADMINISTRAÇÃO</h2>");
        sb.AppendLine($"<p>A administração da sociedade caberá a <strong>{v.AdministradorNome}</strong>, {(v.AdministradorEhSocio ? "sócio(a)" : "não sócio(a)")}, com poderes para praticar os atos necessários à gestão da sociedade, observadas as limitações deste contrato.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A administração deverá atuar em conformidade com a função da sociedade como célula de comando, abstendo-se de desviar a Veículo para atividade operacional própria estranha ao seu objeto.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Dependem de deliberação observando-se os direitos da quota preferencial:</p>");
        sb.AppendLine("<p>I — aquisição ou alienação de participações relevantes;</p>");
        sb.AppendLine("<p>II — alteração da estrutura de classes de quotas;</p>");
        sb.AppendLine("<p>III — reorganizações internas entre Veículo, Cofre e Destino;</p>");
        sb.AppendLine("<p>IV — constituição de ônus sobre participações societárias relevantes.</p>");

        // ── CL.10 RESERVA DE CAPITAL (BC-V2: opcional) ──
        if (v.TemReservaCapital)
        {
            sb.AppendLine("<h2>CLÁUSULA 10 — RESERVA DE CAPITAL</h2>");
            sb.AppendLine("<p>Havendo contribuição de subscritor em valor superior ao nominal das quotas subscritas, o excedente será registrado em reserva de capital, na forma da legislação aplicada supletivamente e das normas contábeis pertinentes.</p>");
            sb.AppendLine("<p><strong>Parágrafo único.</strong> Este módulo só será utilizado quando a operação concreta assim exigir e estiver documentalmente suportada.</p>");
        }

        // ── CL.11 CLÁUSULA DE CALL (BC-V3: parametrizável) ─
        if (v.TemClausulaCall && v.ClausulaCall != null)
        {
            var call = v.ClausulaCall;
            sb.AppendLine("<h2>CLÁUSULA 11 — CLÁUSULA DE CALL DA QUOTA PREFERENCIAL</h2>");

            var evento = call.EventoGatilho switch
            {
                CallEventoGatilho.Falecimento => "falecimento do titular da quota preferencial",
                CallEventoGatilho.Incapacidade => "incapacidade superveniente do titular da quota preferencial",
                CallEventoGatilho.Saida => "saída voluntária do titular da quota preferencial",
                CallEventoGatilho.RenunciaControle => "renúncia ao controle pelo titular da quota preferencial",
                CallEventoGatilho.Personalizado => call.DescricaoEventoPersonalizado ?? "[evento personalizado]",
                _ => "[evento a definir]"
            };

            sb.AppendLine($"<p>Ocorrendo o evento de {evento}, fica atribuída a <strong>{call.CompradoresDescritos}</strong> opção de compra da quota preferencial, observadas as seguintes condições:</p>");
            sb.AppendLine($"<p>I — evento disparador: {evento};</p>");
            sb.AppendLine($"<p>II — legitimado(s) a exercer a opção: {call.CompradoresDescritos};</p>");
            sb.AppendLine($"<p>III — prazo para exercício: {call.PrazoExercicioDias ?? 90} ({ExtN(call.PrazoExercicioDias ?? 90)}) dias;</p>");
            sb.AppendLine($"<p>IV — preço ou critério de preço: {call.MetodoPreco};</p>");
            sb.AppendLine($"<p>V — forma de pagamento: {call.CondicoesPagamento ?? "à vista ou em condições a serem ajustadas"};</p>");
            sb.AppendLine("<p>VI — efeitos da não execução da opção: a questão será tratada conforme deliberação dos sócios remanescentes, observadas as prerrogativas deste contrato.</p>");
        }

        // ── CL.12 RESTRIÇÕES À CIRCULAÇÃO ───────────────
        sb.AppendLine("<h2>CLÁUSULA 12 — RESTRIÇÕES À CIRCULAÇÃO DAS QUOTAS</h2>");
        sb.AppendLine("<p>A cessão, alienação, promessa de cessão, oneração ou qualquer forma de disposição das quotas observará as restrições deste contrato.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> As quotas ordinárias somente poderão circular na forma admitida por este contrato e pelos atos de reorganização interna da estrutura.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A quota preferencial terá circulação mais restrita, dada sua função de preservação do controle político.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A transferência de quotas dependerá da prática dos atos societários e registrais correspondentes, inclusive quando envolver reorganização entre as células do método.</p>");

        // ── CL.13 ATOS REFLEXOS ─────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 13 — ATOS REFLEXOS E REORGANIZAÇÃO INTERNA</h2>");
        sb.AppendLine("<p>As partes reconhecem que a funcionalidade da Veículo depende de atos reflexos correlatos em outras sociedades da estrutura, especialmente:</p>");
        sb.AppendLine("<p>I — alteração contratual da célula Cofre para refletir a transferência de titularidade de suas quotas à Veículo;</p>");
        sb.AppendLine("<p>II — futura alteração contratual da Veículo para refletir a aquisição, pela célula Destino, das quotas ordinárias, na forma da modelagem concreta do caso.</p>");

        // ── CL.14 EXERCÍCIO SOCIAL ──────────────────────
        sb.AppendLine("<h2>CLÁUSULA 14 — EXERCÍCIO SOCIAL E RESULTADOS</h2>");
        sb.AppendLine("<p>O exercício social encerra-se em 31 de dezembro de cada ano, quando serão levantadas as demonstrações contábeis da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> A distribuição de resultados observará a classe das quotas, a legislação aplicável, este contrato e a finalidade da Veículo como célula de comando societário.</p>");

        // ── CL.15 DISSOLUÇÃO ────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 15 — DISSOLUÇÃO E LIQUIDAÇÃO</h2>");
        sb.AppendLine("<p>A sociedade dissolver-se-á nas hipóteses previstas em lei ou por deliberação dos sócios, observadas as prerrogativas da quota preferencial.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Em caso de dissolução, deverá ser preservada, tanto quanto possível, a coerência da arquitetura entre Veículo, Cofre e Destino.</p>");

        // ── CL.16 FORO ──────────────────────────────────
        sb.AppendLine("<h2>CLÁUSULA 16 — FORO</h2>");
        sb.AppendLine($"<p>Fica eleito o foro da Comarca de {v.EnderecoSede.Cidade}, Estado de {v.EnderecoSede.Uf}, para dirimir controvérsias oriundas deste contrato, sem prejuízo de cláusula arbitral, se adotada.</p>");

        // ── FECHO ────────────────────────────────────────
        var vias = v.Socios.Count + 1;
        sb.AppendLine($"<p>E, por estarem assim justos e contratados, assinam o presente instrumento em {vias} ({ExtN(vias)}) vias de igual teor e forma, juntamente com duas testemunhas.</p>");
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var s in v.Socios)
            sb.AppendLine($"<p>________________________________________<br/><strong>{s.NomeCompleto}</strong></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");

        // Blocos condicionais
        var blocos = new List<string>();
        if (uni) blocos.Add("BC-V1-Unipessoal");
        if (v.TemReservaCapital) blocos.Add("BC-V2-ReservaCapital");
        if (v.TemClausulaCall) blocos.Add("BC-V3-Call");
        blocos.Add($"BC-V4-Voto-{v.VotoOrdinarias}");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = blocos,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private static string Chr(int i) => i switch
    {
        0 => "a)", 1 => "b)", 2 => "c)", 3 => "d)", 4 => "e)",
        5 => "f)", 6 => "g)", 7 => "h)", _ => $"{(char)('a' + i)})"
    };

    private static string FmtEnd(Address e)
        => string.Join(", ", new[] { e.Logradouro, e.Numero, e.Complemento, e.Bairro, e.Cidade, e.Uf, $"CEP {e.Cep}" }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

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
