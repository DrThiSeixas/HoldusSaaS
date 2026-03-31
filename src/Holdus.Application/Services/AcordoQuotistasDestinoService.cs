#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// MINUTA 7.0 — Acordo de Quotistas da Célula Destino
// "O acordo não é acessório. Ele regula o sistema."
//
// 14 cláusulas + 4 módulos parametrizáveis + 5 travas
// Código: DESTINO.ACORDO_QUOTISTAS.V1
// ═══════════════════════════════════════════════════════════════

// ── ENUMS ────────────────────────────────────────────────────

public enum RegimeVotoUsufruto
{
    SemUsufruto = 0,           // MOD-A: quotista vota direto
    UsufrutuarioVota = 1,      // MOD-B: usufrutuário com voto
    NuProprietarioVota = 2,    // MOD-C: nu-proprietário com voto
    RegimeMisto = 3            // MOD-D: ordinário/extraordinário separado
}

public enum DireitosEconomicosAcordo
{
    UsufrutuarioIntegral = 0,  // Frutos 100% usufrutuário
    NuProprietarioIntegral = 1,// Frutos 100% nu-proprietário
    Repartido = 2              // Fórmula personalizada
}

public enum QuorumGovernanca
{
    Unanimidade = 0,
    MaioriaQualificada75 = 1,
    DoisTercos = 2,
    Personalizado = 3
}

public enum SolucaoControversias
{
    Foro = 0,
    Arbitragem = 1,
    MediacaoPrevia = 2
}

public enum PrazoAcordo
{
    Indeterminado = 0,
    Determinado = 1
}

// ── DIAGNÓSTICO ──────────────────────────────────────────────

public class DiagnosticoAcordoDestino
{
    // Sociedade
    public string NomeEmpresarial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Comarca { get; set; } = string.Empty;
    public string UfJunta { get; set; } = string.Empty;

    // Partes
    public List<ParteAcordo> Signatarios { get; set; } = [];

    // Módulos parametrizáveis
    public bool TemUsufruto { get; set; }
    public RegimeVotoUsufruto RegimeVoto { get; set; } = RegimeVotoUsufruto.SemUsufruto;
    public DireitosEconomicosAcordo DireitosEconomicos { get; set; } = DireitosEconomicosAcordo.UsufrutuarioIntegral;
    public string? ReparticaoPersonalizada { get; set; }  // Se DireitosEconomicos == Repartido

    // Regime misto (MOD-D)
    public string? VotoOrdinarioExercidoPor { get; set; }   // "usufrutuário" ou "nu-proprietário"
    public string? VotoExtraordinarioExercidoPor { get; set; }

    // Governança
    public QuorumGovernanca Quorum { get; set; } = QuorumGovernanca.Unanimidade;
    public string? AdministradorNome { get; set; }

    // Saída
    public int PrazoNotificacaoDias { get; set; } = 90;
    public string MetodoAvaliacao { get; set; } = "balanço de determinação";
    public int ParcelasSaida { get; set; } = 12;

    // Multa
    public decimal? ValorMulta { get; set; }

    // Prazo
    public PrazoAcordo Prazo { get; set; } = PrazoAcordo.Indeterminado;
    public int? PrazoDeterminadoAnos { get; set; }

    // Controvérsias
    public SolucaoControversias Controversias { get; set; } = SolucaoControversias.Foro;
    public string? CamaraArbitral { get; set; }

    // Confirmações
    public bool ContratoSocialVinculado { get; set; }
}

public class ParteAcordo
{
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public string Qualidade { get; set; } = "quotista"; // quotista, usufrutuário, nu-proprietário, aderente
    public string QualificacaoCompleta { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════════════════
// SERVIÇO
// ═══════════════════════════════════════════════════════════════

public class AcordoQuotistasDestinoService
{
    public ResultadoMontagem Montar(DiagnosticoAcordoDestino diag)
    {
        var val = Validar(diag);
        if (!val.CanGenerateDocuments)
            return new ResultadoMontagem { Sucesso = false, Validacao = ToRV(val) };

        var sb = new StringBuilder();

        MontarTitulo(sb, diag);
        MontarPreambulo(sb, diag);
        Cl01_Objeto(sb, diag);
        Cl02_Vinculacao(sb, diag);
        Cl03_DireitosPoliticos(sb, diag);
        Cl04_DireitosEconomicos(sb, diag);
        Cl05_Administracao(sb, diag);
        Cl06_Circulacao(sb, diag);
        Cl07_EventosFamiliares(sb, diag);
        Cl08_BloqueioFamiliar(sb, diag);
        Cl09_Saida(sb, diag);
        Cl10_Confidencialidade(sb, diag);
        Cl11_Multa(sb, diag);
        Cl12_Arquivamento(sb, diag);
        Cl13_Prazo(sb, diag);
        Cl14_Controversias(sb, diag);
        MontarFecho(sb, diag);

        var blocos = new List<string>();
        blocos.Add($"MOD-A-{diag.RegimeVoto}");
        if (diag.TemUsufruto) blocos.Add($"MOD-B-{diag.DireitosEconomicos}");
        blocos.Add($"MOD-C-{diag.Quorum}");
        blocos.Add($"MOD-D-{diag.Controversias}");

        return new ResultadoMontagem
        {
            Sucesso = true,
            ConteudoHtml = sb.ToString(),
            Validacao = ToRV(val),
            BlocosCondicionaisAtivados = blocos,
        };
    }

    // ═══════════════════════════════════════════════════════════
    // VALIDAÇÃO — 5 travas
    // ═══════════════════════════════════════════════════════════

    private ValidationResult Validar(DiagnosticoAcordoDestino diag)
    {
        var r = new ValidationResult();

        if (!diag.Signatarios.Any())
            r.AddError("AQ_001", "Deve haver ao menos dois signatários.");
        if (string.IsNullOrWhiteSpace(diag.Cnpj))
            r.AddError("AQ_002", "O CNPJ da sociedade é obrigatório.");

        foreach (var s in diag.Signatarios.Where(s => string.IsNullOrWhiteSpace(s.Cpf)))
            r.AddError("AQ_003", $"O signatário {s.Nome} não possui CPF.");

        // T1: Sem contrato social vinculado
        if (!diag.ContratoSocialVinculado)
            r.AddError("AQ_T1", "TRAVA: O acordo de quotistas exige contrato social da Destino vinculado.");

        // T2: Usufruto sem escolha de regime de voto
        if (diag.TemUsufruto && diag.RegimeVoto == RegimeVotoUsufruto.SemUsufruto)
            r.AddError("AQ_T2", "TRAVA: Há usufruto ativo, mas o regime de voto não foi definido. O DREI exige disciplina clara.");

        // T3: Usufruto sem escolha de direitos econômicos
        if (diag.TemUsufruto && diag.DireitosEconomicos == DireitosEconomicosAcordo.Repartido
            && string.IsNullOrWhiteSpace(diag.ReparticaoPersonalizada))
            r.AddError("AQ_T3", "TRAVA: Regime econômico repartido exige definição da fórmula de repartição.");

        // T4: Sem multa
        if (!diag.ValorMulta.HasValue || diag.ValorMulta <= 0)
            r.AddWarning("AQ_T4", "O valor da multa por descumprimento não foi definido.");

        // T5: Sem solução de controvérsias quando arbitragem sem câmara
        if (diag.Controversias == SolucaoControversias.Arbitragem
            && string.IsNullOrWhiteSpace(diag.CamaraArbitral))
            r.AddError("AQ_T5", "TRAVA: Arbitragem selecionada sem indicação da câmara arbitral.");

        // Regime misto sem definição
        if (diag.RegimeVoto == RegimeVotoUsufruto.RegimeMisto)
        {
            if (string.IsNullOrWhiteSpace(diag.VotoOrdinarioExercidoPor))
                r.AddError("AQ_004", "Regime misto: definir quem vota em matérias ordinárias.");
            if (string.IsNullOrWhiteSpace(diag.VotoExtraordinarioExercidoPor))
                r.AddError("AQ_005", "Regime misto: definir quem vota em matérias extraordinárias.");
        }

        return r;
    }

    // ═══════════════════════════════════════════════════════════
    // MONTAGEM — 14 CLÁUSULAS
    // ═══════════════════════════════════════════════════════════

    private void MontarTitulo(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine($"<h1>ACORDO DE QUOTISTAS</h1>");
        sb.AppendLine($"<h2>da sociedade {diag.NomeEmpresarial.ToUpper()} LTDA.</h2>");
    }

    private void MontarPreambulo(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<p>Pelo presente instrumento particular, as partes abaixo identificadas:</p>");
        foreach (var s in diag.Signatarios)
        {
            var qual = !string.IsNullOrWhiteSpace(s.QualificacaoCompleta)
                ? s.QualificacaoCompleta
                : $"{s.Nome}, CPF nº {s.Cpf}";
            sb.AppendLine($"<p><strong>{s.Nome.ToUpper()}</strong>, {qual};</p>");
        }
        sb.AppendLine($"<p>na qualidade de quotistas, usufrutuários, nu-proprietários ou aderentes da sociedade <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong>, inscrita no CNPJ sob nº {diag.Cnpj}, têm entre si justo e contratado o presente Acordo de Quotistas, que se regerá pelas cláusulas e condições seguintes.</p>");
    }

    private void Cl01_Objeto(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 1 — OBJETO E FINALIDADE</h2>");
        sb.AppendLine($"<p>O presente acordo disciplina o exercício de direitos políticos e econômicos, a governança societária, a circulação de quotas, os efeitos de eventos familiares relevantes e os mecanismos de preservação da finalidade patrimonial-societária e sucessória da sociedade <strong>{diag.NomeEmpresarial.ToUpper()} LTDA.</strong></p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A sociedade é reconhecida pelas partes como célula de transferência patrimonial por quotas, voltada à organização da sucessão familiar e à acomodação societária das futuras gerações.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> O presente acordo complementa o contrato social, os instrumentos de doação de quotas, os pactos de usufruto e os demais instrumentos de planejamento sucessório vinculados à sociedade.</p>");
    }

    private void Cl02_Vinculacao(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 2 — VINCULAÇÃO E ADESÃO</h2>");
        sb.AppendLine("<p>Ficam vinculados a este acordo todos os quotistas atuais e futuros, bem como os beneficiários que ingressarem na sociedade por doação, sucessão, cessão ou outro título admitido.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> O ingresso de novo quotista dependerá de adesão expressa a este acordo.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Nenhuma transmissão de quotas, por ato entre vivos ou causa mortis, produzirá plena eficácia interna sem a correspondente submissão do ingressante às regras deste instrumento.</p>");
    }

    private void Cl03_DireitosPoliticos(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 3 — EXERCÍCIO DOS DIREITOS POLÍTICOS</h2>");
        sb.AppendLine("<p>O exercício do direito de voto observará o contrato social e este acordo.</p>");

        // ── MOD-A: Regime de voto ────────────────────────
        switch (diag.RegimeVoto)
        {
            case RegimeVotoUsufruto.SemUsufruto:
                sb.AppendLine("<p>Cada quotista exercerá diretamente os direitos políticos inerentes às quotas de sua titularidade.</p>");
                break;

            case RegimeVotoUsufruto.UsufrutuarioVota:
                sb.AppendLine("<p>Enquanto perdurar o usufruto, os direitos políticos relativos às quotas gravadas serão exercidos pelo usufrutuário.</p>");
                break;

            case RegimeVotoUsufruto.NuProprietarioVota:
                sb.AppendLine("<p>Enquanto perdurar o usufruto, os direitos políticos relativos às quotas gravadas serão exercidos pelo nu-proprietário.</p>");
                break;

            case RegimeVotoUsufruto.RegimeMisto:
                sb.AppendLine($"<p>As matérias ordinárias serão votadas pelo {diag.VotoOrdinarioExercidoPor} e as matérias extraordinárias dependerão da anuência conjunta de usufrutuário e nu-proprietário.</p>");
                break;
        }

        sb.AppendLine("<p><strong>Parágrafo único.</strong> Na falta de definição expressa em cada caso concreto, o sistema deve tratar a matéria como pendente, porque essa disciplina precisa ser clara quando o usufruto estiver regulado em instrumento parassocial.</p>");
    }

    private void Cl04_DireitosEconomicos(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 4 — DIREITOS ECONÔMICOS</h2>");
        sb.AppendLine("<p>Os frutos, lucros, dividendos e demais resultados econômicos observarão a seguinte disciplina:</p>");

        // ── MOD-B: Direitos econômicos ───────────────────
        switch (diag.DireitosEconomicos)
        {
            case DireitosEconomicosAcordo.UsufrutuarioIntegral:
                sb.AppendLine("<p>Pertencem integralmente ao usufrutuário enquanto perdurar o usufruto.</p>");
                break;
            case DireitosEconomicosAcordo.NuProprietarioIntegral:
                sb.AppendLine("<p>Pertencem integralmente ao nu-proprietário.</p>");
                break;
            case DireitosEconomicosAcordo.Repartido:
                sb.AppendLine($"<p>Serão repartidos na seguinte forma: {diag.ReparticaoPersonalizada}.</p>");
                break;
        }

        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> A distribuição de resultados, quando deliberada, observará a lei, o contrato social e este acordo.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Este acordo não autoriza exclusão de quotista da repartição de resultados em desconformidade com a disciplina legal aplicável à limitada.</p>");
    }

    private void Cl05_Administracao(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 5 — ADMINISTRAÇÃO E CONTROLE</h2>");

        if (!string.IsNullOrWhiteSpace(diag.AdministradorNome))
            sb.AppendLine($"<p>A administração da sociedade caberá a <strong>{diag.AdministradorNome}</strong>, na forma do contrato social.</p>");
        else
            sb.AppendLine("<p>A administração da sociedade observará o contrato social.</p>");

        // ── MOD-C: Quórum ────────────────────────────────
        var quorumTexto = diag.Quorum switch
        {
            QuorumGovernanca.Unanimidade => "unanimidade",
            QuorumGovernanca.MaioriaQualificada75 => "75% dos signatários",
            QuorumGovernanca.DoisTercos => "2/3 dos signatários",
            _ => "maioria definida caso a caso"
        };

        sb.AppendLine($"<p><strong>Parágrafo primeiro.</strong> Dependem de aprovação por {quorumTexto}:</p>");
        sb.AppendLine("<p>I — alteração da finalidade da sociedade;</p>");
        sb.AppendLine("<p>II — admissão de terceiro estranho ao núcleo familiar;</p>");
        sb.AppendLine("<p>III — cessão ou oneração de quotas fora das hipóteses previstas neste acordo;</p>");
        sb.AppendLine("<p>IV — alteração da lógica sucessória da célula Destino;</p>");
        sb.AppendLine("<p>V — celebração de ato que comprometa a estabilidade patrimonial-societária da sociedade.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> Os donatários ingressantes por força de planejamento sucessório sujeitam-se às limitações de governança aqui previstas, ainda que passem a figurar formalmente como quotistas.</p>");
    }

    private void Cl06_Circulacao(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 6 — RESTRIÇÕES À CIRCULAÇÃO DE QUOTAS</h2>");
        sb.AppendLine("<p>É vedada a cessão, alienação, promessa de cessão, oneração, dação em garantia ou qualquer forma de disposição das quotas, salvo nas hipóteses expressamente permitidas neste acordo e no contrato social.</p>");
        sb.AppendLine("<p><strong>Parágrafo primeiro.</strong> Em qualquer hipótese de saída voluntária, os demais signatários ou a própria sociedade terão preferência para aquisição das quotas, em igualdade de condições.</p>");
        sb.AppendLine("<p><strong>Parágrafo segundo.</strong> É vedada a entrada automática de cônjuge, companheiro, ex-cônjuge, ex-companheiro ou terceiro estranho ao grupo familiar como quotista, ainda que haja reflexo patrimonial externo sobre as quotas.</p>");
        sb.AppendLine("<p><strong>Parágrafo terceiro.</strong> A disciplina contratual de circulação deve ser lida em conjunto com o regime legal da limitada, especialmente naquilo que o Código Civil reserva ao contrato social e à oposição de sócios na entrada de terceiros.</p>");
    }

    private void Cl07_EventosFamiliares(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 7 — EVENTOS FAMILIARES RELEVANTES</h2>");
        sb.AppendLine("<p>Ocorrendo falecimento, incapacidade, divórcio, dissolução de união estável, insolvência, penhora ou outro evento apto a comprometer a estabilidade da estrutura societária, aplicar-se-ão as seguintes regras:</p>");
        sb.AppendLine("<p>I — o falecimento de quotista ou usufrutuário não autoriza, por si só, ingresso automático de sucessor sem observância do contrato social e deste acordo;</p>");
        sb.AppendLine("<p>II — em caso de incapacidade, os direitos serão exercidos na forma da representação ou assistência legal, sem prejuízo das travas deste acordo;</p>");
        sb.AppendLine("<p>III — em caso de divórcio ou dissolução de união estável, eventual reflexo patrimonial não importa ingresso automático do ex-cônjuge ou ex-companheiro no quadro societário;</p>");
        sb.AppendLine("<p>IV — em caso de constrição judicial, os signatários deverão adotar as medidas contratuais e processuais cabíveis para preservação da coerência da estrutura.</p>");
    }

    private void Cl08_BloqueioFamiliar(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 8 — CLÁUSULA DE BLOQUEIO FAMILIAR</h2>");
        sb.AppendLine("<p>Os signatários reconhecem que a sociedade não se destina à livre circulação de quotas no mercado nem à fragmentação patrimonial desordenada.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Qualquer conduta que tenha por efeito desvirtuar a Destino como célula de transferência patrimonial e governança sucessória será considerada violação grave deste acordo.</p>");
    }

    private void Cl09_Saida(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 9 — SAÍDA, EXCLUSÃO NEGOCIAL E APURAÇÃO</h2>");
        sb.AppendLine("<p>Sem prejuízo da disciplina legal e contratual da sociedade, o signatário que desejar sair da estrutura deverá observar:</p>");
        sb.AppendLine($"<p>I — notificação prévia de {diag.PrazoNotificacaoDias} ({ExtN(diag.PrazoNotificacaoDias)}) dias;</p>");
        sb.AppendLine("<p>II — oferta prioritária das quotas aos demais vinculados;</p>");
        sb.AppendLine($"<p>III — critério de avaliação definido em {diag.MetodoAvaliacao};</p>");
        sb.AppendLine($"<p>IV — pagamento em {diag.ParcelasSaida} ({ExtN(diag.ParcelasSaida)}) parcelas, na forma ajustada.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> O descumprimento grave deste acordo poderá autorizar, além de perdas e danos e multa, a adoção das medidas societárias cabíveis conforme contrato social e legislação aplicável.</p>");
    }

    private void Cl10_Confidencialidade(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 10 — CONFIDENCIALIDADE E DEVER DE COOPERAÇÃO</h2>");
        sb.AppendLine("<p>Os signatários obrigam-se a manter sigilo sobre os termos deste acordo, os instrumentos sucessórios correlatos e as informações patrimoniais e familiares a que tenham acesso, ressalvadas as hipóteses legais, regulatórias ou judiciais de divulgação.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Os signatários comprometem-se a cooperar para a prática dos atos necessários à manutenção da coerência entre contrato social, doação, usufruto, cadastro societário e registro dos atos pertinentes.</p>");
    }

    private void Cl11_Multa(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 11 — MULTA E CONSEQUÊNCIAS DO DESCUMPRIMENTO</h2>");

        var multa = diag.ValorMulta.HasValue && diag.ValorMulta > 0
            ? Fmt(diag.ValorMulta.Value)
            : "R$ [●]";

        sb.AppendLine($"<p>O descumprimento de obrigação assumida neste acordo sujeitará o infrator à multa não compensatória de {multa}, sem prejuízo de:</p>");
        sb.AppendLine("<p>I — obrigação de fazer ou não fazer;</p>");
        sb.AppendLine("<p>II — perdas e danos;</p>");
        sb.AppendLine("<p>III — tutela específica;</p>");
        sb.AppendLine("<p>IV — medidas societárias e registrais cabíveis.</p>");
    }

    private void Cl12_Arquivamento(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 12 — ARQUIVAMENTO E EFICÁCIA PERANTE TERCEIROS</h2>");
        sb.AppendLine("<p>Os signatários autorizam o arquivamento deste acordo, de seu extrato ou de ato que dê ciência de sua existência perante a Junta Comercial, para produção de efeitos perante terceiros, na forma admitida pelo DREI.</p>");
        sb.AppendLine("<p><strong>Parágrafo único.</strong> Quando este acordo regular usufruto de quotas ou disciplina correlata, seu arquivamento será observado para eficácia perante terceiros.</p>");
    }

    private void Cl13_Prazo(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 13 — PRAZO E VIGÊNCIA</h2>");

        if (diag.Prazo == PrazoAcordo.Determinado && diag.PrazoDeterminadoAnos.HasValue)
            sb.AppendLine($"<p>O presente acordo entra em vigor na data de sua assinatura e permanecerá vigente pelo prazo de {diag.PrazoDeterminadoAnos} ({ExtN(diag.PrazoDeterminadoAnos.Value)}) anos, obrigando as partes, seus sucessores e aderentes, na forma aqui prevista.</p>");
        else
            sb.AppendLine("<p>O presente acordo entra em vigor na data de sua assinatura e permanecerá vigente por prazo indeterminado, obrigando as partes, seus sucessores e aderentes, na forma aqui prevista.</p>");
    }

    private void Cl14_Controversias(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<h2>CLÁUSULA 14 — SOLUÇÃO DE CONTROVÉRSIAS</h2>");

        // ── MOD-D ────────────────────────────────────────
        switch (diag.Controversias)
        {
            case SolucaoControversias.Foro:
                sb.AppendLine($"<p>As controvérsias oriundas deste acordo serão dirimidas no foro da Comarca de {diag.Comarca}, Estado de {diag.UfJunta}.</p>");
                break;

            case SolucaoControversias.Arbitragem:
                sb.AppendLine($"<p>As controvérsias oriundas deste acordo serão resolvidas por arbitragem, administrada pela {diag.CamaraArbitral}, de acordo com suas regras vigentes.</p>");
                break;

            case SolucaoControversias.MediacaoPrevia:
                sb.AppendLine($"<p>As controvérsias oriundas deste acordo serão submetidas, previamente, a mediação, no prazo de 30 (trinta) dias, e, persistindo o impasse, serão dirimidas no foro da Comarca de {diag.Comarca}, Estado de {diag.UfJunta}.</p>");
                break;
        }
    }

    private void MontarFecho(StringBuilder sb, DiagnosticoAcordoDestino diag)
    {
        sb.AppendLine("<p>[Local], [data].</p>");
        foreach (var s in diag.Signatarios)
            sb.AppendLine($"<p>________________________________________<br/><strong>{s.Nome}</strong><br/><em>{s.Qualidade}</em></p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 1 — Nome: / CPF:</p>");
        sb.AppendLine("<p>________________________________________<br/>Testemunha 2 — Nome: / CPF:</p>");
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
    private static string ExtN(int n) => $"{n} por extenso";
}
