#nullable enable
using Holdus.Infrastructure.Data.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Holdus.Infrastructure.Data.Configurations;

// ═══════════════════════════════════════════════════════════════
// CONFIGURAÇÕES EF CORE — Workflow para PostgreSQL
// ═══════════════════════════════════════════════════════════════

public sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instances");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => x.ProjetoId)
            .IsUnique();

        builder.HasMany(x => x.Steps)
            .WithOne(x => x.WorkflowInstance)
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowStepStateConfiguration : IEntityTypeConfiguration<WorkflowStepStateEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowStepStateEntity> builder)
    {
        builder.ToTable("workflow_step_states");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.StepCode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Phase)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Objective)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.StartedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.FinishedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(x => new { x.WorkflowInstanceId, x.StepCode })
            .IsUnique();

        builder.HasMany(x => x.DependsOn)
            .WithOne(x => x.Step)
            .HasForeignKey(x => x.WorkflowStepStateEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Documents)
            .WithOne(x => x.Step)
            .HasForeignKey(x => x.WorkflowStepStateEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Gates)
            .WithOne(x => x.Step)
            .HasForeignKey(x => x.WorkflowStepStateEntityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BlockingReasons)
            .WithOne(x => x.Step)
            .HasForeignKey(x => x.WorkflowStepStateEntityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WorkflowDependencyConfiguration : IEntityTypeConfiguration<WorkflowDependencyEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowDependencyEntity> builder)
    {
        builder.ToTable("workflow_dependencies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DependsOnStepCode)
            .HasConversion<int>()
            .IsRequired();
    }
}

public sealed class WorkflowDocumentRequirementConfiguration : IEntityTypeConfiguration<WorkflowDocumentRequirementEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowDocumentRequirementEntity> builder)
    {
        builder.ToTable("workflow_document_requirements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(300)
            .IsRequired();
    }
}

public sealed class WorkflowValidationGateConfiguration : IEntityTypeConfiguration<WorkflowValidationGateEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowValidationGateEntity> builder)
    {
        builder.ToTable("workflow_validation_gates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired();
    }
}

public sealed class WorkflowBlockingReasonConfiguration : IEntityTypeConfiguration<WorkflowBlockingReasonEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowBlockingReasonEntity> builder)
    {
        builder.ToTable("workflow_blocking_reasons");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(1000)
            .IsRequired();
    }
}
