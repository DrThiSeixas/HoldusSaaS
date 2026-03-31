using Holdus.Domain.Enums;

namespace Holdus.Domain.Entities;

// ═══════════════════════════════════════════════════════════════
// TEMPLATES E DOCUMENTOS
// ═══════════════════════════════════════════════════════════════

public class TemplateDocumento : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public CategoriaTemplate Categoria { get; set; }
    public int Versao { get; set; } = 1;

    // Conteúdo (TipTap JSON / HTML)
    public string ConteudoHtml { get; set; } = string.Empty;
    public string? PlaceholdersJson { get; set; }   // Lista de {{campos}} detectados
    public string? SchemaValidacao { get; set; }     // JSON Schema para validar preenchimento

    // Compartilhamento
    public bool IsPublico { get; set; }  // Disponível para outros tenants

    // Navegação
    public ICollection<DocumentoGerado> DocumentosGerados { get; set; } = [];
}

public class DocumentoGerado : BaseGuidEntity
{
    public int TemplateId { get; set; }
    public int? ProjetoId { get; set; }

    public string Titulo { get; set; } = string.Empty;
    public string ConteudoHtml { get; set; } = string.Empty;
    public string? DadosPreenchimentoJson { get; set; }  // Dados usados no preenchimento

    // Versionamento
    public int Versao { get; set; } = 1;
    public Guid? VersaoAnteriorId { get; set; }
    public StatusDocumento Status { get; set; } = StatusDocumento.Rascunho;

    // Exportação (chaves no S3/MinIO)
    public string? PdfStorageKey { get; set; }
    public string? DocxStorageKey { get; set; }

    // Navegação
    public TemplateDocumento Template { get; set; } = null!;
    public ProjetoTriade? Projeto { get; set; }
    public DocumentoGerado? VersaoAnterior { get; set; }
}

public class Clausula : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public CategoriaClausula Categoria { get; set; }
    public string TextoHtml { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public int VezesUtilizada { get; set; }
    public bool IsPublica { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// ARQUIVO STORAGE
// Referência polimórfica para S3/MinIO.
// ═══════════════════════════════════════════════════════════════

public class ArquivoStorage : BaseGuidEntity
{
    public string StorageKey { get; set; } = string.Empty;     // tenants/{id}/docs/{guid}.pdf
    public string NomeOriginal { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }

    // Referência polimórfica (qual entidade e ID)
    public string EntidadeRef { get; set; } = string.Empty;    // "PessoaFisica", "Bem", etc
    public string EntidadeRefId { get; set; } = string.Empty;
    public string? Categoria { get; set; }                      // "RG", "Matrícula", "CRLV"
}

// ═══════════════════════════════════════════════════════════════
// FINANCEIRO
// ═══════════════════════════════════════════════════════════════

public class ContratoHonorarios : BaseEntity
{
    public int ProjetoId { get; set; }

    // Valores
    public decimal ValorBruto { get; set; }
    public decimal ValorDeducoes { get; set; }
    public decimal ValorIncentivo { get; set; }

    // Computado: ValorBruto - ValorDeducoes - ValorIncentivo
    public decimal ValorLiquido => ValorBruto - ValorDeducoes - ValorIncentivo;

    public DateOnly DataContrato { get; set; }
    public StatusContrato Status { get; set; } = StatusContrato.Vigente;

    // Documento gerado vinculado
    public Guid? DocumentoGeradoId { get; set; }
    public string? Observacoes { get; set; }

    // Navegação
    public ProjetoTriade Projeto { get; set; } = null!;
    public DocumentoGerado? DocumentoGerado { get; set; }
    public ICollection<Parcela> Parcelas { get; set; } = [];
}

public class Parcela : BaseEntity
{
    public int ContratoId { get; set; }
    public int Numero { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }

    // Pagamento
    public DateOnly? DataPagamento { get; set; }
    public decimal? ValorPago { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }
    public StatusParcela Status { get; set; } = StatusParcela.Pendente;

    // NFS-e
    public Guid? NotaFiscalId { get; set; }

    // Navegação
    public ContratoHonorarios Contrato { get; set; } = null!;
    public NotaFiscal? NotaFiscal { get; set; }
}

public class NotaFiscal : BaseGuidEntity
{
    public int ParcelaId { get; set; }

    // NFS-e
    public string? NumeroNfse { get; set; }
    public string? CodigoVerificacao { get; set; }
    public string? XmlEnvio { get; set; }
    public string? XmlRetorno { get; set; }
    public decimal ValorServico { get; set; }

    // ISS
    public decimal IssAliquota { get; set; }
    public decimal IssValor { get; set; }

    // Status
    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Pendente;
    public DateTimeOffset? EmitidaEm { get; set; }
    public string? ErroMensagem { get; set; }

    // Navegação
    public Parcela Parcela { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════
// LGPD — AUDITORIA E CONSENTIMENTO
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Log de auditoria imutável (append-only).
/// LGPD Art. 37 — Registro das operações de tratamento.
/// Não herda de BaseEntity (não tem soft delete, não é editável).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }  // bigint serial
    public Guid TenantId { get; set; }
    public Guid? UsuarioId { get; set; }

    // Evento
    public AcaoAuditoria Acao { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string EntidadeId { get; set; } = string.Empty;
    public string[]? CamposAcessados { get; set; }

    // Contexto
    public string? IpOrigem { get; set; }
    public string? UserAgent { get; set; }
    public string? DetalhesJson { get; set; }

    // Timestamp (imutável)
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Consentimento LGPD do titular de dados.
/// Art. 8º — Consentimento livre, informado e inequívoco.
/// </summary>
public class ConsentimentoLgpd : BaseGuidEntity
{
    public int PessoaFisicaId { get; set; }
    public BaseLegalLgpd BaseLegal { get; set; }
    public string Finalidade { get; set; } = string.Empty;

    // Consentimento
    public DateTimeOffset AceitoEm { get; set; }
    public DateTimeOffset? ValidoAte { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }

    // Evidência
    public string? IpAceite { get; set; }
    public string HashEvidencia { get; set; } = string.Empty;  // SHA-256 do momento
    public bool Ativo => RevogadoEm == null && (ValidoAte == null || ValidoAte > DateTimeOffset.UtcNow);

    // Navegação
    public PessoaFisica PessoaFisica { get; set; } = null!;
}

// ═══════════════════════════════════════════════════════════════
// CAPTAÇÃO (Wizard de Viabilidade)
// ═══════════════════════════════════════════════════════════════

public class Captacao : BaseEntity
{
    // Prospect
    public string NomeCompleto { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;      // [Encrypted]
    public string Email { get; set; } = string.Empty;          // [Encrypted]
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    // Perfil
    public string? Profissao { get; set; }
    public decimal? RendaMensalEstimada { get; set; }
    public FaixaPatrimonio FaixaPatrimonio { get; set; }
    public NaturezaBens NaturezaBens { get; set; }

    // Família (JSON flexível para dados variáveis)
    public string? DadosFamiliaJson { get; set; }

    // Origem
    public ComoConheceu ComoConheceu { get; set; } = ComoConheceu.Indicacao;
    public string? IndicadoPor { get; set; }

    // Conversão
    public StatusCaptacao Status { get; set; } = StatusCaptacao.EmPreenchimento;
    public int? PessoaFisicaGeradaId { get; set; }
    public int? ProjetoGeradoId { get; set; }
    public Guid? ConsentimentoLgpdId { get; set; }

    public string? Observacoes { get; set; }
}
