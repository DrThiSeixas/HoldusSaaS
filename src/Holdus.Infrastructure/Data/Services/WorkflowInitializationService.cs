#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Holdus.Domain.Entities;
using Holdus.Infrastructure.Data.Workflow;
using Microsoft.EntityFrameworkCore;

namespace Holdus.Infrastructure.Data.Services;

// ═══════════════════════════════════════════════════════════════
// SERVIÇO DE INICIALIZAÇÃO DO WORKFLOW — PostgreSQL
// Cria a instância a partir da factory e persiste no banco.
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowInitializationService
{
    private readonly AppDbContext _db;

    public WorkflowInitializationService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Garante que existe uma instância de workflow para o projeto.
    /// Se já existir, retorna a existente com todos os includes.
    /// Se não existir, cria a partir da ThreeCellsWorkflowFactory.
    /// </summary>
    public async Task<WorkflowInstance> EnsureCreatedAsync(Guid projetoId, CancellationToken ct = default)
    {
        var existing = await _db.WorkflowInstances
            .Include(x => x.Steps)
                .ThenInclude(x => x.DependsOn)
            .Include(x => x.Steps)
                .ThenInclude(x => x.Documents)
            .Include(x => x.Steps)
                .ThenInclude(x => x.Gates)
            .Include(x => x.Steps)
                .ThenInclude(x => x.BlockingReasons)
            .FirstOrDefaultAsync(x => x.ProjetoId == projetoId, ct);

        if (existing is not null)
            return existing;

        // Criar a partir da factory canônica
        var definitionWorkflow = ThreeCellsWorkflowFactory.Create(projetoId);

        var instance = new WorkflowInstance
        {
            ProjetoId = projetoId,
            Name = definitionWorkflow.Name
        };

        foreach (var def in definitionWorkflow.Definitions
            .OrderBy(x => x.Phase)
            .ThenBy(x => x.OrderInPhase))
        {
            var step = new WorkflowStepStateEntity
            {
                WorkflowInstanceId = instance.Id,
                StepCode = def.Code,
                Phase = def.Phase,
                OrderInPhase = def.OrderInPhase,
                Title = def.Title,
                Objective = def.Objective,
                Status = WorkflowStepStatus.NaoIniciado,
                Optional = def.Optional
            };

            foreach (var dep in def.DependsOn)
            {
                step.DependsOn.Add(new WorkflowDependencyEntity
                {
                    DependsOnStepCode = dep
                });
            }

            foreach (var doc in def.RequiredDocuments)
            {
                step.Documents.Add(new WorkflowDocumentRequirementEntity
                {
                    Code = doc.Code,
                    Name = doc.Name,
                    Required = doc.Required,
                    Generated = false
                });
            }

            foreach (var gate in def.RequiredGates)
            {
                step.Gates.Add(new WorkflowValidationGateEntity
                {
                    Code = gate.Code,
                    Description = gate.Description,
                    Blocking = gate.Blocking,
                    Passed = false
                });
            }

            instance.Steps.Add(step);
        }

        _db.WorkflowInstances.Add(instance);
        await _db.SaveChangesAsync(ct);

        return instance;
    }
}
