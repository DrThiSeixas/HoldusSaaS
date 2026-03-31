#nullable enable
using Holdus.Application.DTOs;
using Holdus.Domain.Entities;
using Holdus.Infrastructure.Data.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holdus.API.Controllers;

[ApiController]
[Route("api/projetos/{projetoId:guid}/workflow")]
[Authorize]
public class WorkflowController : ControllerBase
{
    private readonly WorkflowInitializationService _init;
    private readonly WorkflowExecutionService _exec;

    public WorkflowController(WorkflowInitializationService init, WorkflowExecutionService exec)
    {
        _init = init;
        _exec = exec;
    }

    /// <summary>
    /// Inicializa ou retorna o workflow do projeto.
    /// Cria automaticamente os 20 passos do Método Tríade Capital se não existir.
    /// </summary>
    [HttpPost("inicializar")]
    public async Task<IActionResult> Inicializar(Guid projetoId)
    {
        var wf = await _init.EnsureCreatedAsync(projetoId);
        return Ok(new ApiResponse<object>(true, new { wf.Id, wf.Name, TotalPassos = wf.Steps.Count },
            "Workflow inicializado"));
    }

    /// <summary>
    /// Retorna o workflow completo com fases, passos, gates e documentos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detalhe(Guid projetoId)
    {
        var wf = await _exec.LoadAsync(projetoId);
        if (wf == null) return NotFound(new ApiResponse<object>(false, null, "Workflow não encontrado. Inicialize primeiro."));

        var fases = wf.Steps
            .GroupBy(s => s.Phase)
            .OrderBy(g => g.Key)
            .Select(g => new WorkflowFaseResponse(
                (int)g.Key,
                g.Key switch
                {
                    WorkflowPhase.DestinoSucessorio => "Fase 1 — Destino",
                    WorkflowPhase.CofrePatrimonial => "Fase 2 — Cofre",
                    WorkflowPhase.VeiculoTributarioControle => "Fase 3 — Veículo",
                    _ => "Desconhecida"
                },
                g.Count(),
                g.Count(s => s.Status == WorkflowStepStatus.Concluido),
                g.Count() > 0
                    ? (int)(g.Count(s => s.Status == WorkflowStepStatus.Concluido) * 100.0 / g.Count())
                    : 0,
                g.OrderBy(s => s.OrderInPhase).Select(s => MapStep(s)).ToList()
            ))
            .ToList();

        var dto = new WorkflowDetalheResponse(
            wf.Id, wf.Name, wf.CreatedAtUtc, wf.CompletedAtUtc, fases
        );

        return Ok(new ApiResponse<WorkflowDetalheResponse>(true, dto));
    }

    /// <summary>
    /// Retorna resumo do progresso do workflow.
    /// </summary>
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo(Guid projetoId)
    {
        var wf = await _exec.LoadAsync(projetoId);
        if (wf == null) return NotFound(new ApiResponse<object>(false, null, "Workflow não encontrado."));

        var total = wf.Steps.Count;
        var concluidos = wf.Steps.Count(s => s.Status == WorkflowStepStatus.Concluido);
        var bloqueados = wf.Steps.Count(s => s.Status == WorkflowStepStatus.Bloqueado);
        var passoAtual = wf.Steps
            .Where(s => s.Status != WorkflowStepStatus.Concluido && s.Status != WorkflowStepStatus.NaoIniciado)
            .OrderBy(s => s.Phase).ThenBy(s => s.OrderInPhase)
            .FirstOrDefault();

        var faseAtual = passoAtual?.Phase switch
        {
            WorkflowPhase.DestinoSucessorio => "Destino",
            WorkflowPhase.CofrePatrimonial => "Cofre",
            WorkflowPhase.VeiculoTributarioControle => "Veículo",
            _ => null
        };

        var dto = new WorkflowResumoResponse(
            wf.Id, total, concluidos, bloqueados,
            total > 0 ? (int)(concluidos * 100.0 / total) : 0,
            faseAtual, passoAtual?.Title
        );

        return Ok(new ApiResponse<WorkflowResumoResponse>(true, dto));
    }

    /// <summary>
    /// Retorna os próximos passos disponíveis para execução.
    /// </summary>
    [HttpGet("proximos")]
    public async Task<IActionResult> ProximosPassos(Guid projetoId)
    {
        var steps = await _exec.GetNextAvailableStepsAsync(projetoId);
        var dtos = steps.Select(MapStep).ToList();
        return Ok(new ApiResponse<List<WorkflowPassoResponse>>(true, dtos));
    }

    /// <summary>
    /// Inicia um passo do workflow (muda status para EmCadastro).
    /// </summary>
    [HttpPost("iniciar-passo")]
    public async Task<IActionResult> IniciarPasso(Guid projetoId, [FromBody] WorkflowStartStepRequest req)
    {
        var code = (WorkflowStepCode)req.StepCode;
        var (ok, msg) = await _exec.StartStepAsync(projetoId, code);
        return ok
            ? Ok(new ApiResponse<WorkflowActionResponse>(true, new(true, msg, new())))
            : BadRequest(new ApiResponse<WorkflowActionResponse>(false, new(false, msg, new())));
    }

    /// <summary>
    /// Marca um documento como gerado.
    /// </summary>
    [HttpPost("set-documento")]
    public async Task<IActionResult> SetDocumento(Guid projetoId, [FromBody] WorkflowSetDocRequest req)
    {
        var code = (WorkflowStepCode)req.StepCode;
        var (ok, msg) = await _exec.SetDocumentGeneratedAsync(projetoId, code, req.DocumentCode, req.Generated);
        return ok
            ? Ok(new ApiResponse<WorkflowActionResponse>(true, new(true, msg, new())))
            : BadRequest(new ApiResponse<WorkflowActionResponse>(false, new(false, msg, new())));
    }

    /// <summary>
    /// Marca uma validação (gate) como cumprida ou não.
    /// </summary>
    [HttpPost("set-gate")]
    public async Task<IActionResult> SetGate(Guid projetoId, [FromBody] WorkflowSetGateRequest req)
    {
        var code = (WorkflowStepCode)req.StepCode;
        var (ok, msg) = await _exec.SetGateAsync(projetoId, code, req.GateCode, req.Passed);
        return ok
            ? Ok(new ApiResponse<WorkflowActionResponse>(true, new(true, msg, new())))
            : BadRequest(new ApiResponse<WorkflowActionResponse>(false, new(false, msg, new())));
    }

    /// <summary>
    /// Valida e conclui um passo. Se houver bloqueios, retorna a lista de motivos.
    /// </summary>
    [HttpPost("concluir-passo")]
    public async Task<IActionResult> ConcluirPasso(Guid projetoId, [FromBody] WorkflowCompleteStepRequest req)
    {
        var code = (WorkflowStepCode)req.StepCode;
        var (ok, msg, reasons) = await _exec.ValidateAndCompleteStepAsync(projetoId, code);
        var response = new WorkflowActionResponse(ok, msg, reasons);
        return ok
            ? Ok(new ApiResponse<WorkflowActionResponse>(true, response))
            : BadRequest(new ApiResponse<WorkflowActionResponse>(false, response));
    }

    // ═══════════════════════════════════════════════════════════

    private static WorkflowPassoResponse MapStep(Infrastructure.Data.Workflow.WorkflowStepStateEntity s) => new(
        (int)s.StepCode, (int)s.Phase, s.OrderInPhase,
        s.Title, s.Objective, (int)s.Status,
        s.Status switch
        {
            WorkflowStepStatus.NaoIniciado => "Não iniciado",
            WorkflowStepStatus.EmCadastro => "Em cadastro",
            WorkflowStepStatus.EmValidacao => "Em validação",
            WorkflowStepStatus.PendenteDocumento => "Pendente documento",
            WorkflowStepStatus.PendenteTributo => "Pendente tributo",
            WorkflowStepStatus.Bloqueado => "Bloqueado",
            WorkflowStepStatus.AptoParaGeracao => "Apto para geração",
            WorkflowStepStatus.Gerado => "Gerado",
            WorkflowStepStatus.Concluido => "Concluído",
            _ => "Desconhecido"
        },
        s.Optional, s.StartedAtUtc, s.FinishedAtUtc,
        s.Documents.Select(d => new WorkflowDocResponse(d.Code, d.Name, d.Required, d.Generated)).ToList(),
        s.Gates.Select(g => new WorkflowGateResponse(g.Code, g.Description, g.Passed, g.Blocking)).ToList(),
        s.BlockingReasons.Select(b => b.Reason).ToList(),
        s.DependsOn.Select(d => (int)d.DependsOnStepCode).ToList()
    );
}
