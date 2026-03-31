#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// WORKFLOW 3 CÉLULAS — Modelo de Domínio
// Versão definitiva — Thiago Seixas
//
// "O PDF vira a espinha do workflow. As minutas viram a biblioteca.
//  As validações viram o freio. O sistema só avança quando a etapa
//  estiver juridicamente fechada."
// ═══════════════════════════════════════════════════════════════

public enum WorkflowPhase
{
    DestinoSucessorio = 1,
    CofrePatrimonial = 2,
    VeiculoTributarioControle = 3
}

public enum WorkflowStepStatus
{
    NaoIniciado = 0,
    EmCadastro = 1,
    EmValidacao = 2,
    PendenteDocumento = 3,
    PendenteTributo = 4,
    Bloqueado = 5,
    AptoParaGeracao = 6,
    Gerado = 7,
    Concluido = 8
}

public enum WorkflowStepCode
{
    // Fase 1 — Destino
    DestinoConstituicao = 100,
    DestinoContaBancaria = 101,
    DestinoIntegralizacaoCapital = 102,
    DestinoDoacaoQuotas = 103,
    DestinoAcordoQuotistas = 104,
    DestinoItcmd = 105,

    // Fase 2 — Cofre
    CofreConstituicao = 200,
    CofreApuracaoItbi = 201,
    CofrePreenchimento = 202,
    CofreVertenteItbiNaoCobraDiferenca = 203,
    CofreVertenteItbiCobraDiferenca = 204,

    // Fase 3 — Veículo
    VeiculoConstituicao = 300,
    VeiculoQuotasOrdinarias = 301,
    VeiculoQuotaPreferencial = 302,
    VeiculoVotoPreferencial = 303,
    VeiculoIntegralizacao = 304,
    VeiculoReservaCapital = 305,
    VeiculoCallPreferencial = 306,
    CofreAlteracaoTitularidadeParaVeiculo = 307,
    VeiculoVendaOrdinariasParaDestino = 308
}

// ═══════════════════════════════════════════════════════════════
// DOCUMENTO OBRIGATÓRIO DE UM PASSO
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowDocumentRequirement
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public bool Generated { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// TRAVA DE VALIDAÇÃO (GATE) DE UM PASSO
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowValidationGate
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool Blocking { get; set; } = true;
}

// ═══════════════════════════════════════════════════════════════
// DEFINIÇÃO DE UM PASSO (imutável — o que o passo É)
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowStepDefinition
{
    public WorkflowStepCode Code { get; set; }
    public WorkflowPhase Phase { get; set; }
    public int OrderInPhase { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public List<WorkflowStepCode> DependsOn { get; set; } = new();
    public List<WorkflowDocumentRequirement> RequiredDocuments { get; set; } = new();
    public List<WorkflowValidationGate> RequiredGates { get; set; } = new();
    public bool Optional { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// ESTADO DE UM PASSO (mutável — onde o passo ESTÁ)
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowStepState
{
    public WorkflowStepCode Code { get; set; }
    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.NaoIniciado;
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public string? Notes { get; set; }
    public List<string> BlockingReasons { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════
// WORKFLOW DO PROJETO (Definition + State separados)
// ═══════════════════════════════════════════════════════════════

public sealed class ProjectWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjetoId { get; set; }
    public string Name { get; set; } = "Workflow 3 Células";

    public List<WorkflowStepDefinition> Definitions { get; set; } = new();
    public List<WorkflowStepState> States { get; set; } = new();

    public WorkflowStepDefinition GetDefinition(WorkflowStepCode code) =>
        Definitions.Single(x => x.Code == code);

    public WorkflowStepState GetState(WorkflowStepCode code) =>
        States.Single(x => x.Code == code);
}

// ═══════════════════════════════════════════════════════════════
// FACTORY — Instancia o workflow canônico do PDF
// 20 passos: 6 Destino + 5 Cofre + 9 Veículo
// ═══════════════════════════════════════════════════════════════

public static class ThreeCellsWorkflowFactory
{
    public static ProjectWorkflow Create(Guid projetoId)
    {
        var workflow = new ProjectWorkflow
        {
            ProjetoId = projetoId,
            Definitions = BuildDefinitions()
        };

        foreach (var def in workflow.Definitions)
        {
            workflow.States.Add(new WorkflowStepState
            {
                Code = def.Code,
                Status = WorkflowStepStatus.NaoIniciado
            });
        }

        return workflow;
    }

    private static List<WorkflowStepDefinition> BuildDefinitions()
    {
        return new List<WorkflowStepDefinition>
        {
            // ═══════════════════════════════════════════════
            // FASE 1 — DESTINO (6 passos)
            // ═══════════════════════════════════════════════
            new()
            {
                Code = WorkflowStepCode.DestinoConstituicao,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 1,
                Title = "Constituir Destino",
                Objective = "Criar a célula sucessória com capital reduzido.",
                RequiredDocuments =
                {
                    Doc("DESTINO.CONSTITUICAO.V1", "Contrato Social Master da Destino")
                },
                RequiredGates =
                {
                    Gate("DESTINO_SOCIOS_OK", "Os sócios originários são os donos do patrimônio."),
                    Gate("DESTINO_OBJETO_OK", "Objeto social coerente com a função sucessória."),
                    Gate("DESTINO_CAPITAL_OK", "Capital inicial reduzido e validado.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.DestinoContaBancaria,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 2,
                Title = "Abrir conta bancária da Destino",
                Objective = "Dar materialidade operacional à integralização inicial.",
                DependsOn = { WorkflowStepCode.DestinoConstituicao },
                RequiredGates =
                {
                    Gate("DESTINO_CONTA_ABERTA", "Conta bancária aberta em nome da sociedade.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.DestinoIntegralizacaoCapital,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 3,
                Title = "Integralizar capital inicial da Destino",
                Objective = "Comprovar aporte real de cada sócio.",
                DependsOn = { WorkflowStepCode.DestinoContaBancaria },
                RequiredDocuments =
                {
                    Doc("DESTINO.CHECKLIST_CAPITAL_INICIAL.V1", "Checklist de integralização inicial")
                },
                RequiredGates =
                {
                    Gate("DESTINO_PIX_EXATO", "Cada sócio aportou o valor exato de sua participação.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.DestinoDoacaoQuotas,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 4,
                Title = "Doação de quotas na Destino",
                Objective = "Transferir a camada sucessória por quotas.",
                DependsOn = { WorkflowStepCode.DestinoIntegralizacaoCapital },
                RequiredDocuments =
                {
                    Doc("DESTINO.DOACAO_QUOTAS.V1", "Instrumento de doação de quotas"),
                    Doc("DESTINO.ALT_CONTRATUAL_DOACAO.V1", "Alteração contratual refletindo a doação")
                },
                RequiredGates =
                {
                    Gate("DESTINO_DOACAO_MODO_OK", "Natureza da liberalidade definida."),
                    Gate("DESTINO_USUFRUTO_OK", "Usufruto parametrizado, se houver."),
                    Gate("DESTINO_RESTRICOES_OK", "Restrições incidentes sobre as quotas definidas.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.DestinoAcordoQuotistas,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 5,
                Title = "Acordo de Quotistas da Destino",
                Objective = "Regular voto, usufruto, governança e eventos familiares.",
                DependsOn = { WorkflowStepCode.DestinoDoacaoQuotas },
                RequiredDocuments =
                {
                    Doc("DESTINO.ACQ.V1", "Acordo de Quotistas da Destino")
                },
                RequiredGates =
                {
                    Gate("DESTINO_ACQ_VOTO_OK", "Direitos políticos definidos."),
                    Gate("DESTINO_ACQ_ECONOMICO_OK", "Direitos econômicos definidos."),
                    Gate("DESTINO_ACQ_CIRCULACAO_OK", "Circulação de quotas travada."),
                    Gate("DESTINO_ACQ_EVENTOS_OK", "Eventos familiares críticos regulados.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.DestinoItcmd,
                Phase = WorkflowPhase.DestinoSucessorio,
                OrderInPhase = 6,
                Title = "ITCMD da doação na Destino",
                Objective = "Fechar o fluxo tributário sucessório.",
                DependsOn = { WorkflowStepCode.DestinoAcordoQuotistas },
                RequiredDocuments =
                {
                    Doc("DESTINO.CHECKLIST_ITCMD.V1", "Checklist de ITCMD"),
                    Doc("DESTINO.NOTA_TECNICA_ITCMD.V1", "Nota técnica interna de ITCMD")
                },
                RequiredGates =
                {
                    Gate("DESTINO_ITCMD_OK", "Processamento e pagamento do ITCMD resolvidos.")
                }
            },

            // ═══════════════════════════════════════════════
            // FASE 2 — COFRE (5 passos)
            // ═══════════════════════════════════════════════
            new()
            {
                Code = WorkflowStepCode.CofreConstituicao,
                Phase = WorkflowPhase.CofrePatrimonial,
                OrderInPhase = 1,
                Title = "Constituir Cofre",
                Objective = "Criar a célula patrimonial-societária de preservação.",
                DependsOn = { WorkflowStepCode.DestinoItcmd },
                RequiredDocuments =
                {
                    Doc("COFRE.CONSTITUICAO.V1", "Contrato Social Master da Cofre")
                },
                RequiredGates =
                {
                    Gate("COFRE_PURO_OK", "Cofre permanece sem atividade operacional própria."),
                    Gate("COFRE_OBJETO_OK", "Objeto social puro e coerente.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.CofreApuracaoItbi,
                Phase = WorkflowPhase.CofrePatrimonial,
                OrderInPhase = 2,
                Title = "Apurar cenário municipal do ITBI",
                Objective = "Definir a trilha de integralização patrimonial.",
                DependsOn = { WorkflowStepCode.CofreConstituicao },
                RequiredDocuments =
                {
                    Doc("COFRE.CHECKLIST_ITBI.V1", "Checklist de ITBI da Cofre")
                },
                RequiredGates =
                {
                    Gate("COFRE_ITBI_CENARIO_DEFINIDO", "Modus operandi da prefeitura identificado.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.CofrePreenchimento,
                Phase = WorkflowPhase.CofrePatrimonial,
                OrderInPhase = 3,
                Title = "Preencher Cofre com bens",
                Objective = "Transferir bens da pessoa física para a Cofre.",
                DependsOn = { WorkflowStepCode.CofreApuracaoItbi },
                RequiredDocuments =
                {
                    Doc("COFRE.ALT_CAPITAL_IMOVEL.V1", "Alteração contratual por integralização de imóveis", false),
                    Doc("COFRE.ALT_CAPITAL_PARTICIPACOES.V1", "Alteração contratual por integralização de participações", false)
                },
                RequiredGates =
                {
                    Gate("COFRE_APORTE_OK", "Aporte definido e documentado."),
                    Gate("COFRE_TEMA796_OK", "Sem inconsistência estrutural crítica de Tema 796.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.CofreVertenteItbiNaoCobraDiferenca,
                Phase = WorkflowPhase.CofrePatrimonial,
                OrderInPhase = 4,
                Title = "Vertente A do ITBI",
                Objective = "Executar a trilha em que o município não cobra a diferença.",
                DependsOn = { WorkflowStepCode.CofrePreenchimento },
                Optional = true,
                RequiredGates =
                {
                    Gate("COFRE_VERTENTE_A_ATIVA", "Cenário municipal compatível com a vertente A.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.CofreVertenteItbiCobraDiferenca,
                Phase = WorkflowPhase.CofrePatrimonial,
                OrderInPhase = 5,
                Title = "Vertente B do ITBI",
                Objective = "Executar a trilha em que o município cobra a diferença.",
                DependsOn = { WorkflowStepCode.CofrePreenchimento },
                Optional = true,
                RequiredGates =
                {
                    Gate("COFRE_VERTENTE_B_ATIVA", "Cenário municipal compatível com a vertente B.")
                }
            },

            // ═══════════════════════════════════════════════
            // FASE 3 — VEÍCULO (9 passos)
            // ═══════════════════════════════════════════════
            new()
            {
                Code = WorkflowStepCode.VeiculoConstituicao,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 1,
                Title = "Constituir Veículo",
                Objective = "Criar a célula de comando societário.",
                DependsOn = { WorkflowStepCode.CofrePreenchimento },
                RequiredDocuments =
                {
                    Doc("VEICULO.CONSTITUICAO.V1", "Contrato Social Master da Veículo")
                },
                RequiredGates =
                {
                    Gate("VEICULO_REGENCIA_SA_OK", "Regência supletiva da Lei das S.A. ativada."),
                    Gate("VEICULO_FINALIDADE_OK", "Função de comando societário definida.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoQuotasOrdinarias,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 2,
                Title = "Estruturar quotas ordinárias da Veículo",
                Objective = "Criar a camada econômica da Veículo.",
                DependsOn = { WorkflowStepCode.VeiculoConstituicao },
                RequiredGates =
                {
                    Gate("VEICULO_ORDINARIAS_OK", "Ordinárias espelhadas à Destino.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoQuotaPreferencial,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 3,
                Title = "Estruturar quota preferencial da Veículo",
                Objective = "Criar a camada política de controle.",
                DependsOn = { WorkflowStepCode.VeiculoQuotasOrdinarias },
                RequiredGates =
                {
                    Gate("VEICULO_PREFERENCIAL_OK", "Quota preferencial completamente parametrizada.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoVotoPreferencial,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 4,
                Title = "Definir voto reforçado da preferencial",
                Objective = "Fixar a fórmula de comando político.",
                DependsOn = { WorkflowStepCode.VeiculoQuotaPreferencial },
                RequiredGates =
                {
                    Gate("VEICULO_VOTO_X1_OK", "Fórmula X+1 validada.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoIntegralizacao,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 5,
                Title = "Integralizar a Veículo",
                Objective = "Aportar o valor correspondente ao capital da Cofre.",
                DependsOn = { WorkflowStepCode.VeiculoVotoPreferencial },
                RequiredGates =
                {
                    Gate("VEICULO_INTEGRALIZACAO_OK", "Integralização vinculada ao capital da Cofre.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoReservaCapital,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 6,
                Title = "Reserva de capital da Veículo",
                Objective = "Tratar excedente acima do valor nominal, se houver.",
                DependsOn = { WorkflowStepCode.VeiculoIntegralizacao },
                Optional = true,
                RequiredGates =
                {
                    Gate("VEICULO_RESERVA_CAPITAL_OK", "Reserva de capital tecnicamente suportada.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoCallPreferencial,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 7,
                Title = "Call da quota preferencial",
                Objective = "Disciplinar continuidade do controle em evento sucessório.",
                DependsOn = { WorkflowStepCode.VeiculoIntegralizacao },
                RequiredGates =
                {
                    Gate("VEICULO_CALL_OK", "Call com evento, comprador, preço e prazo definidos.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.CofreAlteracaoTitularidadeParaVeiculo,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 8,
                Title = "Alterar Cofre para entrada da Veículo",
                Objective = "Refletir a titularidade das quotas da Cofre na Veículo.",
                DependsOn = { WorkflowStepCode.VeiculoCallPreferencial },
                RequiredDocuments =
                {
                    Doc("COFRE.ALT_TITULARIDADE_PARA_VEICULO.V1", "Alteração contratual da Cofre para entrada da Veículo")
                },
                RequiredGates =
                {
                    Gate("COFRE_CAUSA_TRANSFERENCIA_OK", "Causa jurídica da transferência definida.")
                }
            },
            new()
            {
                Code = WorkflowStepCode.VeiculoVendaOrdinariasParaDestino,
                Phase = WorkflowPhase.VeiculoTributarioControle,
                OrderInPhase = 9,
                Title = "Vender ordinárias da Veículo para a Destino",
                Objective = "Deslocar a camada econômica para a Destino, sem transferir o controle.",
                DependsOn = { WorkflowStepCode.CofreAlteracaoTitularidadeParaVeiculo },
                RequiredDocuments =
                {
                    Doc("VEICULO.ALT_COMPRA_ORDINARIAS_PELA_DESTINO.V1", "Alteração contratual da Veículo para compra das ordinárias pela Destino")
                },
                RequiredGates =
                {
                    Gate("VEICULO_OPERACAO_ONEROSA_OK", "Operação onerosa definida."),
                    Gate("VEICULO_PREFERENCIAL_FICA_FORA_OK", "Quota preferencial permanece fora da operação.")
                }
            }
        };
    }

    private static WorkflowDocumentRequirement Doc(string code, string name, bool required = true)
        => new() { Code = code, Name = name, Required = required };

    private static WorkflowValidationGate Gate(string code, string description)
        => new() { Code = code, Description = description };
}
