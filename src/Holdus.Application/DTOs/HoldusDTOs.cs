#nullable enable
using Holdus.Domain.Enums;
using Holdus.Domain.Entities;

namespace Holdus.Application.DTOs;

// ═══════════════════════════════════════════════════════════════
// PESSOA FÍSICA — DTOs
// ═══════════════════════════════════════════════════════════════

public record PessoaCreateRequest(
    string Nome,
    string Cpf,
    string? Email,
    string? Celular,
    DateOnly? DataNascimento,
    string? Profissao,
    EstadoCivil EstadoCivil,
    RegimeBens? RegimeBens,
    int? ConjugeId,
    string? NomePai,
    string? NomeMae,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Observacoes
);

public record PessoaUpdateRequest(
    string Nome,
    string? Email,
    string? Celular,
    DateOnly? DataNascimento,
    string? Profissao,
    EstadoCivil EstadoCivil,
    RegimeBens? RegimeBens,
    int? ConjugeId,
    string? NomePai,
    string? NomeMae,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Observacoes
);

public record PessoaResponse(
    int Id,
    string Nome,
    string Cpf,
    string? Email,
    string? Celular,
    DateOnly? DataNascimento,
    string? Profissao,
    string Nacionalidade,
    EstadoCivil EstadoCivil,
    RegimeBens? RegimeBens,
    int? ConjugeId,
    string? ConjugeNome,
    string? Cidade,
    string? Uf,
    bool Ativo,
    int ProjetosVinculados,
    DateTimeOffset CreatedAt
);

public record PessoaDetalheResponse(
    int Id,
    string Nome,
    string Cpf,
    string? Rg,
    string? RgOrgaoEmissor,
    string? Email,
    string? Celular,
    DateOnly? DataNascimento,
    string? Profissao,
    string Nacionalidade,
    EstadoCivil EstadoCivil,
    RegimeBens? RegimeBens,
    int? ConjugeId,
    string? ConjugeNome,
    string? NomePai,
    string? NomeMae,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Uf,
    string? Observacoes,
    bool Ativo,
    List<ParticipacaoResumoResponse> Projetos,
    DateTimeOffset CreatedAt
);

// ═══════════════════════════════════════════════════════════════
// PROJETO — DTOs
// ═══════════════════════════════════════════════════════════════

public record ProjetoCreateRequest(
    string NomeProjeto,
    string NomeFamilia,
    ModeloHolding ModeloEscolhido,
    string? Observacoes
);

public record ProjetoUpdateRequest(
    string NomeProjeto,
    string NomeFamilia,
    StatusProjeto Status,
    string? Observacoes
);

public record ProjetoListResponse(
    int Id,
    string Codigo,
    string NomeProjeto,
    string NomeFamilia,
    ModeloHolding Modelo,
    StatusProjeto Status,
    int TotalParticipantes,
    int TotalCelulas,
    int TotalBens,
    decimal PatrimonioTotal,
    int? FaseAtual,
    int? ProgressoPct,
    DateTimeOffset? UltimaAtividade,
    DateTimeOffset CreatedAt
);

public record ProjetoDetalheResponse(
    int Id,
    string Codigo,
    string NomeProjeto,
    string NomeFamilia,
    ModeloHolding Modelo,
    StatusProjeto Status,
    DateTimeOffset? DataContratacao,
    DateTimeOffset? DataConclusao,
    string? Observacoes,
    List<ParticipanteResponse> Participantes,
    List<CelulaResumoResponse> Celulas,
    List<BemResumoResponse> Bens,
    WorkflowResumoResponse? Workflow,
    DateTimeOffset CreatedAt
);

// ═══════════════════════════════════════════════════════════════
// PARTICIPANTE — DTOs
// ═══════════════════════════════════════════════════════════════

public record ParticipanteAddRequest(
    int PessoaFisicaId,
    PapelProjeto Papel,
    string? Observacoes
);

public record ParticipanteResponse(
    int Id,
    int PessoaFisicaId,
    string NomePessoa,
    string Cpf,
    PapelProjeto Papel,
    string? Observacoes
);

public record ParticipacaoResumoResponse(
    int ProjetoId,
    string NomeProjeto,
    string NomeFamilia,
    PapelProjeto Papel
);

// ═══════════════════════════════════════════════════════════════
// CÉLULA — DTOs
// ═══════════════════════════════════════════════════════════════

public record CelulaCreateRequest(
    TipoCelula Tipo,
    string NomeCelula,
    string? ObjetoSocial,
    decimal CapitalSocialPrevisto,
    int? AdministradorId
);

public record CelulaUpdateRequest(
    string NomeCelula,
    StatusCelula Status,
    string? Cnpj,
    string? RazaoSocial,
    string? Nire,
    decimal? CapitalSocialEfetivo,
    DateOnly? DataRegistro,
    string? Observacoes
);

public record CelulaResumoResponse(
    int Id,
    TipoCelula Tipo,
    string NomeCelula,
    StatusCelula Status,
    decimal CapitalSocialPrevisto,
    string? Cnpj,
    int TotalSocios
);

public record CelulaDetalheResponse(
    int Id,
    TipoCelula Tipo,
    string NomeCelula,
    StatusCelula Status,
    decimal CapitalSocialPrevisto,
    decimal? CapitalSocialEfetivo,
    string? Cnpj,
    string? RazaoSocial,
    string? Nire,
    DateOnly? DataRegistro,
    string? ObjetoSocial,
    int? AdministradorId,
    string? AdministradorNome,
    string? Observacoes,
    List<SocioCelulaResponse> Socios,
    DateTimeOffset CreatedAt
);

public record SocioCelulaAddRequest(
    int PessoaFisicaId,
    int QuantidadeQuotas,
    decimal ValorPorQuota,
    decimal PercentualParticipacao,
    TipoQuota TipoQuota,
    int? PesoVoto,
    bool UsufrutoVitalicio
);

public record SocioCelulaResponse(
    int Id,
    int PessoaFisicaId,
    string NomeSocio,
    int QuantidadeQuotas,
    decimal ValorPorQuota,
    decimal PercentualParticipacao,
    TipoQuota TipoQuota,
    int? PesoVoto,
    bool UsufrutoVitalicio
);

// ═══════════════════════════════════════════════════════════════
// BEM — DTOs
// ═══════════════════════════════════════════════════════════════

public record BemCreateRequest(
    int ProprietarioId,
    TipoBem Tipo,
    string Descricao,
    decimal ValorDeclaracaoIR,
    decimal ValorMercado,
    DateOnly? DataAvaliacao,
    string? DadosEspecificosJson,
    int? CelulaDestinoId,
    string? Observacoes
);

public record BemUpdateRequest(
    string Descricao,
    decimal ValorDeclaracaoIR,
    decimal ValorMercado,
    DateOnly? DataAvaliacao,
    string? DadosEspecificosJson,
    int? CelulaDestinoId,
    string? Observacoes
);

public record BemResumoResponse(
    int Id,
    TipoBem Tipo,
    string Descricao,
    decimal ValorDeclaracaoIR,
    decimal ValorMercado,
    string ProprietarioNome,
    int? CelulaDestinoId,
    string? CelulaDestinoNome
);

public record BemDetalheResponse(
    int Id,
    int ProprietarioId,
    string ProprietarioNome,
    TipoBem Tipo,
    string Descricao,
    decimal ValorDeclaracaoIR,
    decimal ValorMercado,
    decimal Diferenca,
    DateOnly? DataAvaliacao,
    string? DadosEspecificosJson,
    int? CelulaDestinoId,
    string? CelulaDestinoNome,
    string? Observacoes,
    DateTimeOffset CreatedAt
);

// ═══════════════════════════════════════════════════════════════
// WORKFLOW — DTOs
// ═══════════════════════════════════════════════════════════════

public record WorkflowResumoResponse(
    Guid WorkflowId,
    int TotalPassos,
    int PassosConcluidos,
    int PassosBloqueados,
    int ProgressoPct,
    string? FaseAtual,
    string? PassoAtual
);

public record WorkflowDetalheResponse(
    Guid WorkflowId,
    string Name,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    List<WorkflowFaseResponse> Fases
);

public record WorkflowFaseResponse(
    int FaseNumero,
    string FaseNome,
    int TotalPassos,
    int Concluidos,
    int ProgressoPct,
    List<WorkflowPassoResponse> Passos
);

public record WorkflowPassoResponse(
    int StepCode,
    int Phase,
    int Order,
    string Title,
    string Objective,
    int Status,
    string StatusLabel,
    bool Optional,
    DateTime? StartedAtUtc,
    DateTime? FinishedAtUtc,
    List<WorkflowDocResponse> Documents,
    List<WorkflowGateResponse> Gates,
    List<string> BlockingReasons,
    List<int> DependsOn
);

public record WorkflowDocResponse(
    string Code,
    string Name,
    bool Required,
    bool Generated
);

public record WorkflowGateResponse(
    string Code,
    string Description,
    bool Passed,
    bool Blocking
);

public record WorkflowStartStepRequest(int StepCode);
public record WorkflowSetDocRequest(int StepCode, string DocumentCode, bool Generated);
public record WorkflowSetGateRequest(int StepCode, string GateCode, bool Passed);
public record WorkflowCompleteStepRequest(int StepCode);

public record WorkflowActionResponse(
    bool Success,
    string Message,
    List<string> BlockingReasons
);

// ═══════════════════════════════════════════════════════════════
// DOCUMENTOS — DTOs
// ═══════════════════════════════════════════════════════════════

public record GerarDocumentoRequest(
    int ProjetoId,
    int CelulaId,
    string CodigoModulo   // "DESTINO.CONSTITUICAO.V1", etc.
);

public record DocumentoGeradoResponse(
    Guid Id,
    string CodigoModulo,
    string NomeDocumento,
    string ConteudoHtml,
    List<string> BlocosAtivados,
    List<ItemValidacaoResponse> Validacoes,
    DateTimeOffset GeradoEm
);

public record ItemValidacaoResponse(
    string Codigo,
    string Mensagem,
    string Severidade
);
