#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Holdus.Domain.Entities;
using Holdus.Infrastructure.Data.Workflow;
using Microsoft.EntityFrameworkCore;

namespace Holdus.Infrastructure.Data.Services;

// ═══════════════════════════════════════════════════════════════
// WORKFLOW EXECUTION SERVICE — Persistência PostgreSQL
// Versão definitiva — adaptada de Thiago Seixas
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowExecutionService
{
    private readonly AppDbContext _db;

    public WorkflowExecutionService(AppDbContext db) => _db = db;

    public async Task<WorkflowInstance?> LoadAsync(Guid projetoId, CancellationToken ct = default)
    {
        return await _db.WorkflowInstances
            .Include(x => x.Steps).ThenInclude(x => x.DependsOn)
            .Include(x => x.Steps).ThenInclude(x => x.Documents)
            .Include(x => x.Steps).ThenInclude(x => x.Gates)
            .Include(x => x.Steps).ThenInclude(x => x.BlockingReasons)
            .FirstOrDefaultAsync(x => x.ProjetoId == projetoId, ct);
    }

    public async Task<(bool Success, string Message)> StartStepAsync(
        Guid projetoId, WorkflowStepCode code, CancellationToken ct = default)
    {
        var workflow = await LoadAsync(projetoId, ct);
        if (workflow is null) return (false, "Workflow não encontrado.");

        var step = workflow.Steps.SingleOrDefault(x => x.StepCode == code);
        if (step is null) return (false, "Passo não encontrado.");

        if (step.Status == WorkflowStepStatus.Concluido)
            return (false, "O passo já está concluído.");

        var errors = new List<string>();
        foreach (var dep in step.DependsOn)
        {
            var depStep = workflow.Steps.SingleOrDefault(x => x.StepCode == dep.DependsOnStepCode);
            if (depStep != null && depStep.Status != WorkflowStepStatus.Concluido && !depStep.Optional)
                errors.Add($"Dependência pendente: {dep.DependsOnStepCode}");
        }

        if (errors.Count > 0) return (false, string.Join(" | ", errors));

        step.Status = WorkflowStepStatus.EmCadastro;
        step.StartedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return (true, "Passo iniciado.");
    }

    public async Task<(bool Success, string Message)> SetDocumentGeneratedAsync(
        Guid projetoId, WorkflowStepCode code, string documentCode, bool generated,
        CancellationToken ct = default)
    {
        var workflow = await LoadAsync(projetoId, ct);
        if (workflow is null) return (false, "Workflow não encontrado.");

        var step = workflow.Steps.SingleOrDefault(x => x.StepCode == code);
        if (step is null) return (false, "Passo não encontrado.");

        var doc = step.Documents.SingleOrDefault(x => x.Code == documentCode);
        if (doc is null) return (false, "Documento não encontrado no passo.");

        doc.Generated = generated;
        await _db.SaveChangesAsync(ct);
        return (true, "Documento atualizado.");
    }

    public async Task<(bool Success, string Message)> SetGateAsync(
        Guid projetoId, WorkflowStepCode code, string gateCode, bool passed,
        CancellationToken ct = default)
    {
        var workflow = await LoadAsync(projetoId, ct);
        if (workflow is null) return (false, "Workflow não encontrado.");

        var step = workflow.Steps.SingleOrDefault(x => x.StepCode == code);
        if (step is null) return (false, "Passo não encontrado.");

        var gate = step.Gates.SingleOrDefault(x => x.Code == gateCode);
        if (gate is null) return (false, "Validação não encontrada no passo.");

        gate.Passed = passed;
        await _db.SaveChangesAsync(ct);
        return (true, "Validação atualizada.");
    }

    public async Task<(bool Success, string Message, List<string> BlockingReasons)> ValidateAndCompleteStepAsync(
        Guid projetoId, WorkflowStepCode code, CancellationToken ct = default)
    {
        var workflow = await LoadAsync(projetoId, ct);
        if (workflow is null) return (false, "Workflow não encontrado.", new());

        var step = workflow.Steps.SingleOrDefault(x => x.StepCode == code);
        if (step is null) return (false, "Passo não encontrado.", new());

        // Limpar bloqueios anteriores
        step.BlockingReasons.Clear();

        // Verificar documentos obrigatórios
        foreach (var doc in step.Documents.Where(x => x.Required && !x.Generated))
        {
            step.BlockingReasons.Add(new WorkflowBlockingReasonEntity
            {
                Reason = $"Documento obrigatório não gerado: {doc.Code}"
            });
        }

        // Verificar gates bloqueantes
        foreach (var gate in step.Gates.Where(x => x.Blocking && !x.Passed))
        {
            step.BlockingReasons.Add(new WorkflowBlockingReasonEntity
            {
                Reason = $"Validação obrigatória não cumprida: {gate.Code}"
            });
        }

        if (step.BlockingReasons.Count > 0)
        {
            step.Status = WorkflowStepStatus.Bloqueado;
            await _db.SaveChangesAsync(ct);
            return (false, "O passo está bloqueado.",
                step.BlockingReasons.Select(x => x.Reason).ToList());
        }

        // Concluir
        step.Status = WorkflowStepStatus.Concluido;
        step.FinishedAtUtc = DateTime.UtcNow;

        // Verificar se todo o workflow foi concluído
        var allDone = workflow.Steps.All(x =>
            x.Status == WorkflowStepStatus.Concluido || x.Optional);
        if (allDone)
            workflow.CompletedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return (true, "Passo concluído.", new());
    }

    public async Task<List<WorkflowStepStateEntity>> GetNextAvailableStepsAsync(
        Guid projetoId, CancellationToken ct = default)
    {
        var workflow = await LoadAsync(projetoId, ct);
        if (workflow is null) return new();

        var completed = workflow.Steps
            .Where(s => s.Status == WorkflowStepStatus.Concluido)
            .Select(s => s.StepCode)
            .ToHashSet();

        return workflow.Steps
            .Where(s => s.Status != WorkflowStepStatus.Concluido &&
                        s.DependsOn.All(d => completed.Contains(d.DependsOnStepCode)))
            .OrderBy(s => s.Phase).ThenBy(s => s.OrderInPhase)
            .ToList();
    }
}
