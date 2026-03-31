#nullable enable
using System;
using System.Collections.Generic;
using Holdus.Domain.Entities;

namespace Holdus.Infrastructure.Data.Workflow;

// ═══════════════════════════════════════════════════════════════
// PERSISTÊNCIA DO WORKFLOW — Entidades EF Core para PostgreSQL
// Versão definitiva — adaptada de Thiago Seixas
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowInstance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjetoId { get; set; }
    public string Name { get; set; } = "Workflow 3 Células";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<WorkflowStepStateEntity> Steps { get; set; } = new List<WorkflowStepStateEntity>();
}

public sealed class WorkflowStepStateEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance? WorkflowInstance { get; set; }

    public WorkflowStepCode StepCode { get; set; }
    public WorkflowPhase Phase { get; set; }
    public int OrderInPhase { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;

    public WorkflowStepStatus Status { get; set; } = WorkflowStepStatus.NaoIniciado;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string? Notes { get; set; }
    public bool Optional { get; set; }

    public ICollection<WorkflowDependencyEntity> DependsOn { get; set; } = new List<WorkflowDependencyEntity>();
    public ICollection<WorkflowDocumentRequirementEntity> Documents { get; set; } = new List<WorkflowDocumentRequirementEntity>();
    public ICollection<WorkflowValidationGateEntity> Gates { get; set; } = new List<WorkflowValidationGateEntity>();
    public ICollection<WorkflowBlockingReasonEntity> BlockingReasons { get; set; } = new List<WorkflowBlockingReasonEntity>();
}

public sealed class WorkflowDependencyEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowStepStateEntityId { get; set; }
    public WorkflowStepStateEntity? Step { get; set; }
    public WorkflowStepCode DependsOnStepCode { get; set; }
}

public sealed class WorkflowDocumentRequirementEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowStepStateEntityId { get; set; }
    public WorkflowStepStateEntity? Step { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public bool Generated { get; set; }
}

public sealed class WorkflowValidationGateEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowStepStateEntityId { get; set; }
    public WorkflowStepStateEntity? Step { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public bool Blocking { get; set; } = true;
}

public sealed class WorkflowBlockingReasonEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkflowStepStateEntityId { get; set; }
    public WorkflowStepStateEntity? Step { get; set; }
    public string Reason { get; set; } = string.Empty;
}
