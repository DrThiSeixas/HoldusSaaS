using Holdus.Domain.Interfaces;

namespace Holdus.Domain.Entities;

/// <summary>
/// Base para entidades com PK int e multi-tenant.
/// </summary>
public abstract class BaseEntity : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public int Id { get; set; }

    // Multi-tenant
    public Guid TenantId { get; set; }

    // Auditoria
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}

/// <summary>
/// Base para entidades com PK Guid e multi-tenant.
/// </summary>
public abstract class BaseGuidEntity : ITenantEntity, IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Multi-tenant
    public Guid TenantId { get; set; }

    // Auditoria
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Soft delete
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;
}
