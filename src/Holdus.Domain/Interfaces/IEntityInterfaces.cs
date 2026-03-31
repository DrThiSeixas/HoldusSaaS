namespace Holdus.Domain.Interfaces;

/// <summary>
/// Entidade pertence a um tenant (escritório).
/// Todas as queries são filtradas automaticamente pelo TenantId.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}

/// <summary>
/// Entidade com campos de auditoria automáticos.
/// Preenchidos via SaveChanges interceptor.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}

/// <summary>
/// Entidade com soft delete (LGPD: não apaga, marca como deletado).
/// Query filter global exclui registros deletados.
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
    bool IsDeleted { get; }
}
