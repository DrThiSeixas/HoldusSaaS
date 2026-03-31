using Holdus.Domain.Entities;
using Holdus.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Holdus.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly Guid _tenantId;
    private readonly Guid? _currentUserId;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.TenantId;
        _currentUserId = tenantProvider.UserId;
    }

    // ═══════════════════════════════════════════════════════════
    // DbSets
    // ═══════════════════════════════════════════════════════════

    // Infraestrutura
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ConsentimentoLgpd> ConsentimentosLgpd => Set<ConsentimentoLgpd>();

    // Core
    public DbSet<PessoaFisica> PessoasFisicas => Set<PessoaFisica>();
    public DbSet<ProjetoTriade> Projetos => Set<ProjetoTriade>();
    public DbSet<ParticipanteProjeto> Participantes => Set<ParticipanteProjeto>();
    public DbSet<Celula> Celulas => Set<Celula>();
    public DbSet<SocioCelula> SociosCelulas => Set<SocioCelula>();
    public DbSet<Bem> Bens => Set<Bem>();
    public DbSet<FaseProjeto> FasesProjeto => Set<FaseProjeto>();
    public DbSet<PassoFase> PassosFase => Set<PassoFase>();

    // Documentos
    public DbSet<TemplateDocumento> Templates => Set<TemplateDocumento>();
    public DbSet<DocumentoGerado> DocumentosGerados => Set<DocumentoGerado>();
    public DbSet<Clausula> Clausulas => Set<Clausula>();
    public DbSet<ArquivoStorage> Arquivos => Set<ArquivoStorage>();

    // Financeiro
    public DbSet<ContratoHonorarios> ContratosHonorarios => Set<ContratoHonorarios>();
    public DbSet<Parcela> Parcelas => Set<Parcela>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    // Captação
    public DbSet<Captacao> Captacoes => Set<Captacao>();

    // Workflow
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowInstance> WorkflowInstances => Set<Holdus.Infrastructure.Data.Workflow.WorkflowInstance>();
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowStepStateEntity> WorkflowStepStates => Set<Holdus.Infrastructure.Data.Workflow.WorkflowStepStateEntity>();
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowDependencyEntity> WorkflowDependencies => Set<Holdus.Infrastructure.Data.Workflow.WorkflowDependencyEntity>();
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowDocumentRequirementEntity> WorkflowDocumentRequirements => Set<Holdus.Infrastructure.Data.Workflow.WorkflowDocumentRequirementEntity>();
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowValidationGateEntity> WorkflowValidationGates => Set<Holdus.Infrastructure.Data.Workflow.WorkflowValidationGateEntity>();
    public DbSet<Holdus.Infrastructure.Data.Workflow.WorkflowBlockingReasonEntity> WorkflowBlockingReasons => Set<Holdus.Infrastructure.Data.Workflow.WorkflowBlockingReasonEntity>();

    // ═══════════════════════════════════════════════════════════
    // MODEL CONFIGURATION
    // ═══════════════════════════════════════════════════════════

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // ─── Global Query Filters ───────────────────────────
        // EF Core permite apenas UM query filter por entidade.
        // Combinamos tenant + soft delete num único filtro.

        foreach (var entityType in mb.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clrType);
            var isSoftDel = typeof(ISoftDeletable).IsAssignableFrom(clrType);

            if (isTenant && isSoftDel)
            {
                // Combina: TenantId == _tenantId && !IsDeleted
                mb.Entity(clrType).HasQueryFilter(BuildCombinedFilter(clrType));
            }
            else if (isTenant)
            {
                mb.Entity(clrType).HasQueryFilter(BuildTenantFilter(clrType));
            }
            else if (isSoftDel)
            {
                mb.Entity(clrType).HasQueryFilter(BuildSoftDeleteFilter(clrType));
            }
        }

        // ─── Tenant ─────────────────────────────────────────
        mb.Entity<Tenant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Slug).HasMaxLength(50);
            e.Property(x => x.CnpjEscritorio).HasMaxLength(18);
            e.Property(x => x.Municipio).HasMaxLength(100);
            e.Property(x => x.Uf).HasMaxLength(2);
            e.Property(x => x.IssAliquota).HasPrecision(5, 2);
            e.Property(x => x.ConfiguracoesJson).HasColumnType("jsonb");
        });

        // ─── Usuario ────────────────────────────────────────
        mb.Entity<Usuario>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            e.Property(x => x.NomeCompleto).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.OabNumero).HasMaxLength(20);
            e.Property(x => x.OabUf).HasMaxLength(2);
            e.HasOne(x => x.Tenant).WithMany(t => t.Usuarios).HasForeignKey(x => x.TenantId);
        });

        // ─── AuditLog (append-only, sem soft delete) ────────
        mb.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityAlwaysColumn();
            e.HasIndex(x => x.TenantId);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => new { x.TenantId, x.Entidade, x.EntidadeId });
            e.Property(x => x.Entidade).HasMaxLength(100);
            e.Property(x => x.EntidadeId).HasMaxLength(50);
            e.Property(x => x.IpOrigem).HasMaxLength(45);
            e.Property(x => x.DetalhesJson).HasColumnType("jsonb");
        });

        // ─── PessoaFisica ───────────────────────────────────
        mb.Entity<PessoaFisica>(e =>
        {
            e.HasIndex(x => new { x.TenantId, x.Cpf }).IsUnique()
             .HasFilter("\"DeletedAt\" IS NULL");
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.Cpf).HasMaxLength(500);       // Criptografado = maior
            e.Property(x => x.Rg).HasMaxLength(500);
            e.Property(x => x.Celular).HasMaxLength(500);
            e.Property(x => x.Email).HasMaxLength(500);
            e.Property(x => x.NomePai).HasMaxLength(500);
            e.Property(x => x.NomeMae).HasMaxLength(500);
            e.HasOne(x => x.Conjuge).WithOne()
             .HasForeignKey<PessoaFisica>(x => x.ConjugeId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ─── ProjetoTriade ──────────────────────────────────
        mb.Entity<ProjetoTriade>(e =>
        {
            e.HasIndex(x => new { x.TenantId, x.Codigo }).IsUnique()
             .HasFilter("\"DeletedAt\" IS NULL");
            e.Property(x => x.Codigo).HasMaxLength(20);
            e.Property(x => x.NomeProjeto).HasMaxLength(200);
            e.Property(x => x.NomeFamilia).HasMaxLength(200);
            e.HasOne(x => x.AdvogadoResponsavel).WithMany()
             .HasForeignKey(x => x.AdvogadoResponsavelId);
        });

        // ─── Celula ─────────────────────────────────────────
        mb.Entity<Celula>(e =>
        {
            e.Property(x => x.NomeCelula).HasMaxLength(300);
            e.Property(x => x.Cnpj).HasMaxLength(18);
            e.Property(x => x.RazaoSocial).HasMaxLength(300);
            e.Property(x => x.Nire).HasMaxLength(20);
            e.Property(x => x.CapitalSocialPrevisto).HasPrecision(18, 2);
            e.Property(x => x.CapitalSocialEfetivo).HasPrecision(18, 2);
            e.Property(x => x.EnderecoJson).HasColumnType("jsonb");
            e.HasOne(x => x.Projeto).WithMany(p => p.Celulas).HasForeignKey(x => x.ProjetoId);
            e.HasOne(x => x.Administrador).WithMany().HasForeignKey(x => x.AdministradorId);
        });

        // ─── SocioCelula ────────────────────────────────────
        mb.Entity<SocioCelula>(e =>
        {
            e.HasIndex(x => new { x.CelulaId, x.PessoaFisicaId }).IsUnique();
            e.Property(x => x.ValorPorQuota).HasPrecision(18, 2);
            e.Property(x => x.PercentualParticipacao).HasPrecision(5, 2);
            e.HasOne(x => x.Celula).WithMany(c => c.Socios).HasForeignKey(x => x.CelulaId);
            e.HasOne(x => x.PessoaFisica).WithMany(p => p.Sociedades).HasForeignKey(x => x.PessoaFisicaId);
        });

        // ─── Bem ────────────────────────────────────────────
        mb.Entity<Bem>(e =>
        {
            e.Property(x => x.Descricao).HasMaxLength(500);
            e.Property(x => x.ValorDeclaracaoIR).HasPrecision(18, 2);
            e.Property(x => x.ValorMercado).HasPrecision(18, 2);
            e.Property(x => x.DadosEspecificosJson).HasColumnType("jsonb");
            e.HasOne(x => x.Proprietario).WithMany(p => p.Bens).HasForeignKey(x => x.ProprietarioId);
            e.HasOne(x => x.CelulaDestino).WithMany(c => c.BensDestinados).HasForeignKey(x => x.CelulaDestinoId);
        });

        // ─── Templates e Documentos ─────────────────────────
        mb.Entity<TemplateDocumento>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(200);
            e.Property(x => x.PlaceholdersJson).HasColumnType("jsonb");
            e.Property(x => x.SchemaValidacao).HasColumnType("jsonb");
        });

        mb.Entity<DocumentoGerado>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Titulo).HasMaxLength(300);
            e.Property(x => x.DadosPreenchimentoJson).HasColumnType("jsonb");
            e.HasOne(x => x.Template).WithMany(t => t.DocumentosGerados).HasForeignKey(x => x.TemplateId);
            e.HasOne(x => x.Projeto).WithMany().HasForeignKey(x => x.ProjetoId);
            e.HasOne(x => x.VersaoAnterior).WithOne().HasForeignKey<DocumentoGerado>(x => x.VersaoAnteriorId);
        });

        mb.Entity<Clausula>(e =>
        {
            e.Property(x => x.Titulo).HasMaxLength(200);
        });

        // ─── Financeiro ─────────────────────────────────────
        mb.Entity<ContratoHonorarios>(e =>
        {
            e.HasIndex(x => x.ProjetoId).IsUnique();
            e.Property(x => x.ValorBruto).HasPrecision(18, 2);
            e.Property(x => x.ValorDeducoes).HasPrecision(18, 2);
            e.Property(x => x.ValorIncentivo).HasPrecision(18, 2);
            e.Ignore(x => x.ValorLiquido);  // Computado em memória
            e.HasOne(x => x.Projeto).WithOne(p => p.Contrato).HasForeignKey<ContratoHonorarios>(x => x.ProjetoId);
        });

        mb.Entity<Parcela>(e =>
        {
            e.HasIndex(x => new { x.ContratoId, x.Numero }).IsUnique();
            e.Property(x => x.Valor).HasPrecision(18, 2);
            e.Property(x => x.ValorPago).HasPrecision(18, 2);
            e.Property(x => x.Descricao).HasMaxLength(100);
            e.HasOne(x => x.Contrato).WithMany(c => c.Parcelas).HasForeignKey(x => x.ContratoId);
        });

        mb.Entity<NotaFiscal>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ValorServico).HasPrecision(18, 2);
            e.Property(x => x.IssAliquota).HasPrecision(5, 2);
            e.Property(x => x.IssValor).HasPrecision(18, 2);
            e.HasOne(x => x.Parcela).WithOne(p => p.NotaFiscal).HasForeignKey<NotaFiscal>(x => x.ParcelaId);
        });

        // ─── Captação ───────────────────────────────────────
        mb.Entity<Captacao>(e =>
        {
            e.Property(x => x.NomeCompleto).HasMaxLength(200);
            e.Property(x => x.Telefone).HasMaxLength(500);
            e.Property(x => x.Email).HasMaxLength(500);
            e.Property(x => x.RendaMensalEstimada).HasPrecision(18, 2);
            e.Property(x => x.DadosFamiliaJson).HasColumnType("jsonb");
        });

        // ─── ConsentimentoLgpd ──────────────────────────────
        mb.Entity<ConsentimentoLgpd>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Finalidade).HasMaxLength(500);
            e.Property(x => x.IpAceite).HasMaxLength(45);
            e.Property(x => x.HashEvidencia).HasMaxLength(128);
            e.Ignore(x => x.Ativo);  // Computado em memória
            e.HasOne(x => x.PessoaFisica).WithMany().HasForeignKey(x => x.PessoaFisicaId);
        });

        // ─── ArquivoStorage ─────────────────────────────────
        mb.Entity<ArquivoStorage>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.StorageKey).HasMaxLength(500);
            e.Property(x => x.NomeOriginal).HasMaxLength(200);
            e.Property(x => x.ContentType).HasMaxLength(100);
            e.Property(x => x.EntidadeRef).HasMaxLength(50);
            e.Property(x => x.EntidadeRefId).HasMaxLength(50);
            e.HasIndex(x => new { x.TenantId, x.EntidadeRef, x.EntidadeRefId });
        });

        // ─── Workflow ──────────────────────────────────────────
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowInstanceConfiguration());
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowStepStateConfiguration());
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowDependencyConfiguration());
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowDocumentRequirementConfiguration());
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowValidationGateConfiguration());
        mb.ApplyConfiguration(new Holdus.Infrastructure.Data.Configurations.WorkflowBlockingReasonConfiguration());
    }

    // ═══════════════════════════════════════════════════════════
    // AUDIT INTERCEPTOR — SaveChanges
    // ═══════════════════════════════════════════════════════════

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void ApplyAuditInfo()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            // Auto-set TenantId
            if (entry.Entity is ITenantEntity tenantEntity && entry.State == EntityState.Added)
            {
                if (tenantEntity.TenantId == Guid.Empty)
                    tenantEntity.TenantId = _tenantId;
            }

            // Audit fields
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.UpdatedAt = now;
                    auditable.CreatedBy ??= _currentUserId;
                    auditable.UpdatedBy = _currentUserId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = _currentUserId;
                }
            }

            // Soft delete
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.DeletedAt = now;
                softDeletable.DeletedBy = _currentUserId;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // QUERY FILTER BUILDERS
    // ═══════════════════════════════════════════════════════════

    private System.Linq.Expressions.LambdaExpression BuildTenantFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        var prop = System.Linq.Expressions.Expression.Property(param, nameof(ITenantEntity.TenantId));
        var tenantId = System.Linq.Expressions.Expression.Constant(_tenantId);
        var body = System.Linq.Expressions.Expression.Equal(prop, tenantId);
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }

    private System.Linq.Expressions.LambdaExpression BuildSoftDeleteFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");
        // DeletedAt == null (não deletado)
        var prop = System.Linq.Expressions.Expression.Property(param, nameof(ISoftDeletable.DeletedAt));
        var nullVal = System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?));
        var body = System.Linq.Expressions.Expression.Equal(prop, nullVal);
        return System.Linq.Expressions.Expression.Lambda(body, param);
    }

    /// <summary>
    /// Combina tenant + soft delete num único filtro:
    /// e => e.TenantId == _tenantId && e.DeletedAt == null
    /// </summary>
    private System.Linq.Expressions.LambdaExpression BuildCombinedFilter(Type entityType)
    {
        var param = System.Linq.Expressions.Expression.Parameter(entityType, "e");

        // TenantId == _tenantId
        var tenantProp = System.Linq.Expressions.Expression.Property(param, nameof(ITenantEntity.TenantId));
        var tenantVal = System.Linq.Expressions.Expression.Constant(_tenantId);
        var tenantCheck = System.Linq.Expressions.Expression.Equal(tenantProp, tenantVal);

        // DeletedAt == null
        var deletedProp = System.Linq.Expressions.Expression.Property(param, nameof(ISoftDeletable.DeletedAt));
        var nullVal = System.Linq.Expressions.Expression.Constant(null, typeof(DateTimeOffset?));
        var notDeleted = System.Linq.Expressions.Expression.Equal(deletedProp, nullVal);

        // TenantId == _tenantId && DeletedAt == null
        var combined = System.Linq.Expressions.Expression.AndAlso(tenantCheck, notDeleted);

        return System.Linq.Expressions.Expression.Lambda(combined, param);
    }
}

// ═══════════════════════════════════════════════════════════════
// TENANT PROVIDER — Injetado via DI, lê o tenant do JWT
// ═══════════════════════════════════════════════════════════════

public interface ITenantProvider
{
    Guid TenantId { get; }
    Guid? UserId { get; }
}
