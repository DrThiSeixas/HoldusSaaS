using Holdus.Domain.Entities;
using Holdus.Domain.Enums;
using System.Text;

namespace Holdus.Application.Services;

/// <summary>
/// Etapa C do motor documental — Montagem do Contrato Social do Cofre.
/// Carrega a minuta-base, resolve os 7 blocos condicionais, e retorna HTML.
/// </summary>
public class MontagemCofreService
{
    private readonly ValidacaoCofreService _validacao = new();

    /// <summary>
    /// Monta o contrato social completo a partir do diagnóstico.
    /// Retorna o HTML do documento ou erros de validação.
    /// </summary>
    public ResultadoMontagem Montar(DiagnosticoCofre diag)
    {
        // Etapa B — Validação
        var validacao = _validacao.Validar(diag);
        if (!validacao.PodeGerar)
        {
            return new ResultadoMontagem
            {
                Sucesso = false,
                Validacao = validacao,
            };
        }

        // Etapa C — Montagem
        var sb = new StringBuilder();

        MontarTitulo(sb, diag);
        MontarPreambulo(sb, diag);
        MontarClausula1_Denominacao(sb, diag);
        MontarClausula2_Objeto(sb, diag);
        MontarClausula3_Capital(sb, diag);
        MontarClausula4_Integralizacao(sb, diag);
        MontarClausula5_Responsabilidade(sb, diag);
        MontarClausula6_Administracao(sb, diag);
        MontarClausula7_Deliberacoes(sb, diag);
        MontarClausula8_Exercicio(sb, diag);
        MontarClausula9_Cessao(sb, diag);
        MontarClausula10_Remisso(sb, diag);
        MontarClausula11_Falecimento(sb, diag);
        MontarClausula12_Acordo(sb, diag);
        MontarClausula13_Dissolucao(sb, diag);
        MontarClausula14_Declaracao(sb, diag);
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
    // TÍTULO
    // ═══════════════════════════════════════════════════════════

    private void MontarTitulo(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine($"<h1>CONTRATO SOCIAL DE {diag.NomeEmpresarial.ToUpper()} LTDA.</h1>");
    }

    // ═══════════════════════════════════════════════════════════
    // PREÂMBULO — BC1 (unipessoal vs pluripessoal)
    // ═══════════════════════════════════════════════════════════

    private void MontarPreambulo(StringBuilder sb, DiagnosticoCofre diag)
    {
        if (diag.TipoSociedade == TipoSociedadeLtda.Unipessoal)
        {
            // ── BC1: Unipessoal ──
            var socio = diag.Socios[0];
            sb.AppendLine("<p>Pelo presente instrumento particular, o único sócio abaixo qualificado:</p>");
            sb.AppendLine(MontarQualificacaoSocio(socio));
            sb.AppendLine("<p>constitui, por este instrumento, uma sociedade empresária limitada unipessoal, que se regerá pelas disposições aplicáveis do Código Civil e pelas cláusulas e condições seguintes.</p>");
        }
        else
        {
            // ── Pluripessoal ──
            sb.AppendLine("<p>Pelo presente instrumento particular, as partes abaixo qualificadas:</p>");

            foreach (var socio in diag.Socios)
            {
                sb.AppendLine(MontarQualificacaoSocio(socio));
            }

            sb.AppendLine("<p>têm entre si justo e contratado constituir uma sociedade empresária limitada, que se regerá pelas disposições aplicáveis do Código Civil e pelas cláusulas e condições seguintes. A limitada tem regime próprio no Código Civil; a responsabilidade dos sócios é restrita ao valor das quotas, com solidariedade pela integralização do capital; e o contrato social pode prever regência supletiva específica, se desejado.</p>");
        }

        // ── BC2: Cláusula condicional de ciência conjugal ──
        if (diag.Socios.Any(s => s.EstadoCivil == EstadoCivil.Casado || s.UniaoEstavel))
        {
            sb.AppendLine("<p>Os signatários declaram que a composição societária e a prática deste ato observam as restrições legais aplicáveis ao regime de bens e às relações conjugais ou convivenciais incidentes sobre o caso.</p>");
        }
    }

    /// <summary>
    /// Monta a qualificação completa de um sócio.
    /// BC2: inclui regime de bens e união estável quando aplicável.
    /// </summary>
    private string MontarQualificacaoSocio(SocioDiagnostico s)
    {
        var partes = new List<string>
        {
            $"<strong>{s.Nome.ToUpper()}</strong>",
            s.Nacionalidade,
        };

        // ── BC2: estado civil com regime de bens ──
        var ecTexto = FormatarEstadoCivil(s.EstadoCivil);
        if (s.UniaoEstavel)
        {
            ecTexto = "convivente em união estável";
            if (s.HaPactoConvivencia)
                ecTexto += " (com contrato de convivência)";
        }
        else if (s.EstadoCivil == EstadoCivil.Casado && s.RegimeBens.HasValue)
        {
            ecTexto += $" sob o regime de {FormatarRegimeBens(s.RegimeBens.Value)}";
        }
        partes.Add(ecTexto);

        partes.Add(s.Profissao);
        partes.Add($"portador(a) do RG nº {s.Rg} ({s.RgOrgao})");
        partes.Add($"inscrito(a) no CPF sob nº {s.Cpf}");
        partes.Add($"residente e domiciliado(a) à {s.EnderecoCompleto}");

        return $"<p>{string.Join(", ", partes)};</p>";
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULA 1 — DENOMINAÇÃO
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula1_Denominacao(StringBuilder sb, DiagnosticoCofre diag)
    {
        var tipoTexto = diag.TipoSociedade == TipoSociedadeLtda.Unipessoal
            ? "sociedade empresária limitada unipessoal"
            : "sociedade empresária limitada";

        sb.AppendLine("<h2>CLÁUSULA 1 — DENOMINAÇÃO, TIPO SOCIETÁRIO, SEDE, FORO E PRAZO</h2>");
        sb.AppendLine($"<p>A sociedade girará sob a denominação empresarial <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, constituída sob a forma de {tipoTexto}, com sede na {diag.Endereco.Completo}, no Município de {diag.Endereco.Cidade}, Estado de {diag.Endereco.Uf}, podendo abrir, transferir ou encerrar filiais, escritórios, depósitos ou quaisquer estabelecimentos no território nacional, por deliberação {(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "do sócio único" : "dos sócios")} e observadas as formalidades legais aplicáveis.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> O prazo de duração da sociedade é indeterminado.</p>");
        sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> O foro da sede social será o da Comarca de {diag.Comarca}, Estado de {diag.Endereco.Uf}, ressalvada eventual convenção arbitral posterior ou previsão específica em acordo de quotistas.</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULA 2 — OBJETO SOCIAL + BC6 (trava do Cofre)
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula2_Objeto(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 2 — FINALIDADE E OBJETO SOCIAL</h2>");
        sb.AppendLine("<p>A sociedade tem por objeto social:</p>");
        sb.AppendLine("<p>I — a participação em outras sociedades, no país ou no exterior, na qualidade de sócia, quotista ou acionista, inclusive para fins de controle societário;</p>");
        sb.AppendLine("<p>II — a administração de participações societárias próprias;</p>");
        sb.AppendLine("<p>III — o exercício dos direitos inerentes aos ativos societários de sua titularidade;</p>");
        sb.AppendLine("<p>IV — a prática de atos de organização, concentração e preservação patrimonial estritamente vinculados à sua finalidade patrimonial-societária.</p>");

        // ── BC6: Trava de função do Cofre ──
        if (diag.DeveTravaCofre)
        {
            sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A sociedade não exercerá atividade operacional própria perante terceiros, não se destinando à exploração empresarial direta de bens ou serviços.</p>");
            sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> Fica expressamente vedado à sociedade, salvo prévia alteração contratual deliberada pelo{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único" : "s sócios")} e correspondente revisão da sua arquitetura jurídica e tributária:</p>");
            sb.AppendLine("<p>a) explorar locação imobiliária como atividade própria;</p>");
            sb.AppendLine("<p>b) realizar compra e venda habitual de imóveis;</p>");
            sb.AppendLine("<p>c) promover loteamento, incorporação, construção para venda ou atividade equivalente;</p>");
            sb.AppendLine("<p>d) administrar imóveis de terceiros;</p>");
            sb.AppendLine("<p>e) prestar serviços a terceiros;</p>");
            sb.AppendLine("<p>f) assumir função de veículo operacional de linha de frente do grupo familiar.</p>");
            sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A finalidade da sociedade é a concentração patrimonial-societária e a preservação do núcleo patrimonial familiar, sem exposição negocial direta típica de sociedade operacional.</p>");
            sb.AppendLine("<p><strong>Parágrafo quarto.</strong> Para fins cadastrais e de classificação econômica, a sociedade adotará, em regra, CNAE compatível com holding não financeira (CNAE 6462-0/00), sem prejuízo de revisão técnica caso a realidade operacional do caso concreto exija adequação.</p>");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULA 3 — CAPITAL SOCIAL
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula3_Capital(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 3 — CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>O capital social da sociedade é de {FormatarMoeda(diag.CapitalSocial)} ({ValorExtenso(diag.CapitalSocial)}), dividido em {diag.TotalQuotas} ({NumeroExtenso(diag.TotalQuotas)}) quotas, no valor nominal de {FormatarMoeda(diag.ValorPorQuota)} cada uma, totalmente subscritas pelo{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único" : "s sócios")} na seguinte proporção:</p>");

        foreach (var socio in diag.Socios)
        {
            var valorTotal = socio.Quotas * diag.ValorPorQuota;
            sb.AppendLine($"<p>• <strong>{socio.Nome}</strong>: {socio.Quotas} ({NumeroExtenso(socio.Quotas)}) quotas, no valor total de {FormatarMoeda(valorTotal)};</p>");
        }

        sb.AppendLine("<p><strong>Parágrafo único.</strong> As quotas são indivisíveis em relação à sociedade, que não reconhecerá mais de um titular para cada quota, observadas as disposições deste contrato e eventual acordo de quotistas.</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULA 4 — INTEGRALIZAÇÃO + BC3/BC4/BC5
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula4_Integralizacao(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 4 — INTEGRALIZAÇÃO DO CAPITAL SOCIAL</h2>");
        sb.AppendLine($"<p>O capital social ora subscrito será integralizado da seguinte forma:</p>");

        int itemNum = 1;
        foreach (var socio in diag.Socios)
        {
            foreach (var integ in socio.Integralizacoes)
            {
                var romano = ToRomano(itemNum);

                switch (integ.Tipo)
                {
                    case TipoIntegralizacao.Dinheiro:
                        sb.AppendLine($"<p>{romano} — por <strong>{socio.Nome}</strong>, mediante dinheiro, no valor de {FormatarMoeda(integ.Valor)};</p>");
                        break;

                    // ── BC3: Imóvel Urbano ──
                    case TipoIntegralizacao.ImovelUrbano:
                        sb.AppendLine($"<p>{romano} — Para fins de realização de capital social, o sócio <strong>{socio.Nome}</strong> integraliza, neste ato, o imóvel urbano objeto da matrícula nº {integ.Matricula}, do {integ.Cartorio}, situado à {integ.EnderecoImovel}, com área de {integ.Area}, cadastro municipal nº {integ.CadastroMunicipal}, ao qual as partes atribuem, para esta operação societária, o valor de {FormatarMoeda(integ.Valor)}, integralmente destinado à formação do capital social subscrito neste ato.</p>");
                        break;

                    // ── BC4: Imóvel Rural ──
                    case TipoIntegralizacao.ImovelRural:
                        sb.AppendLine($"<p>{romano} — Para fins de realização de capital social, o sócio <strong>{socio.Nome}</strong> integraliza, neste ato, o imóvel rural descrito na matrícula nº {integ.Matricula}, do {integ.Cartorio}, denominado {integ.Denominacao}, com área de {integ.Area}, localizado no município de {integ.MunicipioUf}, CCIR nº {integ.Ccir}, NIRF/CAFIR nº {integ.NirfCafir}, ao qual as partes atribuem, para esta operação societária, o valor de {FormatarMoeda(integ.Valor)}, integralmente destinado à formação do capital social subscrito neste ato.</p>");
                        break;

                    // ── BC5: Participações ──
                    case TipoIntegralizacao.Participacoes:
                        sb.AppendLine($"<p>{romano} — O sócio <strong>{socio.Nome}</strong> integraliza, para fins de realização de capital social, {integ.QtdQuotasAcoes} {integ.TipoParticipacao} de emissão da sociedade {integ.NomeInvestida}, inscrita no CNPJ sob nº {integ.CnpjInvestida}, correspondentes a {integ.PercentualInvestida:F2}% do capital social, às quais as partes atribuem, para esta operação, o valor de {FormatarMoeda(integ.Valor)}, integralmente destinado à formação do capital social subscrito neste ato.</p>");
                        break;

                    default:
                        sb.AppendLine($"<p>{romano} — por <strong>{socio.Nome}</strong>, mediante {integ.Descricao ?? integ.Tipo.ToString().ToLower()}, no valor de {FormatarMoeda(integ.Valor)};</p>");
                        break;
                }
                itemNum++;
            }
        }

        // Parágrafos adicionais para imóvel
        if (diag.TemImovelQualquer)
        {
            sb.AppendLine("<p><strong>Parágrafo segundo.</strong> O valor atribuído ao bem ora conferido corresponde, nesta operação, ao montante efetivamente destinado à integralização do capital social, não havendo, neste ato, destinação de parcela do valor do bem a conta patrimonial diversa do capital social.</p>");
            sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> O sócio conferente responde pela titularidade, legitimidade, disponibilidade e regularidade do bem aportado, na forma da lei.</p>");
            sb.AppendLine("<p><strong>Parágrafo quarto.</strong> Na hipótese de integralização com bens imóveis, os sócios declaram ciência de que a estrutura do aporte e a atividade efetivamente exercida pela sociedade podem repercutir na análise tributária da operação, especialmente para fins de ITBI.</p>");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULAS 5-13 (fixas com adaptação unipessoal)
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula5_Responsabilidade(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 5 — RESPONSABILIDADE DOS SÓCIOS</h2>");
        if (diag.TipoSociedade == TipoSociedadeLtda.Unipessoal)
            sb.AppendLine("<p>A responsabilidade do sócio único é restrita ao valor de suas quotas.</p>");
        else
            sb.AppendLine("<p>A responsabilidade de cada sócio é restrita ao valor de suas quotas, mas todos respondem solidariamente pela integralização do capital social.</p>");
    }

    private void MontarClausula6_Administracao(StringBuilder sb, DiagnosticoCofre diag)
    {
        var admin = diag.Socios[diag.AdministradorIndex];
        var qualSocio = diag.AdminEhSocio ? "sócio(a)" : "não sócio(a)";

        sb.AppendLine("<h2>CLÁUSULA 6 — ADMINISTRAÇÃO</h2>");
        sb.AppendLine($"<p>A administração da sociedade caberá a <strong>{admin.Nome}</strong>, {admin.Nacionalidade}, {FormatarEstadoCivil(admin.EstadoCivil)}, {admin.Profissao}, {qualSocio}, que exercerá o cargo por prazo {diag.AdminPrazo}, dispensado(a) de caução, investido(a) com os poderes necessários à prática dos atos de gestão ordinária da sociedade, observadas as limitações deste contrato.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> Compete ao administrador representar a sociedade ativa e passivamente, judicial e extrajudicialmente, praticando todos os atos necessários à consecução de seu objeto social.</p>");

        if (diag.TipoSociedade == TipoSociedadeLtda.Pluripessoal)
        {
            sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> Dependem de deliberação prévia dos sócios, representando no mínimo {diag.QuorumDeliberacaoEspecial}% do capital social, os seguintes atos:</p>");
            sb.AppendLine("<p>a) alteração do objeto social;</p>");
            sb.AppendLine("<p>b) ingresso em atividade operacional própria;</p>");
            sb.AppendLine("<p>c) prestação de garantias a obrigações de terceiros;</p>");
            sb.AppendLine("<p>d) alienação, oneração ou disposição de ativos relevantes;</p>");
            sb.AppendLine("<p>e) assunção de dívidas fora da lógica patrimonial-societária da célula Cofre;</p>");
            sb.AppendLine("<p>f) celebração de negócios que contrariem a finalidade de preservação patrimonial prevista neste contrato.</p>");
        }

        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> O administrador declara, sob as penas da lei, que não está impedido de exercer a administração da sociedade.</p>");
    }

    private void MontarClausula7_Deliberacoes(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 7 — DELIBERAÇÕES SOCIAIS</h2>");

        if (diag.TipoSociedade == TipoSociedadeLtda.Unipessoal)
        {
            sb.AppendLine("<p>As deliberações sociais serão tomadas pelo sócio único, na forma da lei e deste contrato, mediante documento escrito.</p>");
        }
        else
        {
            sb.AppendLine("<p>As deliberações sociais serão tomadas pelos sócios, na forma da lei e deste contrato, em reunião, assembleia ou documento assinado por todos os sócios, conforme o caso.</p>");
            sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> As matérias legalmente sujeitas a quórum específico observarão o quórum previsto em lei.</p>");
            sb.AppendLine($"<p><strong>Parágrafo segundo.</strong> Sem prejuízo das matérias reservadas por lei, dependerão de aprovação dos sócios representando, no mínimo, {diag.QuorumAlteracaoContratual}% do capital social:</p>");
            sb.AppendLine("<p>I — alteração contratual;</p>");
            sb.AppendLine("<p>II — mudança da finalidade da célula Cofre;</p>");
            sb.AppendLine("<p>III — aprovação de ingresso de terceiros no quadro societário;</p>");
            sb.AppendLine("<p>IV — cisão, fusão, incorporação, dissolução ou transformação;</p>");
            sb.AppendLine("<p>V — qualquer medida que converta a sociedade em veículo operacional.</p>");
        }
    }

    private void MontarClausula8_Exercicio(StringBuilder sb, DiagnosticoCofre diag)
    {
        var suj = diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "do sócio único" : "dos sócios";
        sb.AppendLine("<h2>CLÁUSULA 8 — EXERCÍCIO SOCIAL, DEMONSTRAÇÕES E RESULTADOS</h2>");
        sb.AppendLine("<p>O exercício social encerrar-se-á em 31 de dezembro de cada ano, quando serão levantadas as demonstrações contábeis na forma da legislação aplicável.</p>");
        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> Os lucros, dividendos, resultados de participações societárias, equivalência patrimonial, reservas e demais mutações patrimoniais serão destinados na forma da deliberação {suj}, observada a legislação aplicável e a finalidade patrimonial-societária da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> A sociedade poderá levantar balanços intermediários e deliberar sobre distribuição de resultados, desde que juridicamente possível e contabilmente suportado.</p>");
        sb.AppendLine($"<p><strong>Parágrafo terceiro.</strong> O pró-labore do administrador, se houver, será fixado por deliberação {suj}.</p>");
    }

    private void MontarClausula9_Cessao(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 9 — CESSÃO E CIRCULAÇÃO DE QUOTAS</h2>");
        sb.AppendLine("<p>A cessão ou transferência de quotas, a qualquer título, entre vivos, dependerá da observância das disposições deste contrato.</p>");
        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> A cessão de quotas a terceiros estranhos ao quadro societário dependerá de aprovação {(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "do sócio único" : $"dos sócios representando, no mínimo, {diag.QuorumCessaoTerceiros}% do capital social")}, observado o direito de preferência dos demais sócios, na proporção de suas participações, em igualdade de condições.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> O sócio que pretender ceder suas quotas deverá comunicar os demais sócios por escrito, informando quantidade, preço, condições e identidade do potencial adquirente, quando houver.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> O eventual adquirente deverá aderir, previamente, às regras deste contrato e, se existente, ao acordo de quotistas aplicável.</p>");
    }

    private void MontarClausula10_Remisso(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 10 — SÓCIO REMISSO, RETIRADA, EXCLUSÃO E APURAÇÃO DE HAVERES</h2>");
        sb.AppendLine("<p>O sócio que deixar de integralizar, no prazo ajustado, a quota subscrita ficará constituído em mora, respondendo na forma da lei e deste contrato.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> O sócio poderá retirar-se da sociedade nos casos previstos em lei e neste contrato, observadas as formalidades cabíveis.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Poderá ser excluído o sócio que praticar atos de inegável gravidade que ponham em risco a continuidade da empresa, desde que haja previsão contratual e observância do procedimento legal aplicável.</p>");
        sb.AppendLine($"<p><strong>Parágrafo terceiro.</strong> Na apuração de haveres, salvo deliberação diversa ou previsão específica em acordo de quotistas, será adotado o critério de balanço de determinação, levantado na data da resolução da sociedade em relação ao sócio, com pagamento em {diag.HaveresParcelas} parcelas mensais, corrigidas na forma de {diag.HaveresIndice}, vencendo-se a primeira em {diag.HaveresPrazoDias} dias.</p>");
        sb.AppendLine("<p><strong>Parágrafo quarto.</strong> A exclusão extrajudicial por justa causa exige previsão contratual e deliberação da maioria representativa de mais da metade do capital social.</p>");
    }

    private void MontarClausula11_Falecimento(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 11 — FALECIMENTO, INCAPACIDADE, DIVÓRCIO, SEPARAÇÃO E REFLEXOS FAMILIARES</h2>");
        sb.AppendLine("<p>O falecimento ou incapacidade superveniente de sócio não acarretará, por si só, a dissolução automática da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> Na hipótese de falecimento, a sociedade poderá:</p>");
        sb.AppendLine("<p>I — prosseguir com os sócios remanescentes, com liquidação das quotas do falecido ao espólio ou aos sucessores;</p>");
        sb.AppendLine("<p>II — admitir o ingresso de sucessores, se houver concordância dos sócios remanescentes, nos termos deste contrato e de eventual acordo de quotistas;</p>");
        sb.AppendLine("<p>III — adotar solução diversa expressamente deliberada pelos sócios.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Na hipótese de separação, divórcio ou dissolução de união estável de qualquer sócio, eventual repercussão patrimonial sobre quotas, frutos, haveres ou direitos correlatos será tratada conforme o regime de bens aplicável, os documentos familiares existentes e a legislação incidente, sem ingresso automático do ex-cônjuge ou ex-companheiro no quadro societário.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> Quando exigível, as outorgas, anuências ou adesões pertinentes serão formalizadas em instrumento próprio.</p>");
    }

    private void MontarClausula12_Acordo(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 12 — ACORDO DE QUOTISTAS E GOVERNANÇA COMPLEMENTAR</h2>");
        sb.AppendLine($"<p>O{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único poderá" : "s sócios poderão")} celebrar acordo de quotistas, protocolo familiar ou instrumento complementar de governança, disciplinando matérias como sucessão, voto, restrições adicionais à circulação de quotas, critérios de liquidez, governança familiar, confidencialidade e mecanismos de resolução de impasses.</p>");
        sb.AppendLine($"<p><strong>Parágrafo único.</strong> Havendo acordo de quotistas em vigor, o{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio obriga-se" : "s sócios obrigam-se")} a observá-lo naquilo que não contrariar a lei nem este contrato, devendo eventual futuro adquirente aderir formalmente ao instrumento correspondente.</p>");
    }

    private void MontarClausula13_Dissolucao(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 13 — DISSOLUÇÃO E LIQUIDAÇÃO</h2>");
        sb.AppendLine($"<p>A sociedade dissolver-se-á nas hipóteses previstas em lei ou por deliberação {(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "do sócio único" : "dos sócios")}.</p>");
        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> Dissolvida a sociedade, proceder-se-á à sua liquidação na forma da lei ou na forma deliberada pelo{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único" : "s sócios")}.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Enquanto não ultimada a liquidação, será preservada, tanto quanto possível, a lógica de proteção e organização patrimonial que orientou a constituição da célula Cofre.</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // CLÁUSULA 14 — DECLARAÇÃO FINAL + BC7 (coerência)
    // ═══════════════════════════════════════════════════════════

    private void MontarClausula14_Declaracao(StringBuilder sb, DiagnosticoCofre diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 14 — DECLARAÇÃO FINAL DE FINALIDADE E COERÊNCIA ESTRUTURAL</h2>");

        if (diag.DeveTravaCofre)
        {
            sb.AppendLine($"<p>O{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único declara" : "s sócios declaram")}, para todos os fins, que a presente sociedade foi concebida como célula patrimonial-societária de preservação, não destinada ao exercício de atividade operacional própria, mas sim à concentração, organização e proteção do núcleo patrimonial familiar, com coerência entre seu desenho contratual, sua conduta efetiva e sua função dentro da arquitetura patrimonial adotada pela família.</p>");
        }

        // ── BC7: Coerência da integralização (quando há imóvel) ──
        if (diag.DeveCoerenciaIntegralizacao)
        {
            sb.AppendLine($"<p>O{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? " sócio único declara" : "s sócios declaram")} que a presente sociedade foi concebida como holding não financeira de função patrimonial-societária, sem atividade operacional própria, e que a conferência do bem ora aportado se destina à realização do capital social, nos termos deste contrato, observada a coerência entre a finalidade societária, a estrutura do aporte e a atividade efetivamente exercida.</p>");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // FECHO E ASSINATURAS
    // ═══════════════════════════════════════════════════════════

    private void MontarFecho(StringBuilder sb, DiagnosticoCofre diag)
    {
        var qtdVias = diag.Socios.Count + 1; // sócios + JUCESP
        sb.AppendLine($"<p>E, por estar{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "" : "em")} assim justo{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "" : "s")} e contratado{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "" : "s")}, assina{(diag.TipoSociedade == TipoSociedadeLtda.Unipessoal ? "" : "m")} o presente instrumento em {qtdVias} ({NumeroExtenso(qtdVias)}) vias de igual teor e forma, juntamente com duas testemunhas.</p>");

        sb.AppendLine("<p>[Local], [data].</p>");

        foreach (var socio in diag.Socios)
        {
            sb.AppendLine($"<p>________________________________________<br/><strong>{socio.Nome}</strong></p>");
        }

        sb.AppendLine("<p>________________________________________<br/>Testemunha 1<br/>Nome:<br/>CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2<br/>Nome:<br/>CPF:</p>");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════

    private List<string> IdentificarBlocos(DiagnosticoCofre diag)
    {
        var blocos = new List<string>();
        if (diag.TipoSociedade == TipoSociedadeLtda.Unipessoal) blocos.Add("BC1-Unipessoal");
        if (diag.Socios.Any(s => s.EstadoCivil == EstadoCivil.Casado || s.UniaoEstavel)) blocos.Add("BC2-RegimeBens");
        if (diag.TemImóvelUrbano) blocos.Add("BC3-ImovelUrbano");
        if (diag.TemImovelRural) blocos.Add("BC4-ImovelRural");
        if (diag.TemParticipacoes) blocos.Add("BC5-Participacoes");
        if (diag.DeveTravaCofre) blocos.Add("BC6-TravaCofre");
        if (diag.DeveCoerenciaIntegralizacao) blocos.Add("BC7-CoerenciaIntegralizacao");
        return blocos;
    }

    private static string FormatarEstadoCivil(EstadoCivil ec) => ec switch
    {
        EstadoCivil.Solteiro => "solteiro(a)",
        EstadoCivil.Casado => "casado(a)",
        EstadoCivil.Divorciado => "divorciado(a)",
        EstadoCivil.Viuvo => "viúvo(a)",
        EstadoCivil.UniaoEstavel => "convivente em união estável",
        EstadoCivil.SeparadoJudicialmente => "separado(a) judicialmente",
        _ => ec.ToString().ToLower()
    };

    private static string FormatarRegimeBens(RegimeBens rb) => rb switch
    {
        RegimeBens.ComunhaoTotal => "comunhão universal de bens",
        RegimeBens.ComunhaoParcial => "comunhão parcial de bens",
        RegimeBens.SeparacaoTotal => "separação total de bens",
        RegimeBens.SeparacaoObrigatoria => "separação obrigatória de bens",
        RegimeBens.ParticipacaoFinalAquestos => "participação final nos aquestos",
        _ => rb.ToString().ToLower()
    };

    private static string FormatarMoeda(decimal valor)
        => valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));

    // Placeholder — em produção, usar biblioteca de extenso (Humanizer.pt-BR ou custom)
    private static string ValorExtenso(decimal valor) => $"{valor:N2} por extenso";
    private static string NumeroExtenso(int numero) => $"{numero} por extenso";

    private static string ToRomano(int n) => n switch
    {
        1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
        6 => "VI", 7 => "VII", 8 => "VIII", 9 => "IX", 10 => "X",
        _ => n.ToString()
    };
}

// ═══════════════════════════════════════════════════════════════
// RESULTADO DA MONTAGEM
// ═══════════════════════════════════════════════════════════════

public class ResultadoMontagem
{
    public bool Sucesso { get; set; }
    public string? ConteudoHtml { get; set; }
    public ResultadoValidacao Validacao { get; set; } = new();
    public List<string> BlocosCondicionaisAtivados { get; set; } = [];
}
