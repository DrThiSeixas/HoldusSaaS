#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Holdus.Domain.Entities;

namespace Holdus.Application.Services;

// ═══════════════════════════════════════════════════════════════
// WORKFLOW ENGINE — Motor de execução do Método Tríade Capital
// Versão definitiva — Thiago Seixas
//
// "Nenhuma fase avança se a anterior não estiver fechada."
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowAdvanceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> BlockingReasons { get; set; } = new();
}

public sealed class WorkflowEngine
{
    public WorkflowAdvanceResult CanStart(ProjectWorkflow workflow, WorkflowStepCode code)
    {
        var def = workflow.GetDefinition(code);
        var state = workflow.GetState(code);

        if (state.Status == WorkflowStepStatus.Concluido)
            return new WorkflowAdvanceResult
            {
                Success = false,
                Message = "O passo já está concluído."
            };

        var blocking = new List<string>();

        foreach (var dependency in def.DependsOn)
        {
            var depState = workflow.GetState(dependency);
            if (depState.Status != WorkflowStepStatus.Concluido)
                blocking.Add($"Dependência não concluída: {dependency}");
        }

        return new WorkflowAdvanceResult
        {
            Success = blocking.Count == 0,
            Message = blocking.Count == 0 ? "Passo apto para início." : "Há dependências pendentes.",
            BlockingReasons = blocking
        };
    }

    public WorkflowAdvanceResult ValidateStep(ProjectWorkflow workflow, WorkflowStepCode code)
    {
        var def = workflow.GetDefinition(code);
        var state = workflow.GetState(code);

        var blocking = new List<string>();

        foreach (var doc in def.RequiredDocuments.Where(x => x.Required))
        {
            if (!doc.Generated)
                blocking.Add($"Documento obrigatório não gerado: {doc.Code}");
        }

        foreach (var gate in def.RequiredGates.Where(x => x.Blocking))
        {
            if (!gate.Passed)
                blocking.Add($"Validação não cumprida: {gate.Code}");
        }

        state.BlockingReasons = blocking;

        if (blocking.Count > 0)
        {
            state.Status = WorkflowStepStatus.Bloqueado;
            return new WorkflowAdvanceResult
            {
                Success = false,
                Message = "O passo está bloqueado.",
                BlockingReasons = blocking
            };
        }

        state.Status = WorkflowStepStatus.AptoParaGeracao;
        return new WorkflowAdvanceResult
        {
            Success = true,
            Message = "O passo está validado e apto."
        };
    }

    public WorkflowAdvanceResult StartStep(ProjectWorkflow workflow, WorkflowStepCode code)
    {
        var canStart = CanStart(workflow, code);
        if (!canStart.Success)
            return canStart;

        var state = workflow.GetState(code);
        state.Status = WorkflowStepStatus.EmCadastro;
        state.StartedAt = DateTime.UtcNow;

        return new WorkflowAdvanceResult
        {
            Success = true,
            Message = "Passo iniciado."
        };
    }

    public WorkflowAdvanceResult CompleteStep(ProjectWorkflow workflow, WorkflowStepCode code)
    {
        var validation = ValidateStep(workflow, code);
        if (!validation.Success)
            return validation;

        var state = workflow.GetState(code);
        state.Status = WorkflowStepStatus.Concluido;
        state.FinishedAt = DateTime.UtcNow;

        return new WorkflowAdvanceResult
        {
            Success = true,
            Message = "Passo concluído."
        };
    }

    public IReadOnlyList<WorkflowStepDefinition> GetNextAvailableSteps(ProjectWorkflow workflow)
    {
        return workflow.Definitions
            .Where(def =>
            {
                var state = workflow.GetState(def.Code);
                if (state.Status == WorkflowStepStatus.Concluido)
                    return false;

                return def.DependsOn.All(dep =>
                    workflow.GetState(dep).Status == WorkflowStepStatus.Concluido);
            })
            .OrderBy(x => x.Phase)
            .ThenBy(x => x.OrderInPhase)
            .ToList();
    }

    public bool IsPhaseCompleted(ProjectWorkflow workflow, WorkflowPhase phase)
    {
        var steps = workflow.Definitions.Where(x => x.Phase == phase).ToList();
        return steps.All(step =>
            workflow.GetState(step.Code).Status == WorkflowStepStatus.Concluido);
    }

    public bool IsWorkflowCompleted(ProjectWorkflow workflow)
    {
        return workflow.Definitions.All(step =>
            workflow.GetState(step.Code).Status == WorkflowStepStatus.Concluido);
    }
}
